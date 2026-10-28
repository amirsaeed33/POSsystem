using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Customers;
using SmartPos.Inventory;
using SmartPos.Orders.Dto;
using SmartPos.Products;
using SmartPos.Sales;
using SmartPos.Sales.Dto;
using Abp.Net.Mail;
using Abp.Events.Bus;
using SmartPos.Emailing;
using SmartPos.Staffs;
using SmartPos.Categories;

namespace SmartPos.Orders
{
    [AbpAuthorize(PermissionNames.Pages_CustomerOrders)]
    public class CustomerOrderAppService :
        AsyncCrudAppService<CustomerOrder, CustomerOrderDto, int, PagedCustomerOrderResultRequestDto, CreateCustomerOrderDto, CustomerOrderDto>,
        ICustomerOrderAppService
    {
        public IEventBus EventBus { get; set; } = NullEventBus.Instance;

        private readonly IRepository<CustomerOrderLine> _lineRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<BranchStock> _branchStockRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;
        private readonly ISaleAppService _saleAppService;
        private readonly IBranchEmailNotifier _branchEmailNotifier;
        private readonly IRepository<BranchNotificationSetting> _notificationSettingRepository;
        private readonly IRepository<EmailTemplate> _emailTemplateRepository;
        private readonly IRepository<DeliveryArea> _deliveryAreaRepository;
        private readonly IRepository<DeliveryPromotion> _deliveryPromotionRepository;
        private readonly IRepository<Staff> _staffRepository;
        private readonly IRepository<Category> _categoryRepository;

        public CustomerOrderAppService(
            IRepository<CustomerOrder> repository,
            IRepository<CustomerOrderLine> lineRepository,
            IRepository<Product> productRepository,
            IRepository<Customer> customerRepository,
            IRepository<User, long> userRepository,
            IRepository<Branch> branchRepository,
            IRepository<BranchStock> branchStockRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext,
            ISaleAppService saleAppService,
            IBranchEmailNotifier branchEmailNotifier,
            IRepository<BranchNotificationSetting> notificationSettingRepository,
            IRepository<EmailTemplate> emailTemplateRepository,
            IRepository<DeliveryArea> deliveryAreaRepository,
            IRepository<DeliveryPromotion> deliveryPromotionRepository,
            IRepository<Staff> staffRepository,
            IRepository<Category> categoryRepository)
            : base(repository)
        {
            _lineRepository = lineRepository;
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _branchRepository = branchRepository;
            _branchStockRepository = branchStockRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
            _saleAppService = saleAppService;
            _branchEmailNotifier = branchEmailNotifier;
            _notificationSettingRepository = notificationSettingRepository;
            _emailTemplateRepository = emailTemplateRepository;
            _deliveryAreaRepository = deliveryAreaRepository;
            _deliveryPromotionRepository = deliveryPromotionRepository;
            _staffRepository = staffRepository;
            _categoryRepository = categoryRepository;
            
            CreatePermissionName = PermissionNames.Pages_CustomerOrders_Create;
            UpdatePermissionName = PermissionNames.Pages_CustomerOrders_Edit;
            DeletePermissionName = PermissionNames.Pages_CustomerOrders_Delete;
            GetPermissionName = PermissionNames.Pages_CustomerOrders;
            GetAllPermissionName = PermissionNames.Pages_CustomerOrders;
        }

        [AbpAllowAnonymous]
        public async Task<List<OnlineProductDto>> GetOnlineCatalogAsync(int? branchId)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                Branch targetBranch = null;
                if (branchId.HasValue && branchId.Value > 0)
                {
                    targetBranch = await _branchRepository.FirstOrDefaultAsync(b => b.Id == branchId.Value);
                }

                if (targetBranch == null)
                {
                    targetBranch = await _branchRepository.FirstOrDefaultAsync(b => b.IsActive);
                }

                if (targetBranch == null)
                {
                    return new List<OnlineProductDto>();
                }

                var targetBranchId = targetBranch.Id;
                var isRestaurant = targetBranch.BusinessType == BusinessType.Restaurant;
                var branchCurrency = !string.IsNullOrWhiteSpace(targetBranch.CurrencyFormat) ? targetBranch.CurrencyFormat : "PKR";

                var products = await _productRepository.GetAllIncluding(p => p.Category, p => p.Unit, p => p.Brand)
                    .AsNoTracking()
                    .Where(p => (p.BranchId == targetBranchId) || (p.BranchId == 0 && (!p.TenantId.HasValue || p.TenantId == targetBranch.TenantId)))
                    .WhereVisibleToBranch(_branchStockRepository.GetAll(), targetBranchId)
                    .Where(p => (p.Barcode == null || !p.Barcode.StartsWith("GROCERY-")) && p.Name != "Grocery")
                    .ToListAsync();

                var branchInfo = await _branchStockRepository.GetAll()
                    .Where(s => s.BranchId == targetBranchId)
                    .ToDictionaryAsync(s => s.ProductId, s => new { s.Quantity, s.Price });

                return products
                    .Select(p => new OnlineProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        CategoryName = p.Category?.Name ?? "General",
                        BrandName = p.Brand?.Name,
                        Barcode = p.Barcode,
                        UnitName = p.Unit?.Name ?? "Pcs",
                        Price = branchInfo.TryGetValue(p.Id, out var info) && info.Price > 0 ? info.Price : p.Price,
                        CurrencyFormat = branchCurrency,
                        ImagePath = p.ImagePath,
                        InStock = isRestaurant ? true : (branchInfo.TryGetValue(p.Id, out info) ? info.Quantity > 0 : true),
                        StockQuantity = isRestaurant ? 999 : (branchInfo.TryGetValue(p.Id, out info) ? info.Quantity : 0),
                        Specifications = p.Specifications,
                        IsFreeDelivery = p.IsFreeDelivery,
                        CategoryId = p.CategoryId,
                        IsDeal = p.IsDeal || (p.Category != null && p.Category.Name.ToLower().Contains("deal"))
                    })
                    .ToList();
            }
        }

        [AbpAllowAnonymous]
        public async Task<OnlineProductDto> GetOnlineProductAsync(int id, int? branchId)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var product = await _productRepository.GetAllIncluding(p => p.Category, p => p.Unit, p => p.Brand)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return null;
                }

                Branch targetBranch = null;
                if (branchId.HasValue && branchId.Value > 0)
                {
                    targetBranch = await _branchRepository.FirstOrDefaultAsync(b => b.Id == branchId.Value);
                }
                if (targetBranch == null)
                {
                    targetBranch = await _branchRepository.FirstOrDefaultAsync(b => b.IsActive);
                }

                var isRestaurant = targetBranch != null && targetBranch.BusinessType == BusinessType.Restaurant;
                var price = product.Price;
                var stockQty = isRestaurant ? 999m : 0m;
                var inStock = true;
                var branchCurrency = !string.IsNullOrWhiteSpace(targetBranch?.CurrencyFormat) ? targetBranch.CurrencyFormat : "PKR";

                if (targetBranch != null)
                {
                    var branchStock = await _branchStockRepository.FirstOrDefaultAsync(s => s.BranchId == targetBranch.Id && s.ProductId == id);
                    if (branchStock != null)
                    {
                        if (branchStock.Price > 0)
                        {
                            price = branchStock.Price;
                        }
                        if (!isRestaurant)
                        {
                            stockQty = branchStock.Quantity;
                            inStock = branchStock.Quantity > 0;
                        }
                    }
                }

                return new OnlineProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    CategoryName = product.Category?.Name ?? "General",
                    BrandName = product.Brand?.Name,
                    Barcode = product.Barcode,
                    UnitName = product.Unit?.Name ?? "Pcs",
                    Price = price,
                    CurrencyFormat = branchCurrency,
                    ImagePath = product.ImagePath,
                    InStock = inStock,
                    StockQuantity = stockQty,
                    Specifications = product.Specifications,
                    IsFreeDelivery = product.IsFreeDelivery,
                    CategoryId = product.CategoryId,
                    IsDeal = product.IsDeal || (product.Category != null && product.Category.Name.ToLower().Contains("deal"))
                };
            }
        }

        [AbpAllowAnonymous]
        public async Task<OnlineStoreHeaderDto> GetOnlineStoreHeaderAsync(int branchId)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var branch = await _branchRepository.FirstOrDefaultAsync(b => b.Id == branchId);
                var areas = await _deliveryAreaRepository.GetAll()
                    .Where(a => a.BranchId == branchId && a.IsActive)
                    .OrderBy(a => a.DeliveryFee)
                    .ToListAsync();

                var baseFee = areas.Any() ? areas.First().DeliveryFee : 100m;
                var freeThreshold = 1500m;
                var now = Abp.Timing.Clock.Now;
                var promo = await _deliveryPromotionRepository.FirstOrDefaultAsync(p =>
                    p.IsActive && (!p.BranchId.HasValue || p.BranchId == branchId) &&
                    (!p.StartDate.HasValue || p.StartDate <= now) &&
                    (!p.EndDate.HasValue || p.EndDate >= now) &&
                    p.MinimumOrderAmount.HasValue);
                if (promo != null && promo.MinimumOrderAmount.HasValue)
                {
                    freeThreshold = promo.MinimumOrderAmount.Value;
                }

                var isRestaurant = branch != null && branch.BusinessType == BusinessType.Restaurant;

                var categories = await _categoryRepository.GetAll()
                    .Where(c => (c.BranchId == branchId || c.BranchId == 0) && c.IsActive && !c.IsDeleted)
                    .OrderBy(c => c.Name)
                    .Select(c => c.Name)
                    .ToListAsync();

                return new OnlineStoreHeaderDto
                {
                    BranchId = branch != null ? branch.Id : branchId,
                    BranchName = branch?.Name ?? $"Branch #{branchId}",
                    Address = branch?.InvoiceAddress,
                    Phone = branch?.InvoiceContactPhone,
                    CurrencyFormat = !string.IsNullOrWhiteSpace(branch?.CurrencyFormat) ? branch.CurrencyFormat : "PKR",
                    DateFormat = !string.IsNullOrWhiteSpace(branch?.DateFormat) ? branch.DateFormat : "dd/MM/yyyy",
                    BaseDeliveryFee = baseFee,
                    IsDeliveryEnabled = true,
                    FreeDeliveryThreshold = freeThreshold,
                    DeliveryAreaNote = areas.Any() ? $"{areas.Count} active delivery areas" : "Standard Delivery",
                    IsRestaurant = isRestaurant,
                    BusinessType = branch != null ? (int)branch.BusinessType : 1,
                    Latitude = branch?.Latitude,
                    Longitude = branch?.Longitude,
                    FacebookUrl = branch?.FacebookUrl,
                    InstagramUrl = branch?.InstagramUrl,
                    YoutubeUrl = branch?.YoutubeUrl,
                    Categories = categories
                };
            }
        }

        [AbpAllowAnonymous]
        public async Task<List<string>> GetOnlineCategoriesAsync(int? branchId)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var targetBranchId = branchId ?? 0;
                var categories = await _categoryRepository.GetAll()
                    .Where(c => (targetBranchId <= 0 || c.BranchId == targetBranchId || c.BranchId == 0) && c.IsActive && !c.IsDeleted)
                    .OrderBy(c => c.Name)
                    .Select(c => c.Name)
                    .ToListAsync();
                return categories;
            }
        }

        [AbpAllowAnonymous]
        public async Task<CustomerOrderDto> CreateOnlineOrderAsync(CreateCustomerOrderDto input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                if (input.Lines == null || !input.Lines.Any())
                {
                    throw new UserFriendlyException("Add at least one product line.");
                }

                int branchId;
                Branch branch = null;
                if (input.BranchId.HasValue && input.BranchId.Value > 0)
                {
                    branch = await _branchRepository.FirstOrDefaultAsync(b => b.Id == input.BranchId.Value);
                }

                if (branch == null)
                {
                    branch = await _branchRepository.FirstOrDefaultAsync(b => b.IsActive);
                    if (branch == null)
                    {
                        throw new UserFriendlyException("No active location available for online ordering.");
                    }
                }
                branchId = branch.Id;
                var tenantId = branch.TenantId ?? AbpSession.TenantId;

                int customerId = input.CustomerId;
                if (customerId <= 0)
                {
                    var name = string.IsNullOrWhiteSpace(input.CustomerName) ? "Online Customer" : input.CustomerName.Trim();
                    var mobile = string.IsNullOrWhiteSpace(input.CustomerMobile) ? "" : input.CustomerMobile.Trim();

                    Customer existingCustomer = null;
                    if (!string.IsNullOrEmpty(mobile))
                    {
                        existingCustomer = await _customerRepository.FirstOrDefaultAsync(c => c.Phone == mobile && c.BranchId == branchId);
                    }

                    if (existingCustomer != null)
                    {
                        customerId = existingCustomer.Id;
                    }
                    else
                    {
                        var newCust = new Customer
                        {
                            TenantId = tenantId,
                            BranchId = branchId,
                            Name = name,
                            Phone = mobile
                        };
                        customerId = await _customerRepository.InsertAndGetIdAsync(newCust);
                    }
                }

                var noteParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(input.Notes))
                {
                    noteParts.Add(input.Notes.Trim());
                }
                if (!string.IsNullOrWhiteSpace(input.DeliveryAddress))
                {
                    noteParts.Add($"📍 {input.DeliveryAddress.Trim()}");
                }
                if (!string.IsNullOrWhiteSpace(input.DeliveryLandmark))
                {
                    noteParts.Add($"🚩 {input.DeliveryLandmark.Trim()}");
                }
                if (!string.IsNullOrWhiteSpace(input.GoogleMapsLink) && !noteParts.Any(p => p.Contains("maps.google.com")))
                {
                    noteParts.Add($"🗺️ {input.GoogleMapsLink.Trim()}");
                }
                if (input.DistanceKm.HasValue && input.DistanceKm.Value > 0)
                {
                    noteParts.Add($"📏 Distance: {input.DistanceKm.Value:F1} km");
                }

                var finalNotes = string.Join(" | ", noteParts);
                if (finalNotes.Length > CustomerOrder.MaxNotesLength)
                {
                    finalNotes = finalNotes.Substring(0, CustomerOrder.MaxNotesLength);
                }

                var order = new CustomerOrder
                {
                    TenantId = tenantId,
                    BranchId = branchId,
                    CustomerId = customerId,
                    OrderDate = input.OrderDate == default ? Abp.Timing.Clock.Now : input.OrderDate,
                    Notes = finalNotes,
                    Status = CustomerOrderStatus.Pending,
                    Latitude = input.Latitude,
                    Longitude = input.Longitude,
                    DistanceKm = input.DistanceKm,
                    Lines = new List<CustomerOrderLine>()
                };

                decimal subTotal = 0;
                var orderedProductIds = new List<int>();
                foreach (var lineInput in input.Lines)
                {
                    if (lineInput.Quantity <= 0)
                    {
                        throw new UserFriendlyException("Quantity must be greater than zero.");
                    }

                    var product = await _productRepository.GetAsync(lineInput.ProductId);
                    var branchStock = await _branchStockRepository.FirstOrDefaultAsync(s => s.BranchId == branchId && s.ProductId == lineInput.ProductId);
                    var authoritativePrice = branchStock != null && branchStock.Price > 0 ? branchStock.Price : product.Price;
                    var lineTotal = lineInput.Quantity * authoritativePrice;
                    subTotal += lineTotal;
                    orderedProductIds.Add(product.Id);

                    order.Lines.Add(new CustomerOrderLine
                    {
                        TenantId = tenantId,
                        ProductId = lineInput.ProductId,
                        Quantity = lineInput.Quantity,
                        UnitPrice = authoritativePrice,
                        LineTotal = lineTotal
                    });
                }

                subTotal = Math.Round(subTotal, 2);

                // Server-side delivery area resolution & calculation
                DeliveryArea deliveryArea = null;
                if (input.DeliveryAreaId.HasValue && input.DeliveryAreaId.Value > 0)
                {
                    deliveryArea = await _deliveryAreaRepository.FirstOrDefaultAsync(
                        a => a.Id == input.DeliveryAreaId.Value && a.BranchId == branchId && a.IsActive);
                }

                if (deliveryArea == null && !string.IsNullOrWhiteSpace(input.DeliveryArea))
                {
                    var areaQuery = input.DeliveryArea.Trim().ToLower();
                    deliveryArea = await _deliveryAreaRepository.FirstOrDefaultAsync(
                        a => a.BranchId == branchId && a.IsActive && a.Name.ToLower() == areaQuery);
                }

                decimal defaultFee = 100m;
                string resolvedAreaName = input.DeliveryArea?.Trim();
                int? resolvedAreaId = null;

                if (deliveryArea != null)
                {
                    defaultFee = deliveryArea.DeliveryFee;
                    resolvedAreaName = deliveryArea.Name;
                    resolvedAreaId = deliveryArea.Id;

                    if (deliveryArea.MinimumOrderAmount > 0 && subTotal < deliveryArea.MinimumOrderAmount)
                    {
                        throw new UserFriendlyException(
                            $"Minimum order subtotal for delivery to {deliveryArea.Name} is PKR {deliveryArea.MinimumOrderAmount:N0}. Your current subtotal is PKR {subTotal:N0}.");
                    }
                }
                else
                {
                    var firstActive = await _deliveryAreaRepository.FirstOrDefaultAsync(a => a.BranchId == branchId && a.IsActive);
                    if (firstActive != null)
                    {
                        defaultFee = firstActive.DeliveryFee;
                        if (string.IsNullOrWhiteSpace(resolvedAreaName))
                        {
                            resolvedAreaName = firstActive.Name;
                            resolvedAreaId = firstActive.Id;
                        }
                    }
                }

                // Check free delivery promotions
                var now = Abp.Timing.Clock.Now;
                var activePromos = await _deliveryPromotionRepository.GetAll()
                    .Where(p => p.IsActive && (!p.BranchId.HasValue || p.BranchId == branchId))
                    .Where(p => !p.StartDate.HasValue || p.StartDate <= now)
                    .Where(p => !p.EndDate.HasValue || p.EndDate >= now)
                    .ToListAsync();

                bool isFreeDelivery = false;
                string freeDeliveryReason = null;
                int? appliedPromotionId = null;

                // Priority 1: Deal item with free delivery promotion
                var dealPromo = activePromos.FirstOrDefault(p => p.ProductId.HasValue && orderedProductIds.Contains(p.ProductId.Value));
                if (dealPromo != null)
                {
                    isFreeDelivery = true;
                    appliedPromotionId = dealPromo.Id;
                    freeDeliveryReason = $"Promotion: {dealPromo.Name} (Included deal item)";
                }
                else
                {
                    // Priority 2: Amount threshold promotion
                    var amountPromo = activePromos.FirstOrDefault(p => p.MinimumOrderAmount.HasValue && subTotal >= p.MinimumOrderAmount.Value);
                    if (amountPromo != null)
                    {
                        isFreeDelivery = true;
                        appliedPromotionId = amountPromo.Id;
                        freeDeliveryReason = $"Promotion: {amountPromo.Name} (Orders over PKR {amountPromo.MinimumOrderAmount:N0})";
                    }
                    else if (subTotal >= 1500m)
                    {
                        isFreeDelivery = true;
                        freeDeliveryReason = "Free Delivery on orders PKR 1,500 and above";
                    }
                }

                decimal finalDeliveryFee = isFreeDelivery ? 0 : defaultFee;

                order.SubTotal = subTotal;
                order.DeliveryFee = finalDeliveryFee;
                order.DeliveryAreaId = resolvedAreaId;
                order.DeliveryAreaName = resolvedAreaName;
                order.IsFreeDelivery = isFreeDelivery;
                order.FreeDeliveryReason = freeDeliveryReason;
                order.DeliveryPromotionId = appliedPromotionId;
                order.TotalAmount = Math.Round(subTotal + finalDeliveryFee, 2);

                var insertedId = await Repository.InsertAndGetIdAsync(order);
                await CurrentUnitOfWork.SaveChangesAsync();

                order.OrderNo = "ORD-" + insertedId.ToString("D5");
                await CurrentUnitOfWork.SaveChangesAsync();

                await EventBus.TriggerAsync(new CustomerOrderCreatedEventData(
                    insertedId,
                    order.OrderNo,
                    branchId,
                    input.CustomerName ?? "Online Customer",
                    order.TotalAmount));

                var settings = await _notificationSettingRepository.FirstOrDefaultAsync(
                    s => s.BranchId == branchId && s.TenantId == tenantId);

                if (settings != null && settings.IsEnabled && settings.NotifyOnOnlineOrder && !string.IsNullOrWhiteSpace(settings.Emails))
                {
                    var branchName = branch?.Name ?? $"Branch #{branchId}";
                    var safeBranchName = System.Net.WebUtility.HtmlEncode(branchName);
                    var safeOrderNo = System.Net.WebUtility.HtmlEncode(order.OrderNo);
                    var safeCustomerName = System.Net.WebUtility.HtmlEncode(input.CustomerName ?? "Guest");
                    
                    var template = await _emailTemplateRepository.FirstOrDefaultAsync(x => x.Code == EmailTemplateCodes.OnlineOrderCreated && x.TenantId == tenantId && x.IsActive);
                    if (template == null && tenantId.HasValue)
                    {
                        template = await _emailTemplateRepository.FirstOrDefaultAsync(x => x.Code == EmailTemplateCodes.OnlineOrderCreated && x.TenantId == null && x.IsActive);
                    }

                    var subjectTemplate = template?.Subject ?? $"New Online Order {safeOrderNo} Received";
                    var bodyTemplate = template?.BodyHtml;

                    if (string.IsNullOrWhiteSpace(bodyTemplate))
                    {
                        bodyTemplate = EmailTemplateDefaults.OnlineOrderCreatedBodyHtml();
                    }

                    var placeholders = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "BranchName", safeBranchName },
                        { "OrderNo", safeOrderNo },
                        { "CustomerName", safeCustomerName },
                        { "TotalAmount", order.TotalAmount.ToString("N2") },
                        { "FormattedDate", order.OrderDate.ToString("yyyy-MM-dd hh:mm tt") }
                    };

                    var subject = EmailTemplateRenderer.Render(subjectTemplate, placeholders);
                    var body = EmailTemplateRenderer.Render(bodyTemplate, placeholders);

                    await _branchEmailNotifier.SendEmailsAsync(settings.Emails, subject, body);
                }

                return await GetAsync(new EntityDto<int>(insertedId));
            }
        }

        public override async Task<CustomerOrderDto> CreateAsync(CreateCustomerOrderDto input)
        {
            CheckCreatePermission();

            if (input.Lines == null || !input.Lines.Any())
            {
                throw new UserFriendlyException("Add at least one product line.");
            }

            var customer = await _customerRepository.GetAsync(input.CustomerId);

            if (input.OrderDate == default)
            {
                input.OrderDate = Abp.Timing.Clock.Now;
            }

            var branchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();
            if (customer.BranchId != branchId)
            {
                throw new UserFriendlyException("Customer does not belong to the current location.");
            }

            var order = new CustomerOrder
            {
                TenantId = AbpSession.TenantId,
                BranchId = branchId,
                CustomerId = input.CustomerId,
                OrderDate = input.OrderDate,
                Notes = input.Notes,
                Status = CustomerOrderStatus.Pending,
                Lines = new List<CustomerOrderLine>()
            };

            decimal total = 0;
            foreach (var lineInput in input.Lines)
            {
                if (lineInput.Quantity <= 0)
                {
                    throw new UserFriendlyException("Quantity must be greater than zero.");
                }

                await _productRepository.GetAsync(lineInput.ProductId);

                var lineTotal = lineInput.Quantity * lineInput.UnitPrice;
                total += lineTotal;

                order.Lines.Add(new CustomerOrderLine
                {
                    ProductId = lineInput.ProductId,
                    Quantity = lineInput.Quantity,
                    UnitPrice = lineInput.UnitPrice,
                    LineTotal = lineTotal
                });
            }

            var deliveryFee = input.DeliveryCharges.GetValueOrDefault(0);
            order.SubTotal = Math.Round(total, 2);
            order.DeliveryFee = Math.Round(deliveryFee, 2);
            order.DeliveryAreaId = input.DeliveryAreaId;
            order.DeliveryAreaName = input.DeliveryArea;
            order.TotalAmount = Math.Round(total + deliveryFee, 2);

            await Repository.InsertAsync(order);
            await CurrentUnitOfWork.SaveChangesAsync();

            order.OrderNo = "ORD-" + order.Id.ToString("D6");
            await CurrentUnitOfWork.SaveChangesAsync();

            await EventBus.TriggerAsync(new CustomerOrderCreatedEventData(
                order.Id,
                order.OrderNo,
                branchId,
                customer.Name,
                order.TotalAmount));

            return await GetAsync(new EntityDto<int>(order.Id));
        }

        public override async Task<CustomerOrderDto> UpdateAsync(CustomerOrderDto input)
        {
            throw new UserFriendlyException("Orders cannot be edited. Reject and create a new order instead.");
        }

        public override async Task<PagedResultDto<CustomerOrderDto>> GetAllAsync(PagedCustomerOrderResultRequestDto input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                return await base.GetAllAsync(input);
            }
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                CheckDeletePermission();

                var order = await GetEntityByIdAsync(input.Id);
                if (order == null)
                {
                    throw new UserFriendlyException("Order not found.");
                }

                if (order.Status != CustomerOrderStatus.Pending)
                {
                    throw new UserFriendlyException("Only pending orders can be deleted.");
                }

                await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

                foreach (var line in order.Lines.ToList())
                {
                    await _lineRepository.DeleteAsync(line);
                }

                await Repository.DeleteAsync(order);
            }
        }

        [AbpAuthorize(PermissionNames.Pages_CustomerOrders_OverrideDeliveryFee)]
        public async Task<CustomerOrderDto> OverrideDeliveryFeeAsync(OverrideDeliveryFeeInput input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                if (input == null)
                {
                    throw new ArgumentNullException(nameof(input));
                }

                var order = await GetEntityByIdAsync(input.OrderId);
                if (order == null)
                {
                    throw new UserFriendlyException("Order not found.");
                }

                if (order.Status != CustomerOrderStatus.Pending)
                {
                    throw new UserFriendlyException("Delivery charges can only be modified while the order is Pending.");
                }

                if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length < 3)
                {
                    throw new UserFriendlyException("A valid reason (at least 3 characters) is required to adjust or waive delivery charges.");
                }

                if (input.NewDeliveryFee < 0)
                {
                    throw new UserFriendlyException("Delivery fee cannot be negative.");
                }

                await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

                if (!order.OriginalDeliveryFee.HasValue)
                {
                    order.OriginalDeliveryFee = order.DeliveryFee;
                }

                var newFee = Math.Round(input.NewDeliveryFee, 2);
                order.DeliveryFee = newFee;
                order.DeliveryFeeOverrideReason = input.Reason.Trim();
                order.DeliveryFeeOverriddenByUserId = AbpSession.UserId;
                order.DeliveryFeeOverrideTime = Abp.Timing.Clock.Now;

                if (newFee == 0)
                {
                    order.IsFreeDelivery = true;
                    order.FreeDeliveryReason = "Waived by Admin: " + input.Reason.Trim();
                }

                order.TotalAmount = Math.Round(order.SubTotal + order.DeliveryFee, 2);

                await Repository.UpdateAsync(order);
                await CurrentUnitOfWork.SaveChangesAsync();

                return await GetAsync(new EntityDto<int>(order.Id));
            }
        }

        public async Task<SaleDto> ApproveAsync(EntityDto<int> input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var order = await GetEntityByIdAsync(input.Id);
                if (order == null)
                {
                    throw new UserFriendlyException("Order not found.");
                }

                if (order.Status != CustomerOrderStatus.Pending)
                {
                    throw new UserFriendlyException("Only pending orders can be approved.");
                }

                if (order.Lines == null || !order.Lines.Any())
                {
                    throw new UserFriendlyException("Order has no product lines.");
                }

                await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

                var sale = await _saleAppService.CreateSaleInternalAsync(new CreateSaleDto
                {
                    CustomerId = order.CustomerId,
                    SaleDate = Abp.Timing.Clock.Now,
                    PaymentType = PaymentTypes.Credit,
                    DeliveryCharges = order.DeliveryFee,
                    DeliveryAreaName = order.DeliveryAreaName,
                    Notes = string.IsNullOrWhiteSpace(order.Notes)
                        ? "From order " + (order.OrderNo ?? ("#" + order.Id))
                        : order.Notes + " (Order " + (order.OrderNo ?? ("#" + order.Id)) + ")",
                    Lines = order.Lines.Select(x => new CreateSaleLineDto
                    {
                        ProductId = x.ProductId,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice
                    }).ToList()
                });

                var branch = await _branchRepository.FirstOrDefaultAsync(order.BranchId);
                var isRestaurant = branch != null && branch.BusinessType == BusinessType.Restaurant;

                if (isRestaurant)
                {
                    // Automatic Dynamic Rider Assignment ONLY for Restaurant branches
                    // Select the available rider who has been free for the longest time
                    var availableRiders = await GetAvailableRidersAsync(order.BranchId, order.Id);
                    var longestIdleRider = availableRiders
                        .Where(r => !r.IsBusy)
                        .OrderByDescending(r => r.IdleMinutes)
                        .ThenBy(r => r.TodayDeliveredCount)
                        .ThenBy(r => r.RiderId)
                        .FirstOrDefault();

                    if (longestIdleRider != null)
                    {
                        order.RiderId = longestIdleRider.RiderId;
                        order.RiderName = longestIdleRider.RiderName;
                        order.RiderPhone = longestIdleRider.RiderPhone;
                        order.RiderAssignedTime = DateTime.UtcNow;
                        order.Status = CustomerOrderStatus.InProgress; // Out for delivery
                    }
                    else
                    {
                        order.Status = CustomerOrderStatus.Approved;
                    }
                }
                else
                {
                    order.Status = CustomerOrderStatus.Approved;
                }

                order.SaleId = sale.Id;
                await CurrentUnitOfWork.SaveChangesAsync();

                await EventBus.TriggerAsync(new CustomerOrderStatusChangedEventData(
                    order.Id,
                    order.OrderNo,
                    order.BranchId,
                    order.Status));

                return sale;
            }
        }

        public async Task RejectAsync(EntityDto<int> input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var order = await GetEntityByIdAsync(input.Id);
                if (order == null)
                {
                    throw new UserFriendlyException("Order not found.");
                }

                if (order.Status != CustomerOrderStatus.Pending)
                {
                    throw new UserFriendlyException("Only pending orders can be rejected.");
                }

                await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

                order.Status = CustomerOrderStatus.Rejected;
                await CurrentUnitOfWork.SaveChangesAsync();

                await EventBus.TriggerAsync(new CustomerOrderStatusChangedEventData(
                    order.Id,
                    order.OrderNo,
                    order.BranchId,
                    CustomerOrderStatus.Rejected));
            }
        }

        public async Task DeliverAsync(EntityDto<int> input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var order = await GetEntityByIdAsync(input.Id);
                if (order == null)
                {
                    throw new UserFriendlyException("Order not found.");
                }

                if (order.Status != CustomerOrderStatus.Approved && order.Status != CustomerOrderStatus.InProgress)
                {
                    throw new UserFriendlyException("Only approved or in-progress orders can be marked as delivered.");
                }

                await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

                order.Status = CustomerOrderStatus.Delivered;
                await CurrentUnitOfWork.SaveChangesAsync();

                await EventBus.TriggerAsync(new CustomerOrderStatusChangedEventData(
                    order.Id,
                    order.OrderNo,
                    order.BranchId,
                    CustomerOrderStatus.Delivered));
            }
        }

        protected override IQueryable<CustomerOrder> CreateFilteredQuery(PagedCustomerOrderResultRequestDto input)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);

            return Repository.GetAllIncluding(x => x.Customer, x => x.Sale, x => x.Lines, x => x.Branch)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.OrderNo != null && x.OrderNo.Contains(input.Keyword))
                         || (x.Notes != null && x.Notes.Contains(input.Keyword))
                         || (x.Customer != null && x.Customer.Name.Contains(input.Keyword))
                         || (x.Sale != null && x.Sale.InvoiceNo != null && x.Sale.InvoiceNo.Contains(input.Keyword)));
        }

        protected override async Task<CustomerOrder> GetEntityByIdAsync(int id)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                return await AsyncQueryableExecuter.FirstOrDefaultAsync(
                    Repository.GetAllIncluding(x => x.Customer, x => x.Sale, x => x.Lines, x => x.Branch)
                        .Where(x => x.Id == id));
            }
        }

        public override async Task<CustomerOrderDto> GetAsync(EntityDto<int> input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var entity = await GetEntityByIdAsync(input.Id);
                if (entity == null)
                {
                    throw new UserFriendlyException("Order not found.");
                }

                foreach (var line in entity.Lines)
                {
                    line.Product = await _productRepository.FirstOrDefaultAsync(line.ProductId);
                }

                return MapToEntityDto(entity);
            }
        }

        protected override CustomerOrderDto MapToEntityDto(CustomerOrder entity)
        {
            string overrideUserName = null;
            if (entity.DeliveryFeeOverriddenByUserId.HasValue)
            {
                var overrideUser = _userRepository.FirstOrDefault(entity.DeliveryFeeOverriddenByUserId.Value);
                overrideUserName = !string.IsNullOrWhiteSpace(overrideUser?.FullName)
                    ? overrideUser.FullName
                    : overrideUser?.UserName;
            }

            var dto = new CustomerOrderDto
            {
                Id = entity.Id,
                BranchId = entity.BranchId,
                BranchName = entity.Branch?.Name,
                CustomerId = entity.CustomerId,
                CustomerName = entity.Customer?.Name,
                CustomerPhone = entity.Customer?.Phone,
                CustomerAddress = entity.Customer?.Address,
                OrderDate = entity.OrderDate,
                OrderNo = entity.OrderNo,
                Status = entity.Status,
                StatusName = (entity.Status == CustomerOrderStatus.Approved || entity.Status == CustomerOrderStatus.InProgress)
                    ? "In Progress"
                    : entity.Status.ToString(),
                SubTotal = entity.SubTotal,
                DeliveryFee = entity.DeliveryFee,
                DeliveryAreaId = entity.DeliveryAreaId,
                DeliveryAreaName = entity.DeliveryAreaName,
                IsFreeDelivery = entity.IsFreeDelivery,
                FreeDeliveryReason = entity.FreeDeliveryReason,
                OriginalDeliveryFee = entity.OriginalDeliveryFee,
                DeliveryFeeOverrideReason = entity.DeliveryFeeOverrideReason,
                DeliveryFeeOverriddenByName = overrideUserName,
                DeliveryFeeOverrideTime = entity.DeliveryFeeOverrideTime,
                TotalAmount = entity.TotalAmount,
                Notes = entity.Notes,
                SaleId = entity.SaleId,
                SaleInvoiceNo = entity.Sale?.InvoiceNo,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                DistanceKm = entity.DistanceKm,
                RiderId = entity.RiderId,
                RiderName = entity.RiderName,
                RiderPhone = entity.RiderPhone,
                RiderAssignedTime = entity.RiderAssignedTime,
                Lines = new List<CustomerOrderLineDto>()
            };

            if (entity.Lines != null)
            {
                foreach (var line in entity.Lines)
                {
                    dto.Lines.Add(new CustomerOrderLineDto
                    {
                        Id = line.Id,
                        OrderId = line.OrderId,
                        ProductId = line.ProductId,
                        ProductName = line.Product?.Name,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        LineTotal = line.LineTotal
                    });
                }
            }

            return dto;
        }

        [AbpAllowAnonymous]
        public async Task<CustomerOrderDto> TrackOrderAsync(string orderNo)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                if (string.IsNullOrWhiteSpace(orderNo))
                {
                    throw new UserFriendlyException("Please provide a valid Order Number.");
                }

                var clean = orderNo.Trim();
                var query = Repository.GetAllIncluding(
                    x => x.Customer,
                    x => x.Sale,
                    x => x.Lines,
                    x => x.Branch
                ).AsNoTracking();

                CustomerOrder order = await query.FirstOrDefaultAsync(x => x.OrderNo == clean);

                if (order == null)
                {
                    order = await query.FirstOrDefaultAsync(x => x.OrderNo.ToLower() == clean.ToLower());
                }

                if (order == null)
                {
                    var numericPart = clean.Replace("#", "").Replace("ORD-", "").Replace("ord-", "").Trim();
                    if (int.TryParse(numericPart, out int orderId))
                    {
                        order = await query.FirstOrDefaultAsync(x => x.Id == orderId || x.OrderNo == $"ORD-{orderId:D5}" || x.OrderNo == $"ORD-{orderId:D6}");
                    }
                }

                if (order == null && clean.Length >= 7)
                {
                    order = await query.OrderByDescending(x => x.CreationTime)
                        .FirstOrDefaultAsync(x => x.Customer != null && x.Customer.Phone == clean);
                }

                if (order == null)
                {
                    throw new UserFriendlyException($"No order found matching '{clean}'. Please verify your Order Number and try again.");
                }

                var dto = MapToEntityDto(order);

                // Populate product names if missing
                if (dto.Lines != null && dto.Lines.Any())
                {
                    var productIds = dto.Lines.Select(l => l.ProductId).Distinct().ToList();
                    var products = await _productRepository.GetAll()
                        .Where(p => productIds.Contains(p.Id))
                        .Select(p => new { p.Id, p.Name })
                        .ToDictionaryAsync(p => p.Id, p => p.Name);

                    foreach (var line in dto.Lines)
                    {
                        if (string.IsNullOrEmpty(line.ProductName) && products.TryGetValue(line.ProductId, out var pName))
                        {
                            line.ProductName = pName;
                        }
                    }
                }

                return dto;
            }
        }

        public async Task<List<DeliveryRiderStatusDto>> GetAvailableRidersAsync(int? branchId, int? currentOrderId = null)
        {
            var targetBranchId = branchId ?? await _branchAccessChecker.GetEffectiveBranchIdAsync();

            var branch = targetBranchId.HasValue ? await _branchRepository.FirstOrDefaultAsync(targetBranchId.Value) : null;
            if (branch != null && branch.BusinessType != BusinessType.Restaurant)
            {
                return new List<DeliveryRiderStatusDto>();
            }

            // Find staff whose Designation is "Delivery Rider" (or contains "rider" or "delivery")
            var riders = await _staffRepository.GetAll()
                .Where(s => s.IsActive && !s.IsDeleted)
                .WhereIf(targetBranchId.HasValue, s => s.BranchId == targetBranchId.Value)
                .Where(s => s.Designation != null &&
                            (s.Designation.ToLower().Contains("rider") || s.Designation.ToLower().Contains("delivery")))
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Find all active orders currently assigned to a rider that are not yet Delivered or Rejected
            // Exclude currentOrderId so reassignment doesn't treat the rider as busy on the very order being edited
            var activeOrders = await Repository.GetAll()
                .Where(o => o.RiderId.HasValue &&
                            o.Status != CustomerOrderStatus.Delivered &&
                            o.Status != CustomerOrderStatus.Rejected)
                .WhereIf(currentOrderId.HasValue, o => o.Id != currentOrderId.Value)
                .Select(o => new { o.Id, o.OrderNo, RiderId = o.RiderId.Value })
                .ToListAsync();

            var activeByRider = activeOrders
                .GroupBy(o => o.RiderId)
                .ToDictionary(g => g.Key, g => g.First());

            var todayLocal = DateTime.Today;
            var todayUtc = DateTime.UtcNow.Date;

            // Query previous order times for each rider to determine idle time and today's delivered count
            var riderIds = riders.Select(r => r.Id).ToList();
            var riderOrders = await Repository.GetAll()
                .Where(o => o.RiderId.HasValue && riderIds.Contains(o.RiderId.Value))
                .WhereIf(currentOrderId.HasValue, o => o.Id != currentOrderId.Value)
                .Select(o => new
                {
                    RiderId = o.RiderId.Value,
                    Status = o.Status,
                    DeliveryTime = o.Status == CustomerOrderStatus.Delivered && o.LastModificationTime.HasValue
                        ? o.LastModificationTime.Value
                        : (o.RiderAssignedTime ?? o.OrderDate),
                    OrderTime = o.Status == CustomerOrderStatus.Delivered && o.LastModificationTime.HasValue
                        ? o.LastModificationTime.Value
                        : (o.RiderAssignedTime ?? o.OrderDate)
                })
                .ToListAsync();

            var lastOrderTimes = riderOrders
                .GroupBy(o => o.RiderId)
                .ToDictionary(g => g.Key, g => g.Max(x => x.OrderTime));

            var todayDeliveredByRider = riderOrders
                .Where(o => o.Status == CustomerOrderStatus.Delivered &&
                            (o.DeliveryTime.Date == todayLocal || o.DeliveryTime.Date == todayUtc))
                .GroupBy(o => o.RiderId)
                .ToDictionary(g => g.Key, g => g.Count());

            var nowUtc = DateTime.UtcNow;

            return riders.Select(r =>
            {
                var isBusy = activeByRider.TryGetValue(r.Id, out var busyOrder);
                var hasLastOrder = lastOrderTimes.TryGetValue(r.Id, out var lastTime);
                DateTime? lastOrderTime = hasLastOrder ? (DateTime?)lastTime : null;
                string lastOrderText = "No previous orders";
                int idleMins = int.MaxValue;

                if (lastOrderTime.HasValue)
                {
                    var diff = nowUtc - lastOrderTime.Value.ToUniversalTime();
                    if (diff.TotalMinutes < 0) diff = TimeSpan.Zero;
                    idleMins = (int)diff.TotalMinutes;

                    if (diff.TotalMinutes < 1)
                    {
                        lastOrderText = "Last order: Just now";
                    }
                    else if (diff.TotalMinutes < 60)
                    {
                        int mins = Math.Max(1, (int)diff.TotalMinutes);
                        lastOrderText = $"Last order: {mins} minute{(mins == 1 ? "" : "s")} ago";
                    }
                    else if (diff.TotalHours < 24)
                    {
                        int hours = (int)diff.TotalHours;
                        lastOrderText = $"Last order: {hours} hour{(hours == 1 ? "" : "s")} ago";
                    }
                    else if (diff.TotalDays < 2)
                    {
                        lastOrderText = "Last order: Yesterday";
                    }
                    else
                    {
                        int days = (int)diff.TotalDays;
                        lastOrderText = $"Last order: {days} days ago";
                    }
                }

                int todayDelivered = todayDeliveredByRider.TryGetValue(r.Id, out var count) ? count : 0;
                string todayDeliveredText;
                if (todayDelivered == 0)
                {
                    todayDeliveredText = "No order delivered today";
                }
                else if (todayDelivered == 1)
                {
                    todayDeliveredText = "1 order delivered today";
                }
                else
                {
                    todayDeliveredText = $"{todayDelivered} orders delivered today";
                }

                return new DeliveryRiderStatusDto
                {
                    RiderId = r.Id,
                    RiderName = r.Name,
                    RiderPhone = r.Phone,
                    EmployeeCode = r.EmployeeCode,
                    IsBusy = isBusy,
                    BusyOrderId = isBusy ? busyOrder.Id : null,
                    BusyOrderNo = isBusy ? (!string.IsNullOrWhiteSpace(busyOrder.OrderNo) ? busyOrder.OrderNo : $"#{busyOrder.Id}") : null,
                    LastOrderTime = lastOrderTime,
                    LastOrderTimeText = lastOrderText,
                    IdleMinutes = idleMins,
                    TodayDeliveredCount = todayDelivered,
                    TodayDeliveredText = todayDeliveredText
                };
            })
            .OrderBy(r => r.IsBusy ? 1 : 0) // Available riders first
            .ThenByDescending(r => r.IdleMinutes) // Longest free / idle rider first
            .ThenBy(r => r.TodayDeliveredCount) // Fewer orders delivered today as tie breaker
            .ThenBy(r => r.RiderName)
            .ToList();
        }

        public async Task<CustomerOrderDto> AssignRiderAsync(AssignRiderDto input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var order = await GetEntityByIdAsync(input.OrderId);
                if (order == null)
                {
                    throw new UserFriendlyException("Order not found.");
                }

                var branch = await _branchRepository.FirstOrDefaultAsync(order.BranchId);
                if (branch != null && branch.BusinessType != BusinessType.Restaurant)
                {
                    throw new UserFriendlyException("Rider assignment is only available for Restaurant branches.");
                }

                var rider = await _staffRepository.FirstOrDefaultAsync(input.RiderId);
                if (rider == null || !rider.IsActive)
                {
                    throw new UserFriendlyException("Selected delivery rider is invalid or inactive.");
                }

                // Check if this rider is already busy delivering another order that is not finished
                var busyOrder = await Repository.FirstOrDefaultAsync(o =>
                    o.RiderId == input.RiderId &&
                    o.Id != input.OrderId &&
                    o.Status != CustomerOrderStatus.Delivered &&
                    o.Status != CustomerOrderStatus.Rejected);

                if (busyOrder != null)
                {
                    var busyNo = !string.IsNullOrWhiteSpace(busyOrder.OrderNo) ? busyOrder.OrderNo : $"#{busyOrder.Id}";
                    throw new UserFriendlyException($"Rider '{rider.Name}' is already delivering Order #{busyNo}. Cannot assign until that delivery is completed or cancelled.");
                }

                order.RiderId = rider.Id;
                order.RiderName = rider.Name;
                order.RiderPhone = rider.Phone;
                order.RiderAssignedTime = DateTime.UtcNow;

                // Advance status to InProgress (Out for delivery) if not already delivered/rejected
                if (order.Status == CustomerOrderStatus.Approved || order.Status == CustomerOrderStatus.Pending)
                {
                    order.Status = CustomerOrderStatus.InProgress;
                }

                await CurrentUnitOfWork.SaveChangesAsync();
                return MapToEntityDto(order);
            }
        }
    }
}
