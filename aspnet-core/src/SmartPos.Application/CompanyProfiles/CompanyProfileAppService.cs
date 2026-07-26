using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.CompanyProfiles.Dto;

namespace SmartPos.CompanyProfiles
{
    [AbpAuthorize(PermissionNames.Pages_CompanyProfiles)]
    public class CompanyProfileAppService : AsyncCrudAppService<CompanyProfile, CompanyProfileDto, int, PagedCompanyProfileResultRequestDto, CreateCompanyProfileDto, CompanyProfileDto>, ICompanyProfileAppService
    {
        public CompanyProfileAppService(IRepository<CompanyProfile> repository)
            : base(repository)
        {
        }

        [AbpAllowAnonymous]
        public async Task<CompanyProfileDto> GetCurrentAsync()
        {
            var profile = await Repository.GetAll()
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            return profile == null ? null : ObjectMapper.Map<CompanyProfileDto>(profile);
        }

        public override async Task<CompanyProfileDto> CreateAsync(CreateCompanyProfileDto input)
        {
            CheckCreatePermission();

            var profile = ObjectMapper.Map<CompanyProfile>(input);
            profile.ImagePath = CompanyProfileImageStore.SaveBase64Image(input.ImageBase64);

            await Repository.InsertAsync(profile);
            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAsync(new EntityDto<int>(profile.Id));
        }

        public override async Task<CompanyProfileDto> UpdateAsync(CompanyProfileDto input)
        {
            CheckUpdatePermission();

            var profile = await GetEntityByIdAsync(input.Id);

            profile.Name = input.Name;
            profile.InvoiceAddress = input.InvoiceAddress;
            profile.InvoiceContactEmail = input.InvoiceContactEmail;
            profile.InvoiceContactPhone = input.InvoiceContactPhone;
            profile.TaxNumber = input.TaxNumber;
            profile.Website = input.Website;
            profile.InvoiceFooter = input.InvoiceFooter;

            if (CompanyProfileImageStore.IsNewImagePayload(input.ImageBase64))
            {
                CompanyProfileImageStore.DeleteIfExists(profile.ImagePath);
                profile.ImagePath = CompanyProfileImageStore.SaveBase64Image(input.ImageBase64);
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAsync(new EntityDto<int>(profile.Id));
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var profile = await Repository.GetAsync(input.Id);
            CompanyProfileImageStore.DeleteIfExists(profile.ImagePath);
            await Repository.DeleteAsync(profile);
        }

        protected override IQueryable<CompanyProfile> CreateFilteredQuery(PagedCompanyProfileResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.InvoiceAddress != null && x.InvoiceAddress.Contains(input.Keyword))
                         || (x.InvoiceContactEmail != null && x.InvoiceContactEmail.Contains(input.Keyword))
                         || (x.InvoiceContactPhone != null && x.InvoiceContactPhone.Contains(input.Keyword))
                         || (x.TaxNumber != null && x.TaxNumber.Contains(input.Keyword)));
        }
    }
}
