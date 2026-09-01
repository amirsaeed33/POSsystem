using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Runtime.Caching;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches.Dto;
using SmartPos.Emailing;
using SmartPos.HostCatalog;
using SmartPos.Inventory;
using SmartPos.Lookups;
using SmartPos.MultiTenancy;
using SmartPos.Products;

namespace SmartPos.Branches
{
    [AbpAuthorize]
    public class BranchAppService : AsyncCrudAppService<Branch, BranchDto, int, PagedBranchResultRequestDto, CreateBranchDto, BranchDto>, IBranchAppService
    {
        private const string CacheName = "BranchLookupCache";
        public UserManager UserManager { get; set; }

        private readonly IRepository<Product> _productRepository;
        private readonly IBranchStockManager _branchStockManager;
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IRepository<LookUp> _lookUpRepository;
        private readonly IRepository<EmailTemplate> _emailTemplateRepository;
        private readonly BranchStatusLookup _branchStatusLookup;
        private readonly ISmtpMailSender _smtpMailSender;
        private readonly IConfiguration _configuration;
        private readonly BranchCatalogSeedService _branchCatalogSeedService;
        private readonly ICacheManager _cacheManager;

        public BranchAppService(
            IRepository<Branch> repository,
            IRepository<Product> productRepository,
            IBranchStockManager branchStockManager,
            IRepository<Tenant> tenantRepository,
            IRepository<LookUp> lookUpRepository,
            IRepository<EmailTemplate> emailTemplateRepository,
            BranchStatusLookup branchStatusLookup,
            ISmtpMailSender smtpMailSender,
            IConfiguration configuration,
            BranchCatalogSeedService branchCatalogSeedService,
            ICacheManager cacheManager)
            : base(repository)
        {
            _productRepository = productRepository;
            _branchStockManager = branchStockManager;
            _tenantRepository = tenantRepository;
            _lookUpRepository = lookUpRepository;
            _emailTemplateRepository = emailTemplateRepository;
            _branchStatusLookup = branchStatusLookup;
            _smtpMailSender = smtpMailSender;
            _configuration = configuration;
            _branchCatalogSeedService = branchCatalogSeedService;
            _cacheManager = cacheManager;
            CreatePermissionName = PermissionNames.Pages_Branches;
            UpdatePermissionName = PermissionNames.Pages_Branches;
            DeletePermissionName = PermissionNames.Pages_Branches;
            GetPermissionName = PermissionNames.Pages_Branches;
            GetAllPermissionName = PermissionNames.Pages_Branches;
        }

        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task<BranchDto> CreateAsync(CreateBranchDto input)
        {
            CheckCreatePermission();

            if (!AbpSession.TenantId.HasValue)
            {
                throw new UserFriendlyException(
                    "Host administrators cannot create locations. Each business creates its own locations.");
            }

            NormalizeCreateInput(input);

            var branch = ObjectMapper.Map<Branch>(input);
            branch.TenantId = AbpSession.TenantId;
            // New branches always start as Pending until host admin approves.
            branch.StatusId = await _branchStatusLookup.GetIdAsync(BranchStatuses.Pending);
            branch.ImagePath = BranchImageStore.SaveBase64Image(input.ImageBase64);

            await Repository.InsertAsync(branch);
            await CurrentUnitOfWork.SaveChangesAsync();

            if (!AbpSession.UserId.HasValue)
            {
                throw new UserFriendlyException("User session is required to create a branch seed request.");
            }

            await _branchCatalogSeedService.CreateRequestAsync(
                branch,
                input.CompanyTypeId,
                input.HostCatalogItemIds,
                AbpSession.UserId.Value);

            await SeedSharedProductsAsync(branch.Id);

            // First branch after signup: bind the creating user so session/context have a location.
            var currentUser = await UserManager.GetUserByIdAsync(AbpSession.UserId.Value);
            if (currentUser.BranchId == null)
            {
                currentUser.BranchId = branch.Id;
                await UserManager.UpdateAsync(currentUser);
            }

            await ClearLookupCacheAsync();
            return await GetAsync(new EntityDto<int>(branch.Id));
        }

        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task<BranchDto> UpdateAsync(BranchDto input)
        {
            CheckUpdatePermission();

            var branch = await GetEntityByIdAsync(input.Id);

            branch.Name = input.Name;
            branch.Code = input.Code;
            branch.IsActive = input.IsActive;
            branch.InvoiceAddress = input.InvoiceAddress;
            branch.InvoiceContactEmail = input.InvoiceContactEmail;
            branch.InvoiceContactPhone = input.InvoiceContactPhone;
            branch.TaxNumber = input.TaxNumber;
            branch.Website = input.Website;
            branch.InvoiceFooter = input.InvoiceFooter;
            branch.TaxPercent = Math.Max(0, input.TaxPercent);
            branch.DiscountPercent = Math.Max(0, input.DiscountPercent);
            branch.DiscountAmount = Math.Max(0, input.DiscountAmount);

            // Only host admin with approve permission may change StatusId.
            // Selecting Approved sends an activation email and keeps Pending.
            var sendActivationEmail = false;
            if (AbpSession.TenantId == null
                && input.StatusId > 0
                && input.StatusId != branch.StatusId
                && await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches_Approve))
            {
                var status = await _branchStatusLookup.GetAsync(input.StatusId);
                if (BranchStatuses.IsApproved(status.Name))
                {
                    sendActivationEmail = true;
                }
                else
                {
                    branch.StatusId = status.Id;
                    if (string.Equals(status.Name, BranchStatuses.Rejected, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(status.Name, BranchStatuses.Pending, StringComparison.OrdinalIgnoreCase))
                    {
                        ClearActivationToken(branch);
                    }
                }
            }

            if (BranchImageStore.IsNewImagePayload(input.ImageBase64))
            {
                BranchImageStore.DeleteIfExists(branch.ImagePath);
                branch.ImagePath = BranchImageStore.SaveBase64Image(input.ImageBase64);
            }
            else if (string.IsNullOrWhiteSpace(input.ImageBase64) && string.IsNullOrWhiteSpace(input.ImagePath))
            {
                BranchImageStore.DeleteIfExists(branch.ImagePath);
                branch.ImagePath = null;
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            if (sendActivationEmail)
            {
                await _branchCatalogSeedService.ApproveAndCopyAsync(branch, AbpSession.UserId);
                await SendBranchActivationEmailAsync(branch);
            }
            else if (AbpSession.TenantId == null
                     && input.StatusId > 0
                     && await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches_Approve))
            {
                var status = await _branchStatusLookup.GetAsync(input.StatusId);
                if (string.Equals(status.Name, BranchStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
                {
                    await _branchCatalogSeedService.RejectAsync(branch.Id, AbpSession.UserId);
                }
            }

            await ClearLookupCacheAsync();
            return await GetAsync(new EntityDto<int>(branch.Id));
        }

        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var branch = await GetEntityByIdAsync(input.Id);
            BranchImageStore.DeleteIfExists(branch.ImagePath);
            await Repository.DeleteAsync(branch);
            await ClearLookupCacheAsync();
        }

        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task<BranchDto> GetAsync(EntityDto<int> input)
        {
            BranchDto dto;
            if (AbpSession.TenantId == null)
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    var entity = await Repository.GetAsync(input.Id);
                    dto = MapToEntityDto(entity);
                }
            }
            else
            {
                dto = await base.GetAsync(input);
            }

            await FillStatusNamesAsync(new[] { dto });
            return dto;
        }

        protected override async Task<Branch> GetEntityByIdAsync(int id)
        {
            if (AbpSession.TenantId == null)
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    return await Repository.GetAsync(id);
                }
            }

            return await base.GetEntityByIdAsync(id);
        }

        /// <summary>
        /// Host admin sees every business location; tenant users see only their own.
        /// </summary>
        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task<PagedResultDto<BranchDto>> GetAllAsync(PagedBranchResultRequestDto input)
        {
            CheckGetAllPermission();

            PagedResultDto<BranchDto> result;
            if (await CanBrowseAllLocationsAsync())
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    var query = CreateFilteredQuery(input).Where(x => x.TenantId != null);
                    var totalCount = await AsyncQueryableExecuter.CountAsync(query);
                    query = ApplySorting(query, input);
                    query = ApplyPaging(query, input);
                    var entities = await AsyncQueryableExecuter.ToListAsync(query);
                    result = new PagedResultDto<BranchDto>(
                        totalCount,
                        entities.Select(MapToEntityDto).ToList());
                    await FillTenancyNamesAsync(result.Items);
                }
            }
            else if (AbpSession.TenantId.HasValue)
            {
                result = await base.GetAllAsync(input);
            }
            else
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    result = await base.GetAllAsync(input);
                    await FillTenancyNamesAsync(result.Items);
                }
            }

            await FillStatusNamesAsync(result.Items);
            return result;
        }

        public async Task<ListResultDto<BranchDto>> GetLookupAsync()
        {
            var canBrowseAll = await CanBrowseAllLocationsAsync();
            var tenantId = AbpSession.TenantId ?? 0;
            var userId = AbpSession.UserId ?? 0;
            var cacheKey = $"Tenant_{tenantId}_User_{userId}_BrowseAll_{canBrowseAll}";

            var cache = _cacheManager.GetCache<string, ListResultDto<BranchDto>>(CacheName);
            return await cache.GetAsync(cacheKey, async (key) =>
            {
                // Host admin: every business location across tenants.
                if (canBrowseAll)
                {
                    using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                    {
                        var allLocations = await Repository.GetAll()
                            .Where(x => x.IsActive && x.TenantId != null)
                            .OrderBy(x => x.Name)
                            .ToListAsync();
                        var allDtos = ObjectMapper.Map<List<BranchDto>>(allLocations);
                        for (var i = 0; i < allLocations.Count; i++)
                        {
                            allDtos[i].TenantId = allLocations[i].TenantId;
                        }
                        await FillTenancyNamesAsync(allDtos);
                        await FillStatusNamesAsync(allDtos);
                        return new ListResultDto<BranchDto>(allDtos);
                    }
                }

                IQueryable<Branch> query = Repository.GetAll().Where(x => x.IsActive);

                if (!await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches))
                {
                    if (!AbpSession.UserId.HasValue)
                    {
                        return new ListResultDto<BranchDto>(new List<BranchDto>());
                    }

                    var currentUser = await UserManager.GetUserByIdAsync(AbpSession.UserId.Value);
                    if (!currentUser.BranchId.HasValue)
                    {
                        return new ListResultDto<BranchDto>(new List<BranchDto>());
                    }

                    query = query.Where(x => x.Id == currentUser.BranchId.Value);
                }

                var branches = await query.OrderBy(x => x.Name).ToListAsync();
                var dtos = ObjectMapper.Map<List<BranchDto>>(branches);
                for (var i = 0; i < branches.Count; i++)
                {
                    dtos[i].TenantId = branches[i].TenantId;
                }
                await FillStatusNamesAsync(dtos);
                return new ListResultDto<BranchDto>(dtos);
            });
        }

        private async Task ClearLookupCacheAsync()
        {
            await _cacheManager.GetCache(CacheName).ClearAsync();
        }

        public async Task<BranchDto> GetInvoiceInfoAsync()
        {
            Branch branch = null;

            if (AbpSession.UserId.HasValue)
            {
                var currentUser = await UserManager.FindByIdAsync(AbpSession.UserId.Value.ToString());
                if (currentUser?.BranchId != null)
                {
                    branch = await Repository.FirstOrDefaultAsync(currentUser.BranchId.Value);
                }
            }

            if (branch == null)
            {
                branch = await Repository.GetAll()
                    .OrderBy(x => x.Id)
                    .FirstOrDefaultAsync();
            }

            if (branch == null)
            {
                return null;
            }

            var dto = ObjectMapper.Map<BranchDto>(branch);
            await FillStatusNamesAsync(new[] { dto });
            return dto;
        }

        [AbpAuthorize(PermissionNames.Pages_Branches, PermissionNames.Pages_Branches_Approve)]
        public async Task<ListResultDto<BranchDto>> GetPendingApprovalsAsync()
        {
            var pendingStatusId = await _branchStatusLookup.GetIdAsync(BranchStatuses.Pending);

            if (AbpSession.TenantId.HasValue)
            {
                var tenantId = AbpSession.TenantId.Value;
                var branches = await Repository.GetAll()
                    .Where(x => x.TenantId == tenantId && x.StatusId == pendingStatusId)
                    .OrderByDescending(x => x.CreationTime)
                    .ToListAsync();

                var dtos = ObjectMapper.Map<List<BranchDto>>(branches);
                await FillTenancyNamesAsync(dtos);
                await FillStatusNamesAsync(dtos);
                return new ListResultDto<BranchDto>(dtos);
            }

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var branches = await Repository.GetAll()
                    .Where(x => x.TenantId != null && x.StatusId == pendingStatusId)
                    .OrderByDescending(x => x.CreationTime)
                    .ToListAsync();

                var dtos = ObjectMapper.Map<List<BranchDto>>(branches);
                await FillTenancyNamesAsync(dtos);
                await FillStatusNamesAsync(dtos);
                return new ListResultDto<BranchDto>(dtos);
            }
        }

        [AbpAuthorize(PermissionNames.Pages_Branches_Approve)]
        public async Task<BranchDto> ChangeStatusAsync(ChangeBranchStatusDto input)
        {
            var status = await _branchStatusLookup.GetAsync(input.StatusId);

            // Approved means "send activation email"; status stays Pending until the link is opened.
            if (BranchStatuses.IsApproved(status.Name))
            {
                return await RequestBranchActivationAsync(new EntityDto<int>(input.Id));
            }

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var branch = await Repository.GetAll()
                    .FirstOrDefaultAsync(x => x.Id == input.Id);

                if (branch == null)
                {
                    throw new UserFriendlyException("Branch not found.");
                }

                if (!branch.TenantId.HasValue)
                {
                    throw new UserFriendlyException("Host branches do not require approval.");
                }

                branch.StatusId = status.Id;
                ClearActivationToken(branch);
                await CurrentUnitOfWork.SaveChangesAsync();

                if (string.Equals(status.Name, BranchStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
                {
                    await _branchCatalogSeedService.RejectAsync(branch.Id, AbpSession.UserId);
                }

                var dto = ObjectMapper.Map<BranchDto>(branch);
                var tenant = await _tenantRepository.FirstOrDefaultAsync(branch.TenantId.Value);
                dto.TenancyName = tenant?.TenancyName;
                dto.Status = status.Name;
                dto.StatusDisplayName = status.DisplayName;
                return dto;
            }
        }

        [AbpAuthorize(PermissionNames.Pages_Branches_Approve)]
        public async Task<BranchDto> RequestBranchActivationAsync(EntityDto<int> input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var branch = await Repository.GetAll()
                    .FirstOrDefaultAsync(x => x.Id == input.Id);

                if (branch == null)
                {
                    throw new UserFriendlyException("Branch not found.");
                }

                // Approve seed request and copy host catalog into branch-owned tables first.
                await _branchCatalogSeedService.ApproveAndCopyAsync(branch, AbpSession.UserId);
                await SendBranchActivationEmailAsync(branch);
                return await GetAsync(new EntityDto<int>(branch.Id));
            }
        }

        [AbpAllowAnonymous]
        public async Task<ActivateBranchResultDto> ActivateBranchAsync(ActivateBranchInput input)
        {
            var token = (input?.Token ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new UserFriendlyException("Activation token is required.");
            }

            var tokenHash = BranchActivationToken.Hash(token);
            var now = DateTime.UtcNow;

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var branch = await Repository.GetAll()
                    .FirstOrDefaultAsync(x =>
                        x.ActivationTokenHash != null
                        && x.ActivationTokenHash == tokenHash);

                if (branch == null
                    || string.IsNullOrEmpty(branch.ActivationTokenHash)
                    || !BranchActivationToken.FixedTimeEquals(branch.ActivationTokenHash, tokenHash))
                {
                    throw new UserFriendlyException("Invalid or expired activation link.");
                }

                if (!branch.ActivationTokenExpiresAt.HasValue
                    || branch.ActivationTokenExpiresAt.Value < now)
                {
                    ClearActivationToken(branch);
                    await CurrentUnitOfWork.SaveChangesAsync();
                    throw new UserFriendlyException("This activation link has expired. Ask the host admin to send a new one.");
                }

                if (!branch.TenantId.HasValue)
                {
                    throw new UserFriendlyException("Host branches do not require activation.");
                }

                var approvedStatusId = await _branchStatusLookup.GetIdAsync(BranchStatuses.Approved);
                branch.StatusId = approvedStatusId;
                branch.IsActive = true;
                ClearActivationToken(branch);
                await CurrentUnitOfWork.SaveChangesAsync();

                var tenant = await _tenantRepository.FirstOrDefaultAsync(branch.TenantId.Value);
                return new ActivateBranchResultDto
                {
                    BranchName = branch.Name,
                    TenancyName = tenant?.TenancyName,
                    TenantName = !string.IsNullOrWhiteSpace(tenant?.Name)
                        ? tenant.Name
                        : tenant?.TenancyName
                };
            }
        }

        protected override IQueryable<Branch> CreateFilteredQuery(PagedBranchResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || x.Code.Contains(input.Keyword)
                         || (x.InvoiceAddress != null && x.InvoiceAddress.Contains(input.Keyword))
                         || (x.InvoiceContactEmail != null && x.InvoiceContactEmail.Contains(input.Keyword))
                         || (x.InvoiceContactPhone != null && x.InvoiceContactPhone.Contains(input.Keyword))
                         || (x.TaxNumber != null && x.TaxNumber.Contains(input.Keyword)))
                .WhereIf(input.StatusId.HasValue, x => x.StatusId == input.StatusId.Value);
        }

        protected override BranchDto MapToEntityDto(Branch entity)
        {
            var dto = base.MapToEntityDto(entity);
            dto.TenantId = entity.TenantId;
            dto.StatusId = entity.StatusId;
            return dto;
        }

        private async Task<bool> CanBrowseAllLocationsAsync()
        {
            if (await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches_Approve)
                || await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Tenants))
            {
                return true;
            }

            // Host user with a location cookie still needs the full location list.
            // Host-only permissions may not apply once Abp.TenantId is set.
            if (AbpSession.UserId.HasValue)
            {
                var user = await UserManager.GetUserByIdAsync(AbpSession.UserId.Value);
                if (user != null && !user.TenantId.HasValue)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task FillTenancyNamesAsync(IReadOnlyList<BranchDto> branches)
        {
            if (branches == null || branches.Count == 0)
            {
                return;
            }

            var tenantIds = branches
                .Where(x => x.TenantId.HasValue)
                .Select(x => x.TenantId.Value)
                .Distinct()
                .ToList();

            if (tenantIds.Count == 0)
            {
                return;
            }

            var tenants = await _tenantRepository.GetAll()
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.TenancyName);

            foreach (var dto in branches)
            {
                if (dto.TenantId.HasValue && tenants.TryGetValue(dto.TenantId.Value, out var tenancyName))
                {
                    dto.TenancyName = tenancyName;
                }
            }
        }

        private async Task FillStatusNamesAsync(IReadOnlyList<BranchDto> branches)
        {
            if (branches == null || branches.Count == 0)
            {
                return;
            }

            var statusIds = branches.Select(x => x.StatusId).Where(x => x > 0).Distinct().ToList();
            if (statusIds.Count == 0)
            {
                return;
            }

            Dictionary<int, LookUp> statuses;
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                statuses = await _lookUpRepository.GetAll()
                    .Where(x => statusIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id);
            }

            foreach (var dto in branches)
            {
                if (statuses.TryGetValue(dto.StatusId, out var lookUp))
                {
                    dto.Status = lookUp.Name;
                    dto.StatusDisplayName = lookUp.DisplayName;
                }
            }
        }

        private async Task SendBranchActivationEmailAsync(Branch branch)
        {
            if (branch == null)
            {
                throw new UserFriendlyException("Branch not found.");
            }

            if (!branch.TenantId.HasValue)
            {
                throw new UserFriendlyException("Host branches do not require approval.");
            }

            if (string.IsNullOrWhiteSpace(branch.InvoiceContactEmail))
            {
                throw new UserFriendlyException(
                    "Branch invoice contact email is required before sending the activation link.");
            }

            var pendingStatusId = await _branchStatusLookup.GetIdAsync(BranchStatuses.Pending);
            if (branch.StatusId != pendingStatusId)
            {
                throw new UserFriendlyException(
                    "Only pending branches can receive an activation email.");
            }

            var tenant = await _tenantRepository.FirstOrDefaultAsync(branch.TenantId.Value);
            if (tenant == null)
            {
                throw new UserFriendlyException("Tenant not found for this branch.");
            }

            var expirationHours = GetActivationExpirationHours();
            var plainToken = BranchActivationToken.CreatePlainToken();
            branch.ActivationTokenHash = BranchActivationToken.Hash(plainToken);
            branch.ActivationTokenExpiresAt = DateTime.UtcNow.AddHours(expirationHours);
            await CurrentUnitOfWork.SaveChangesAsync();

            var tenantName = !string.IsNullOrWhiteSpace(tenant.Name)
                ? tenant.Name
                : tenant.TenancyName;
            var activationLink = BuildActivationLink(plainToken);

            var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["TenantName"] = tenantName ?? string.Empty,
                ["BranchName"] = branch.Name ?? string.Empty,
                ["ActivationLink"] = activationLink,
                ["AppName"] = "SmartPos",
                ["ExpirationHours"] = expirationHours.ToString()
            };

            var (subject, bodyHtml) = await ResolveBranchActivationTemplateAsync(placeholders);
            await _smtpMailSender.SendAsync(
                branch.InvoiceContactEmail.Trim(),
                subject,
                bodyHtml,
                isBodyHtml: true);
        }

        private async Task<(string Subject, string BodyHtml)> ResolveBranchActivationTemplateAsync(
            IReadOnlyDictionary<string, string> placeholders)
        {
            EmailTemplate template;
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                template = await _emailTemplateRepository.FirstOrDefaultAsync(
                    x => x.Code == EmailTemplateCodes.BranchActivation && x.IsActive && x.TenantId == null);
            }

            var subjectTemplate = template?.Subject ?? "Activate {{BranchName}} for {{TenantName}}";
            var bodyTemplate = !string.IsNullOrWhiteSpace(template?.BodyHtml)
                ? template.BodyHtml
                : EmailTemplateDefaults.BranchActivationBodyHtml();

            return (
                EmailTemplateRenderer.Render(subjectTemplate, placeholders),
                EmailTemplateRenderer.Render(bodyTemplate, placeholders)
            );
        }

        private string BuildActivationLink(string plainToken)
        {
            var clientRoot = (_configuration["App:ClientRootAddress"] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(clientRoot))
            {
                throw new UserFriendlyException("Client root address is not configured.");
            }

            if (!clientRoot.EndsWith("/"))
            {
                clientRoot += "/";
            }

            return $"{clientRoot}auth/activate-branch?token={Uri.EscapeDataString(plainToken)}";
        }

        private int GetActivationExpirationHours()
        {
            if (int.TryParse(_configuration["App:BranchActivation:ExpirationHours"], out var hours)
                && hours > 0)
            {
                return hours;
            }

            return 72;
        }

        private static void ClearActivationToken(Branch branch)
        {
            branch.ActivationTokenHash = null;
            branch.ActivationTokenExpiresAt = null;
        }

        private static void NormalizeCreateInput(CreateBranchDto input)
        {
            if (input == null)
            {
                return;
            }

            input.Name = input.Name?.Trim();
            input.Code = input.Code?.Trim();
            input.InvoiceAddress = NullIfWhiteSpace(input.InvoiceAddress);
            input.InvoiceContactEmail = NullIfWhiteSpace(input.InvoiceContactEmail);
            input.InvoiceContactPhone = NullIfWhiteSpace(input.InvoiceContactPhone);
            input.TaxNumber = NullIfWhiteSpace(input.TaxNumber);
            input.Website = NullIfWhiteSpace(input.Website);
            input.InvoiceFooter = NullIfWhiteSpace(input.InvoiceFooter);
            input.ImageBase64 = NullIfWhiteSpace(input.ImageBase64);

            if (!string.IsNullOrWhiteSpace(input.InvoiceContactEmail)
                && !input.InvoiceContactEmail.Contains('@'))
            {
                throw new UserFriendlyException("Invoice contact email is not valid.");
            }
        }

        private static string NullIfWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private async Task SeedSharedProductsAsync(int branchId)
        {
            // Tenant-level products with no BranchStock rows are already visible everywhere.
            // Seed products that were assigned to every other active branch (legacy "all locations").
            var otherBranchIds = await Repository.GetAll()
                .Where(x => x.IsActive && x.Id != branchId)
                .Select(x => x.Id)
                .ToListAsync();

            if (!otherBranchIds.Any())
            {
                return;
            }

            var products = await _productRepository.GetAll()
                .Select(x => new
                {
                    x.Id,
                    x.Price,
                    x.WholesalePrice,
                    x.CostPrice
                })
                .ToListAsync();

            foreach (var product in products)
            {
                var assigned = await _branchStockManager.GetAssignedBranchIdsAsync(product.Id);
                if (!assigned.Any())
                {
                    continue;
                }

                if (!otherBranchIds.All(id => assigned.Contains(id)))
                {
                    continue;
                }

                await _branchStockManager.UpsertStockAndPricesAsync(
                    branchId,
                    product.Id,
                    0,
                    product.Price,
                    product.WholesalePrice,
                    product.CostPrice);
            }
        }
    }
}
