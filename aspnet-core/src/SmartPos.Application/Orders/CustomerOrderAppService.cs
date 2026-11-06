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
using Abp.Runtime.Caching;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Caching;
using SmartPos.Addresses;
using SmartPos.Customers;
using SmartPos.Customers.Dto;
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
using SmartPos.Deals;
using SmartPos.Restaurant;
using Abp.Timing;

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
        private readonly SavedAddressManager _savedAddresses;
        private readonly IRepository<CustomerFavorite> _customerFavoriteRepository;
        private readonly IRepository<Staff> _staffRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly ICacheManager _cacheManager;
        private readonly ITokenNumberManager _tokenNumberManager;
        private readonly IRepository<Deal> _dealRepository;
        private readonly IRepository<DealDetail> _dealDetailRepository;
        private readonly IRepository<RestaurantOrder> _restaurantOrderRepository;

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
            SavedAddressManager savedAddresses,
            IRepository<CustomerFavorite> customerFavoriteRepository,
            IRepository<Staff> staffRepository,
            IRepository<Category> categoryRepository,
            ICacheManager cacheManager,
            ITokenNumberManager tokenNumberManager,
            IRepository<Deal> dealRepository,
            IRepository<DealDetail> dealDetailRepository,
            IRepository<RestaurantOrder> restaurantOrderRepository)
            : base(repository)
        {
            _lineRepository = lineRepository;
            _tokenNumberManager = tokenNumberManager;
            _dealRepository = dealRepository;
            _dealDetailRepository = dealDetailRepository;
            _restaurantOrderRepository = restaurantOrderRepository;
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
            _savedAddresses = savedAddresses;
            _customerFavoriteRepository = customerFavoriteRepository;
            _staffRepository = staffRepository;
            _categoryRepository = categoryRepository;
            _cacheManager = cacheManager;
            
            CreatePermissionName = PermissionNames.Pages_CustomerOrders_Create;
            UpdatePermissionName = PermissionNames.Pages_CustomerOrders_Edit;
            DeletePermissionName = PermissionNames.Pages_CustomerOrders_Delete;
            GetPermissionName = PermissionNames.Pages_CustomerOrders;
            GetAllPermissionName = PermissionNames.Pages_CustomerOrders;
        }

        [AbpAllowAnonymous]
        public async Task<List<OnlineProductDto>> GetOnlineCatalogAsync(int? branchId)
        {
            var resolvedBranchId = branchId.GetValueOrDefault();
            var cacheKey = SmartPosAppCaches.BranchKey(resolvedBranchId > 0 ? resolvedBranchId : 0, "Catalog");
            var cache = SmartPosAppCaches.Typed<List<OnlineProductDto>>(
                _cacheManager,
                SmartPosAppCaches.OnlineCatalog,
                TimeSpan.FromMinutes(2));

            return await cache.GetAsync(cacheKey, async _ =>
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
                        targetBranch = await _branchRepository.GetAll()
                            .AsNoTracking()
                            .Where(b => b.IsActive)
                            .OrderByDescending(b => b.BusinessType == BusinessType.Restaurant)
                            .ThenBy(b => b.Id)
                            .FirstOrDefaultAsync();
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
                        .Where(p => p.IsVisibleOnline)
                        .Where(p => (p.BranchId == targetBranchId) || (p.BranchId == 0 && (!p.TenantId.HasValue || p.TenantId == targetBranch.TenantId)))
                        .WhereVisibleToBranch(_branchStockRepository.GetAll(), targetBranchId)
                        .Where(p => (p.Barcode == null || !p.Barcode.StartsWith("GROCERY-")) && p.Name != "Grocery")
                        .Where(p => p.Category == null || p.Category.Name != "Permanent Grocery")
                        .ToListAsync();

                    var branchInfo = await _branchStockRepository.GetAll()
                        .AsNoTracking()
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
                            IsDeal = p.IsDeal || (p.Category != null && p.Category.Name.ToLower().Contains("deal")),
                            CardTag = p.CardTag
                        })
                        .ToList();
                }
            });
        }

        [AbpAllowAnonymous]
        public async Task<OnlineProductDto> GetOnlineProductAsync(int id, int? branchId)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var product = await _productRepository.GetAllIncluding(p => p.Category, p => p.Unit, p => p.Brand)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsVisibleOnline);

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
                    IsDeal = product.IsDeal || (product.Category != null && product.Category.Name.ToLower().Contains("deal")),
                    CardTag = product.CardTag
                };
            }
        }

        [AbpAllowAnonymous]
        public async Task<List<OnlineBranchDto>> GetOnlineBranchesAsync()
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                // Public storefront: list every active branch so the customer can pick (no hard-coded branch).
                return await _branchRepository.GetAll()
                    .AsNoTracking()
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .ThenBy(b => b.Id)
                    .Select(b => new OnlineBranchDto
                    {
                        Id = b.Id,
                        Name = b.Name,
                        IsRestaurant = b.BusinessType == BusinessType.Restaurant,
                        Address = b.InvoiceAddress
                    })
                    .ToListAsync();
            }
        }

        [AbpAllowAnonymous]
        public async Task<OnlineStoreHeaderDto> GetOnlineStoreHeaderAsync(int branchId)
        {
            var cacheKey = SmartPosAppCaches.BranchKey(branchId, "Header");
            var cache = SmartPosAppCaches.Typed<OnlineStoreHeaderDto>(
                _cacheManager,
                SmartPosAppCaches.OnlineStoreHeader,
                TimeSpan.FromMinutes(10));

            return await cache.GetAsync(cacheKey, async _ =>
            {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var branch = await _branchRepository.FirstOrDefaultAsync(b => b.Id == branchId);
                var areas = await _deliveryAreaRepository.GetAll()
                    .AsNoTracking()
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
                    .AsNoTracking()
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
                    ImagePath = branch?.ImagePath,
                    Categories = categories
                };
            }
            });
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
                        if (!string.IsNullOrWhiteSpace(name) &&
                            !string.Equals(existingCustomer.Name, name, StringComparison.Ordinal))
                        {
                            existingCustomer.Name = name;
                        }
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
                    noteParts.Add($"?? {input.DeliveryAddress.Trim()}");
                }
                if (!string.IsNullOrWhiteSpace(input.DeliveryLandmark))
                {
                    noteParts.Add($"?? {input.DeliveryLandmark.Trim()}");
                }
                if (!string.IsNullOrWhiteSpace(input.GoogleMapsLink) && !noteParts.Any(p => p.Contains("maps.google.com")))
                {
                    noteParts.Add($"??? {input.GoogleMapsLink.Trim()}");
                }
                if (input.DistanceKm.HasValue && input.DistanceKm.Value > 0)
                {
                    noteParts.Add($"?? Distance: {input.DistanceKm.Value:F1} km");
                }

                var paymentMethod = NormalizePaymentMethod(input.PaymentMethod);
                noteParts.Add($"?? Payment: {FormatPaymentMethodLabel(paymentMethod)}");

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
                    PaymentMethod = paymentMethod,
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
                    if (!product.IsVisibleOnline)
                    {
                        throw new UserFriendlyException(
                            $"'{product.Name}' is not available on the online store.");
                    }

                    var branchStock = await _branchStockRepository.FirstOrDefaultAsync(s => s.BranchId == branchId && s.ProductId == lineInput.ProductId);
                    var authoritativePrice = branchStock != null && branchStock.Price > 0 ? branchStock.Price : product.Price;

                    // Non-restaurant online orders must not oversell available branch stock.
                    if (branch.BusinessType != BusinessType.Restaurant)
                    {
                        var available = branchStock?.Quantity ?? 0m;
                        if (available < lineInput.Quantity)
                        {
                            throw new UserFriendlyException(
                                $"Insufficient stock for '{product.Name}'. Available: {available:0.##}.");
                        }
                    }

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
                order.TokenNo = await _tokenNumberManager.AllocateNextTokenNoAsync(branchId);

                var insertedId = await Repository.InsertAndGetIdAsync(order);
                await CurrentUnitOfWork.SaveChangesAsync();

                order.OrderNo = "ORD-" + insertedId.ToString("D5");
                await CurrentUnitOfWork.SaveChangesAsync();

                // Purchaser checkout: persist address + zone onto the customer profile.
                await UpsertSavedAddressForCustomerAsync(
                    customerId,
                    tenantId,
                    input.DeliveryAddress,
                    input.DeliveryLandmark,
                    resolvedAreaId,
                    resolvedAreaName,
                    input.Latitude,
                    input.Longitude,
                    setAsDefault: true);

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

                // Do not call GetAsync here: it is class-authorized and rejects anonymous guests.
                return await GetOnlineOrderDtoAsync(insertedId);
            }
        }

        /// <summary>
        /// Loads an order DTO for anonymous online flows without going through authorized GetAsync.
        /// </summary>
        private async Task<CustomerOrderDto> GetOnlineOrderDtoAsync(int orderId)
        {
            var entity = await GetEntityByIdAsync(orderId);
            if (entity == null)
            {
                throw new UserFriendlyException("Order not found.");
            }

            if (entity.Lines != null)
            {
                foreach (var line in entity.Lines)
                {
                    line.Product = await _productRepository.FirstOrDefaultAsync(line.ProductId);
                }
            }

            return MapToEntityDto(entity);
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

            // Staff-created order: also save address + zone when provided.
            await UpsertSavedAddressForCustomerAsync(
                customer.Id,
                customer.TenantId ?? AbpSession.TenantId,
                input.DeliveryAddress,
                input.DeliveryLandmark,
                input.DeliveryAreaId,
                input.DeliveryArea,
                input.Latitude,
                input.Longitude,
                setAsDefault: true);

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
                        // Ensure phone is captured even if the availability DTO omitted it.
                        if (string.IsNullOrWhiteSpace(order.RiderPhone) && longestIdleRider.RiderId > 0)
                        {
                            var riderStaff = await _staffRepository.FirstOrDefaultAsync(longestIdleRider.RiderId);
                            order.RiderPhone = riderStaff?.Phone;
                            if (string.IsNullOrWhiteSpace(order.RiderName))
                            {
                                order.RiderName = riderStaff?.Name;
                            }
                        }
                        order.RiderAssignedTime = Clock.Now;
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

                // Restaurant online orders go to Kitchen Display (KOT) for preparing.
                if (isRestaurant)
                {
                    await CreateKitchenTicketForOnlineOrderAsync(order);
                }

                await CurrentUnitOfWork.SaveChangesAsync();

                await EventBus.TriggerAsync(new CustomerOrderStatusChangedEventData(
                    order.Id,
                    order.OrderNo,
                    order.BranchId,
                    order.Status));

                return null;
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

        /// <summary>
        /// Admin/staff: mark food as delivered (no sale). Payment is posted via CompleteAsync.
        /// </summary>
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
                await SettleKitchenTicketForOnlineOrderAsync(order.Id);
                await CurrentUnitOfWork.SaveChangesAsync();

                await EventBus.TriggerAsync(new CustomerOrderStatusChangedEventData(
                    order.Id,
                    order.OrderNo,
                    order.BranchId,
                    CustomerOrderStatus.Delivered));
            }
        }

        /// <summary>
        /// Admin only: mark order Completed and insert payment/sale into the system.
        /// Prefer completing after rider (or staff) marked Delivered.
        /// </summary>
        public async Task CompleteAsync(CompleteCustomerOrderInput input)
        {
            if (input == null || input.OrderId <= 0)
            {
                throw new UserFriendlyException("Order is required.");
            }

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var order = await GetEntityByIdAsync(input.OrderId);
                if (order == null)
                {
                    throw new UserFriendlyException("Order not found.");
                }

                if (order.Status == CustomerOrderStatus.Completed)
                {
                    throw new UserFriendlyException("This order is already completed.");
                }

                if (order.Status == CustomerOrderStatus.Rejected || order.Status == CustomerOrderStatus.Pending)
                {
                    throw new UserFriendlyException("Pending or rejected orders cannot be completed.");
                }

                // Delivered is the normal path; Approved/InProgress allowed for counter/admin-handled orders.
                if (order.Status != CustomerOrderStatus.Delivered &&
                    order.Status != CustomerOrderStatus.Approved &&
                    order.Status != CustomerOrderStatus.InProgress)
                {
                    throw new UserFriendlyException("Only delivered (or in-progress) orders can be completed.");
                }

                await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

                if (order.Status != CustomerOrderStatus.Delivered)
                {
                    await SettleKitchenTicketForOnlineOrderAsync(order.Id);
                }

                order.Status = CustomerOrderStatus.Completed;
                await CurrentUnitOfWork.SaveChangesAsync();

                if (order.SaleId == null || input.PaymentReceivedAmount.HasValue)
                {
                    await _saleAppService.CreateSaleForCustomerOrderAsync(order.Id, input.PaymentReceivedAmount);
                }

                await EventBus.TriggerAsync(new CustomerOrderStatusChangedEventData(
                    order.Id,
                    order.OrderNo,
                    order.BranchId,
                    CustomerOrderStatus.Completed));
            }
        }

        /// <summary>
        /// Creates an open Delivery restaurant ticket (sent to kitchen) so Kitchen Display can prepare the order.
        /// </summary>
        private async Task CreateKitchenTicketForOnlineOrderAsync(CustomerOrder order)
        {
            if (order == null || order.Lines == null || !order.Lines.Any())
            {
                return;
            }

            var alreadySent = await _restaurantOrderRepository.GetAll()
                .AsNoTracking()
                .AnyAsync(o =>
                    o.CustomerOrderId == order.Id &&
                    (o.Status == RestaurantOrderStatus.Open || o.Status == RestaurantOrderStatus.Billed));
            if (alreadySent)
            {
                return;
            }

            var productIds = order.Lines.Select(l => l.ProductId).Distinct().ToList();
            var productNames = await _productRepository.GetAll()
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            var now = Clock.Now;
            var tokenNo = order.TokenNo;
            if (!tokenNo.HasValue)
            {
                tokenNo = await _tokenNumberManager.AllocateNextTokenNoAsync(order.BranchId);
                order.TokenNo = tokenNo;
            }

            // Kitchen only needs prep context — never copy delivery address / maps from customer notes.
            var noteParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(order.PaymentMethod))
            {
                noteParts.Add($"Payment: {FormatPaymentMethodLabel(order.PaymentMethod)}");
            }
            if (!string.IsNullOrWhiteSpace(order.DeliveryAreaName))
            {
                noteParts.Add($"Area: {order.DeliveryAreaName.Trim()}");
            }

            var kitchenOrder = new RestaurantOrder
            {
                TenantId = order.TenantId,
                BranchId = order.BranchId,
                OrderNo = string.IsNullOrWhiteSpace(order.OrderNo)
                    ? $"ONL-{now:yyMMddHHmm}-{order.Id}"
                    : order.OrderNo,
                TokenNo = tokenNo,
                OrderType = OrderType.Delivery,
                Status = RestaurantOrderStatus.Open,
                KitchenStatus = RestaurantKitchenStatus.InProgress,
                CustomerId = order.CustomerId,
                CustomerOrderId = order.Id,
                WaiterName = "Online",
                TableNo = "Online Delivery",
                Notes = noteParts.Count > 0 ? string.Join(" · ", noteParts) : null,
                SubTotal = order.SubTotal,
                GrandTotal = order.TotalAmount,
                LastPrintedTime = now,
                LastPrintedBy = "Online Approve",
                Lines = new List<RestaurantOrderLine>()
            };

            foreach (var line in order.Lines.Where(l => l.Quantity > 0))
            {
                productNames.TryGetValue(line.ProductId, out var productName);
                kitchenOrder.Lines.Add(new RestaurantOrderLine
                {
                    TenantId = order.TenantId,
                    ProductId = line.ProductId,
                    ProductName = string.IsNullOrWhiteSpace(productName)
                        ? $"Product #{line.ProductId}"
                        : productName,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    LineTotal = line.LineTotal,
                    IsSentToKitchen = true,
                    PrintedQuantity = line.Quantity,
                    SentToKitchenTime = now
                });
            }

            if (!kitchenOrder.Lines.Any())
            {
                return;
            }

            await _restaurantOrderRepository.InsertAsync(kitchenOrder);
        }

        private async Task SettleKitchenTicketForOnlineOrderAsync(int customerOrderId)
        {
            var tickets = await _restaurantOrderRepository.GetAll()
                .Where(o =>
                    o.CustomerOrderId == customerOrderId &&
                    (o.Status == RestaurantOrderStatus.Open || o.Status == RestaurantOrderStatus.Billed))
                .ToListAsync();

            if (tickets.Count == 0)
            {
                return;
            }

            var now = Clock.Now;
            foreach (var ticket in tickets)
            {
                ticket.Status = RestaurantOrderStatus.Settled;
                ticket.SettledTime = now;
                if (ticket.KitchenStatus != RestaurantKitchenStatus.ReadyToPick)
                {
                    ticket.KitchenStatus = RestaurantKitchenStatus.ReadyToPick;
                    ticket.KitchenReadyTime ??= now;
                }
                await _restaurantOrderRepository.UpdateAsync(ticket);
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
                CustomerAddress = GetDefaultSavedAddressText(entity.CustomerId),
                OrderDate = entity.OrderDate,
                OrderNo = entity.OrderNo,
                TokenNo = entity.TokenNo,
                Status = entity.Status,
                StatusName = entity.Status == CustomerOrderStatus.Approved || entity.Status == CustomerOrderStatus.InProgress
                    ? "In Progress"
                    : entity.Status == CustomerOrderStatus.Completed
                        ? "Completed"
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
                PaymentMethod = entity.PaymentMethod,
                DeliveryRating = entity.DeliveryRating,
                CustomerRating = entity.CustomerRating,
                CustomerFeedbackComment = entity.CustomerFeedbackComment,
                SaleId = entity.SaleId,
                SaleInvoiceNo = entity.Sale?.InvoiceNo,
                PaidAmount = entity.Sale != null
                    ? Math.Round(entity.Sale.CashAmount + entity.Sale.CardAmount, 2)
                    : (entity.Status == CustomerOrderStatus.Completed ? entity.TotalAmount : 0m),
                DueAmount = entity.Sale != null
                    ? Math.Round(Math.Max(0m, entity.Sale.TotalAmount - (entity.Sale.CashAmount + entity.Sale.CardAmount)), 2)
                    : (entity.Status == CustomerOrderStatus.Completed ? 0m : entity.TotalAmount),
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                DistanceKm = entity.DistanceKm,
                RiderId = entity.RiderId,
                RiderName = entity.RiderName,
                RiderPhone = entity.RiderPhone,
                RiderAssignedTime = entity.RiderAssignedTime.HasValue && entity.RiderAssignedTime.Value < entity.OrderDate.AddMinutes(-10)
                    ? entity.RiderAssignedTime.Value.Add(TimeZoneInfo.Local.GetUtcOffset(entity.RiderAssignedTime.Value))
                    : entity.RiderAssignedTime,
                Lines = new List<CustomerOrderLineDto>()
            };

            if (entity.Lines != null)
            {
                var productIds = entity.Lines.Select(l => l.ProductId).Distinct().ToList();
                var dealIds = entity.Lines
                    .Where(l => l.Product != null && (l.Product.IsDeal || l.Product.DealId.HasValue))
                    .Select(l => l.Product.DealId)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .Distinct()
                    .ToList();

                var matchingDeals = _dealRepository.GetAll()
                    .Where(d => d.ProductId.HasValue && productIds.Contains(d.ProductId.Value))
                    .ToList();

                foreach (var md in matchingDeals)
                {
                    if (!dealIds.Contains(md.Id))
                    {
                        dealIds.Add(md.Id);
                    }
                }

                var dealBreakdowns = new Dictionary<int, List<string>>();
                if (dealIds.Count > 0)
                {
                    var details = _dealDetailRepository.GetAll()
                        .Include(d => d.Product)
                        .Where(d => dealIds.Contains(d.DealId))
                        .ToList();

                    dealBreakdowns = details
                        .GroupBy(d => d.DealId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(d => $"{d.Quantity:0.#}x {d.Product?.Name ?? "Item"}").ToList());
                }

                foreach (var line in entity.Lines)
                {
                    var dealId = line.Product?.DealId;
                    if (!dealId.HasValue)
                    {
                        var matching = matchingDeals.FirstOrDefault(d => d.ProductId == line.ProductId);
                        if (matching != null) dealId = matching.Id;
                    }

                    var isDeal = (line.Product != null && line.Product.IsDeal) || dealId.HasValue;
                    var breakdowns = new List<string>();
                    if (dealId.HasValue && dealBreakdowns.TryGetValue(dealId.Value, out var list))
                    {
                        breakdowns = list;
                    }

                    dto.Lines.Add(new CustomerOrderLineDto
                    {
                        Id = line.Id,
                        OrderId = line.OrderId,
                        ProductId = line.ProductId,
                        ProductName = line.Product?.Name,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        LineTotal = line.LineTotal,
                        IsDeal = isDeal,
                        DealItems = breakdowns
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

                await EnrichRiderContactAsync(new List<CustomerOrderDto> { dto });
                return dto;
            }
        }

        /// <summary>
        /// Anonymous customer order history lookup by phone number.
        /// Matches exact phone or the same trailing digits (handles 03xx / +92 formats).
        /// </summary>
        [AbpAllowAnonymous]
        public async Task<List<CustomerOrderDto>> GetOrdersByPhoneAsync(CustomerOrderHistoryInput input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                if (input == null || string.IsNullOrWhiteSpace(input.Phone))
                {
                    throw new UserFriendlyException("Please enter your phone number.");
                }

                var phone = input.Phone.Trim();
                var digits = NormalizePhoneDigits(phone);
                if (digits.Length < 7)
                {
                    throw new UserFriendlyException("Please enter a valid phone number (at least 7 digits).");
                }

                var take = input.MaxResultCount > 0 ? Math.Min(input.MaxResultCount, 50) : 30;
                var tail10 = digits.Length >= 10 ? digits.Substring(digits.Length - 10) : digits;
                var tail7 = digits.Length >= 7 ? digits.Substring(digits.Length - 7) : digits;

                var query = Repository.GetAll()
                    .AsNoTracking()
                    .Include(x => x.Customer)
                    .Include(x => x.Sale)
                    .Include(x => x.Lines)
                        .ThenInclude(l => l.Product)
                    .Include(x => x.Branch)
                    .Where(x => x.Customer != null && x.Customer.Phone != null && x.Customer.Phone != "")
                    .Where(x =>
                        x.Customer.Phone == phone ||
                        x.Customer.Phone.EndsWith(tail7) ||
                        x.Customer.Phone.Contains(tail7));

                if (input.BranchId.HasValue && input.BranchId.Value > 0)
                {
                    query = query.Where(x => x.BranchId == input.BranchId.Value);
                }

                var candidates = await query
                    .OrderByDescending(x => x.OrderDate)
                    .Take(100)
                    .ToListAsync();

                var matched = candidates
                    .Where(o => PhoneDigitsMatch(o.Customer?.Phone, digits, tail10))
                    .Take(take)
                    .ToList();

                var dtos = matched.Select(MapToEntityDto).ToList();
                await EnrichRiderContactAsync(dtos);
                return dtos;
            }
        }

        /// <summary>
        /// Backfill missing rider phone from Staff (and linked user) so customers can Call / WhatsApp.
        /// </summary>
        private async Task EnrichRiderContactAsync(List<CustomerOrderDto> orders)
        {
            if (orders == null || orders.Count == 0)
            {
                return;
            }

            var riderIds = orders
                .Where(o => o.RiderId.HasValue && o.RiderId.Value > 0 && string.IsNullOrWhiteSpace(o.RiderPhone))
                .Select(o => o.RiderId.Value)
                .Distinct()
                .ToList();
            if (riderIds.Count == 0)
            {
                return;
            }

            var staffRows = await _staffRepository.GetAll()
                .AsNoTracking()
                .Where(s => riderIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name, s.Phone, s.UserId })
                .ToListAsync();

            var userIds = staffRows
                .Where(s => s.UserId.HasValue && s.UserId.Value > 0 && string.IsNullOrWhiteSpace(s.Phone))
                .Select(s => s.UserId.Value)
                .Distinct()
                .ToList();

            Dictionary<long, string> userPhones = new Dictionary<long, string>();
            if (userIds.Count > 0)
            {
                userPhones = await _userRepository.GetAll()
                    .AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.PhoneNumber })
                    .ToDictionaryAsync(u => u.Id, u => u.PhoneNumber);
            }

            var phoneByRider = new Dictionary<int, string>();
            var nameByRider = new Dictionary<int, string>();
            foreach (var s in staffRows)
            {
                if (!string.IsNullOrWhiteSpace(s.Name))
                {
                    nameByRider[s.Id] = s.Name;
                }

                var phone = (s.Phone ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(phone) &&
                    s.UserId.HasValue &&
                    userPhones.TryGetValue(s.UserId.Value, out var up) &&
                    !string.IsNullOrWhiteSpace(up))
                {
                    phone = up.Trim();
                }

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    phoneByRider[s.Id] = phone;
                }
            }

            foreach (var dto in orders)
            {
                if (!dto.RiderId.HasValue)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(dto.RiderName) &&
                    nameByRider.TryGetValue(dto.RiderId.Value, out var name))
                {
                    dto.RiderName = name;
                }

                if (string.IsNullOrWhiteSpace(dto.RiderPhone) &&
                    phoneByRider.TryGetValue(dto.RiderId.Value, out var phone))
                {
                    dto.RiderPhone = phone;
                }
            }
        }

        [AbpAllowAnonymous]
        public async Task<OnlineCustomerProfileDto> GetOnlineCustomerProfileAsync(string phone, int branchId)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                if (branchId <= 0)
                {
                    throw new UserFriendlyException("Please select a branch.");
                }

                var mobile = (phone ?? string.Empty).Trim();
                var digits = NormalizePhoneDigits(mobile);
                if (digits.Length < 7)
                {
                    throw new UserFriendlyException("Please enter a valid phone number.");
                }

                var customers = await _customerRepository.GetAll()
                    .AsNoTracking()
                    .Where(c => c.BranchId == branchId && c.Phone != null && c.Phone != "")
                    .OrderByDescending(c => c.Id)
                    .Take(50)
                    .ToListAsync();

                var match = customers.FirstOrDefault(c =>
                {
                    var stored = NormalizePhoneDigits(c.Phone);
                    return stored == digits ||
                           (stored.Length >= 10 && digits.Length >= 10 &&
                            stored.Substring(stored.Length - 10) == digits.Substring(digits.Length - 10));
                });

                if (match == null)
                {
                    return null;
                }

                var profile = MapOnlineCustomerProfile(match);
                profile.SavedAddresses = await LoadSavedAddressDtosAsync(match.Id);
                return profile;
            }
        }

        [AbpAllowAnonymous]
        public async Task<OnlineCustomerProfileDto> UpsertOnlineCustomerProfileAsync(UpsertOnlineCustomerProfileInput input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                if (input == null)
                {
                    throw new UserFriendlyException("Profile details are required.");
                }

                var branchId = input.BranchId.GetValueOrDefault();
                if (branchId <= 0)
                {
                    throw new UserFriendlyException("Please select a branch.");
                }

                var branch = await _branchRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == branchId);
                if (branch == null)
                {
                    throw new UserFriendlyException("Branch not found.");
                }

                var name = (input.Name ?? string.Empty).Trim();
                var mobile = (input.Phone ?? string.Empty).Trim();
                var email = (input.Email ?? string.Empty).Trim();
                var address = (input.Address ?? string.Empty).Trim();
                var digits = NormalizePhoneDigits(mobile);
                var hasCoords = input.Latitude.HasValue && input.Longitude.HasValue
                    && Math.Abs(input.Latitude.Value) <= 90
                    && Math.Abs(input.Longitude.Value) <= 180;

                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new UserFriendlyException("Please enter your name.");
                }

                if (digits.Length < 7)
                {
                    throw new UserFriendlyException("Please enter a valid phone number.");
                }

                var candidates = await _customerRepository.GetAll()
                    .Where(c => c.BranchId == branchId && c.Phone != null && c.Phone != "")
                    .OrderByDescending(c => c.Id)
                    .Take(50)
                    .ToListAsync();

                var customer = candidates.FirstOrDefault(c =>
                {
                    var stored = NormalizePhoneDigits(c.Phone);
                    return stored == digits ||
                           (stored.Length >= 10 && digits.Length >= 10 &&
                            stored.Substring(stored.Length - 10) == digits.Substring(digits.Length - 10));
                });

                if (customer == null)
                {
                    customer = new Customer
                    {
                        TenantId = branch.TenantId ?? AbpSession.TenantId,
                        BranchId = branchId,
                        Name = name,
                        Phone = mobile,
                        Email = string.IsNullOrWhiteSpace(email) ? null : email,
                        CustomerType = CustomerTypes.Direct
                    };
                    await _customerRepository.InsertAsync(customer);
                }
                else
                {
                    customer.Name = name;
                    customer.Phone = mobile;
                    customer.Email = string.IsNullOrWhiteSpace(email) ? null : email;
                }

                if (input.ClearImage)
                {
                    CustomerImageStore.DeleteIfExists(customer.ImagePath);
                    customer.ImagePath = null;
                }
                else if (CustomerImageStore.IsNewImagePayload(input.ImageBase64))
                {
                    var oldPath = customer.ImagePath;
                    customer.ImagePath = CustomerImageStore.SaveBase64Image(input.ImageBase64);
                    if (!string.IsNullOrWhiteSpace(oldPath) &&
                        !string.Equals(oldPath, customer.ImagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        CustomerImageStore.DeleteIfExists(oldPath);
                    }
                }

                await CurrentUnitOfWork.SaveChangesAsync();

                // Customer profile: keep a selectable saved address+zone when address is provided.
                if (!string.IsNullOrWhiteSpace(address))
                {
                    await UpsertSavedAddressForCustomerAsync(
                        customer.Id,
                        customer.TenantId ?? branch.TenantId ?? AbpSession.TenantId,
                        address,
                        landmark: null,
                        deliveryAreaId: null,
                        deliveryAreaName: null,
                        latitude: hasCoords ? input.Latitude : null,
                        longitude: hasCoords ? input.Longitude : null,
                        setAsDefault: true);
                }

                var profile = MapOnlineCustomerProfile(customer);
                profile.SavedAddresses = await LoadSavedAddressDtosAsync(customer.Id);
                return profile;
            }
        }

        [AbpAllowAnonymous]
        public async Task<List<CustomerSavedAddressDto>> GetSavedAddressesAsync(GetCustomerSavedAddressesInput input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var customerId = await ResolveCustomerIdForSavedAddressAsync(input?.CustomerId, input?.Phone, input?.BranchId);
                if (!customerId.HasValue)
                {
                    return new List<CustomerSavedAddressDto>();
                }

                return await LoadSavedAddressDtosAsync(customerId.Value);
            }
        }

        [AbpAllowAnonymous]
        public async Task<CustomerSavedAddressDto> SaveSavedAddressAsync(SaveCustomerAddressInput input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                if (input == null || string.IsNullOrWhiteSpace(input.Address))
                {
                    throw new UserFriendlyException("Address is required.");
                }

                int customerId;
                int? tenantId = AbpSession.TenantId;

                if (input.CustomerId.HasValue && input.CustomerId.Value > 0)
                {
                    // Staff path: known customer id.
                    var customer = await _customerRepository.GetAsync(input.CustomerId.Value);
                    if (AbpSession.UserId.HasValue)
                    {
                        await _branchAccessChecker.EnsureCanAccessBranchAsync(customer.BranchId);
                    }
                    customerId = customer.Id;
                    tenantId = customer.TenantId ?? tenantId;
                }
                else
                {
                    var branchId = input.BranchId.GetValueOrDefault();
                    if (branchId <= 0)
                    {
                        throw new UserFriendlyException("Please select a branch.");
                    }

                    var digits = NormalizePhoneDigits(input.Phone);
                    if (digits.Length < 7)
                    {
                        throw new UserFriendlyException("Please enter a valid phone number.");
                    }

                    var customer = await FindCustomerByPhoneAsync(branchId, digits);
                    if (customer == null)
                    {
                        var branch = await _branchRepository.GetAll()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(b => b.Id == branchId);
                        if (branch == null)
                        {
                            throw new UserFriendlyException("Branch not found.");
                        }

                        customer = new Customer
                        {
                            TenantId = branch.TenantId ?? AbpSession.TenantId,
                            BranchId = branchId,
                            Name = "Customer " + digits.Substring(Math.Max(0, digits.Length - 4)),
                            Phone = (input.Phone ?? string.Empty).Trim(),
                            CustomerType = CustomerTypes.Direct
                        };
                        customerId = await _customerRepository.InsertAndGetIdAsync(customer);
                        tenantId = customer.TenantId;
                        await CurrentUnitOfWork.SaveChangesAsync();
                    }
                    else
                    {
                        customerId = customer.Id;
                        tenantId = customer.TenantId ?? tenantId;
                    }
                }

                var saved = await UpsertSavedAddressForCustomerAsync(
                    customerId,
                    tenantId,
                    input.Address,
                    input.Landmark,
                    input.DeliveryAreaId,
                    input.DeliveryAreaName,
                    input.Latitude,
                    input.Longitude,
                    setAsDefault: input.IsDefault || !input.Id.HasValue,
                    existingId: input.Id,
                    label: input.Label);

                if (saved == null)
                {
                    throw new UserFriendlyException("Could not save address.");
                }

                return MapSavedAddressDto(saved);
            }
        }

        [AbpAllowAnonymous]
        public async Task DeleteSavedAddressAsync(DeleteCustomerSavedAddressInput input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                if (input == null || input.Id <= 0)
                {
                    throw new UserFriendlyException("Address id is required.");
                }

                var entity = await _savedAddresses.GetAsync(input.Id);
                if (entity == null)
                {
                    return;
                }

                if (entity.OwnerType != SavedAddressOwnerTypes.Customer)
                {
                    throw new UserFriendlyException("You cannot delete this address.");
                }

                if (AbpSession.UserId.HasValue)
                {
                    var customer = await _customerRepository.GetAsync(entity.OwnerId);
                    await _branchAccessChecker.EnsureCanAccessBranchAsync(customer.BranchId);
                }
                else
                {
                    var ownerId = await ResolveCustomerIdForSavedAddressAsync(null, input.Phone, input.BranchId);
                    if (!ownerId.HasValue || ownerId.Value != entity.OwnerId)
                    {
                        throw new UserFriendlyException("You cannot delete this address.");
                    }
                }

                await _savedAddresses.DeleteAsync(entity.Id);
            }
        }

        [AbpAllowAnonymous]
        public async Task<List<int>> GetFavoriteProductIdsAsync(GetCustomerFavoritesInput input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var branchId = input?.BranchId.GetValueOrDefault() ?? 0;
                var customerId = await ResolveCustomerIdForSavedAddressAsync(
                    input?.CustomerId,
                    input?.Phone,
                    branchId > 0 ? branchId : (int?)null);

                if (!customerId.HasValue)
                {
                    return new List<int>();
                }

                var query = _customerFavoriteRepository.GetAll()
                    .AsNoTracking()
                    .Where(f => f.CustomerId == customerId.Value);

                if (branchId > 0)
                {
                    query = query.Where(f => f.BranchId == branchId);
                }

                return await query
                    .OrderByDescending(f => f.Id)
                    .Select(f => f.ProductId)
                    .Distinct()
                    .ToListAsync();
            }
        }

        [AbpAllowAnonymous]
        public async Task<ToggleCustomerFavoriteResultDto> ToggleFavoriteAsync(ToggleCustomerFavoriteInput input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                if (input == null || input.ProductId <= 0)
                {
                    throw new UserFriendlyException("Product is required.");
                }

                var branchId = input.BranchId.GetValueOrDefault();
                if (branchId <= 0)
                {
                    throw new UserFriendlyException("Please select a branch.");
                }

                var productExists = await _productRepository.GetAll()
                    .AsNoTracking()
                    .AnyAsync(p => p.Id == input.ProductId);
                if (!productExists)
                {
                    throw new UserFriendlyException("Product not found.");
                }

                var customer = await EnsureCustomerForFavoritesAsync(input.CustomerId, input.Phone, branchId);
                var existing = await _customerFavoriteRepository.FirstOrDefaultAsync(f =>
                    f.CustomerId == customer.Id &&
                    f.ProductId == input.ProductId &&
                    f.BranchId == branchId);

                if (existing != null)
                {
                    await _customerFavoriteRepository.DeleteAsync(existing);
                    return new ToggleCustomerFavoriteResultDto
                    {
                        ProductId = input.ProductId,
                        IsFavorite = false
                    };
                }

                await _customerFavoriteRepository.InsertAsync(new CustomerFavorite
                {
                    TenantId = customer.TenantId ?? AbpSession.TenantId,
                    CustomerId = customer.Id,
                    ProductId = input.ProductId,
                    BranchId = branchId
                });

                return new ToggleCustomerFavoriteResultDto
                {
                    ProductId = input.ProductId,
                    IsFavorite = true
                };
            }
        }

        private async Task<Customer> EnsureCustomerForFavoritesAsync(int? customerId, string phone, int branchId)
        {
            if (customerId.HasValue && customerId.Value > 0 && AbpSession.UserId.HasValue)
            {
                var existing = await _customerRepository.GetAsync(customerId.Value);
                await _branchAccessChecker.EnsureCanAccessBranchAsync(existing.BranchId);
                return existing;
            }

            var digits = NormalizePhoneDigits(phone);
            if (digits.Length < 7)
            {
                throw new UserFriendlyException("Please enter your phone number in Account to save favorites.");
            }

            var match = await FindCustomerByPhoneAsync(branchId, digits);
            if (match != null)
            {
                return match;
            }

            var branch = await _branchRepository.GetAll()
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == branchId);
            if (branch == null)
            {
                throw new UserFriendlyException("Branch not found.");
            }

            var created = new Customer
            {
                TenantId = branch.TenantId ?? AbpSession.TenantId,
                BranchId = branchId,
                Name = "Online Customer",
                Phone = (phone ?? string.Empty).Trim(),
                CustomerType = CustomerTypes.Direct
            };
            await _customerRepository.InsertAsync(created);
            await CurrentUnitOfWork.SaveChangesAsync();
            return created;
        }

        private OnlineCustomerProfileDto MapOnlineCustomerProfile(Customer customer)
        {
            var def = _savedAddresses.QueryForOwner(SavedAddressOwnerTypes.Customer, customer.Id)
                .AsNoTracking()
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.Id)
                .FirstOrDefault();

            return new OnlineCustomerProfileDto
            {
                Id = customer.Id,
                BranchId = customer.BranchId,
                Name = customer.Name,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = def?.Address,
                Latitude = def?.Latitude,
                Longitude = def?.Longitude,
                ImagePath = customer.ImagePath
            };
        }

        private string GetDefaultSavedAddressText(int customerId)
        {
            return _savedAddresses.GetDefaultAddressText(SavedAddressOwnerTypes.Customer, customerId);
        }

        private async Task<List<CustomerSavedAddressDto>> LoadSavedAddressDtosAsync(int customerId)
        {
            var rows = await _savedAddresses.QueryForOwner(SavedAddressOwnerTypes.Customer, customerId)
                .AsNoTracking()
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.Id)
                .Take(30)
                .ToListAsync();

            return rows.Select(MapSavedAddressDto).ToList();
        }

        private static CustomerSavedAddressDto MapSavedAddressDto(SavedAddress entity)
        {
            return new CustomerSavedAddressDto
            {
                Id = entity.Id,
                OwnerType = entity.OwnerType,
                OwnerId = entity.OwnerId,
                CustomerId = entity.OwnerType == SavedAddressOwnerTypes.Customer ? entity.OwnerId : 0,
                Label = entity.Label,
                Address = entity.Address,
                Landmark = entity.Landmark,
                DeliveryAreaId = entity.DeliveryAreaId,
                DeliveryAreaName = entity.DeliveryAreaName,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                IsDefault = entity.IsDefault
            };
        }

        private async Task<int?> ResolveCustomerIdForSavedAddressAsync(int? customerId, string phone, int? branchId)
        {
            if (customerId.HasValue && customerId.Value > 0 && AbpSession.UserId.HasValue)
            {
                var customer = await _customerRepository.GetAsync(customerId.Value);
                await _branchAccessChecker.EnsureCanAccessBranchAsync(customer.BranchId);
                return customer.Id;
            }

            var bid = branchId.GetValueOrDefault();
            var digits = NormalizePhoneDigits(phone);
            if (bid <= 0 || digits.Length < 7)
            {
                return null;
            }

            var match = await FindCustomerByPhoneAsync(bid, digits);
            return match?.Id;
        }

        private async Task<Customer> FindCustomerByPhoneAsync(int branchId, string digits)
        {
            var candidates = await _customerRepository.GetAll()
                .Where(c => c.BranchId == branchId && c.Phone != null && c.Phone != "")
                .OrderByDescending(c => c.Id)
                .Take(50)
                .ToListAsync();

            return candidates.FirstOrDefault(c =>
            {
                var stored = NormalizePhoneDigits(c.Phone);
                return stored == digits ||
                       (stored.Length >= 10 && digits.Length >= 10 &&
                        stored.Substring(stored.Length - 10) == digits.Substring(digits.Length - 10));
            });
        }

        private Task<SavedAddress> UpsertSavedAddressForCustomerAsync(
            int customerId,
            int? tenantId,
            string address,
            string landmark,
            int? deliveryAreaId,
            string deliveryAreaName,
            double? latitude,
            double? longitude,
            bool setAsDefault,
            int? existingId = null,
            string label = null)
        {
            return _savedAddresses.UpsertDefaultAsync(
                SavedAddressOwnerTypes.Customer,
                customerId,
                tenantId,
                address,
                landmark,
                deliveryAreaId,
                deliveryAreaName,
                latitude,
                longitude,
                label,
                existingId);
        }

        [AbpAllowAnonymous]
        public async Task<List<BranchFeedbackDto>> GetBranchFeedbacksAsync(int branchId, int maxResultCount = 20)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                if (branchId <= 0)
                {
                    return new List<BranchFeedbackDto>();
                }

                // Public strip: latest rating per customer, then newest 5 only.
                var take = maxResultCount > 0 ? Math.Min(maxResultCount, 5) : 5;

                var candidates = await Repository.GetAll()
                    .AsNoTracking()
                    .Include(o => o.Customer)
                    .Include(o => o.Branch)
                    .Include(o => o.Lines)
                        .ThenInclude(l => l.Product)
                    .Where(o => o.BranchId == branchId && o.CustomerRating != null && o.CustomerRating > 0)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(80)
                    .ToListAsync();

                var orders = candidates
                    .GroupBy(o => o.CustomerId)
                    .Select(g => g.OrderByDescending(x => x.OrderDate).First())
                    .OrderByDescending(o => o.OrderDate)
                    .Take(take)
                    .ToList();

                return orders.Select(o =>
                {
                    var itemNames = (o.Lines ?? Enumerable.Empty<CustomerOrderLine>())
                        .Where(l => l.Product != null && !string.IsNullOrWhiteSpace(l.Product.Name))
                        .Select(l => l.Product.Name.Trim())
                        .Distinct()
                        .ToList();

                    return new BranchFeedbackDto
                    {
                        OrderId = o.Id,
                        CustomerId = o.CustomerId,
                        OrderNo = o.OrderNo,
                        CustomerName = o.Customer != null ? o.Customer.Name : "Customer",
                        CustomerImagePath = o.Customer != null ? o.Customer.ImagePath : null,
                        Rating = o.CustomerRating.Value,
                        OrderDate = o.OrderDate,
                        BranchName = o.Branch != null ? o.Branch.Name : null,
                        Comment = o.CustomerFeedbackComment,
                        ItemNames = itemNames,
                        ItemsSummary = itemNames.Count == 0
                            ? null
                            : string.Join(", ", itemNames)
                    };
                }).ToList();
            }
        }

        private static string NormalizePhoneDigits(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            return new string(phone.Where(char.IsDigit).ToArray());
        }

        private static bool PhoneDigitsMatch(string storedPhone, string lookupDigits, string lookupTail)
        {
            var storedDigits = NormalizePhoneDigits(storedPhone);
            if (string.IsNullOrEmpty(storedDigits))
            {
                return false;
            }

            if (storedDigits == lookupDigits)
            {
                return true;
            }

            var storedTail = storedDigits.Length >= 10
                ? storedDigits.Substring(storedDigits.Length - 10)
                : storedDigits;

            return storedTail == lookupTail;
        }

        [AbpAllowAnonymous]
        public async Task<CustomerOrderDto> RateOrderAsync(RateCustomerOrderInput input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                if (input == null || string.IsNullOrWhiteSpace(input.OrderNo))
                {
                    throw new UserFriendlyException("Please provide a valid Order Number.");
                }

                if (input.Rating < 1 || input.Rating > 5)
                {
                    throw new UserFriendlyException("Rating must be between 1 and 5.");
                }

                var clean = input.OrderNo.Trim();
                var query = Repository.GetAll()
                    .Include(x => x.Customer)
                    .Include(x => x.Sale)
                    .Include(x => x.Lines)
                        .ThenInclude(l => l.Product)
                    .Include(x => x.Branch);

                var order = await query.FirstOrDefaultAsync(x => x.OrderNo == clean);

                if (order == null)
                {
                    order = await query.FirstOrDefaultAsync(x => x.OrderNo.ToLower() == clean.ToLower());
                }

                if (order == null)
                {
                    throw new UserFriendlyException($"No order found matching '{clean}'.");
                }

                if (order.Status != CustomerOrderStatus.Delivered &&
                    order.Status != CustomerOrderStatus.Completed)
                {
                    throw new UserFriendlyException("You can only rate a delivered order.");
                }

                order.CustomerRating = input.Rating;
                var comment = (input.Comment ?? string.Empty).Trim();
                if (comment.Length > CustomerOrder.MaxCustomerFeedbackCommentLength)
                {
                    comment = comment.Substring(0, CustomerOrder.MaxCustomerFeedbackCommentLength);
                }
                order.CustomerFeedbackComment = string.IsNullOrWhiteSpace(comment) ? null : comment;
                await CurrentUnitOfWork.SaveChangesAsync();

                return MapToEntityDto(order);
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

            // Find all active orders currently assigned to a rider that are not yet finished
            // Exclude currentOrderId so reassignment doesn't treat the rider as busy on the very order being edited
            var activeOrders = await Repository.GetAll()
                .Where(o => o.RiderId.HasValue &&
                            o.Status != CustomerOrderStatus.Delivered &&
                            o.Status != CustomerOrderStatus.Completed &&
                            o.Status != CustomerOrderStatus.Rejected)
                .WhereIf(currentOrderId.HasValue, o => o.Id != currentOrderId.Value)
                .Select(o => new { o.Id, o.OrderNo, RiderId = o.RiderId.Value })
                .ToListAsync();

            var activeByRider = activeOrders
                .GroupBy(o => o.RiderId)
                .ToDictionary(g => g.Key, g => g.First());

            var today = Clock.Now.Date;

            // Query previous order times for each rider to determine idle time and today's delivered count
            var riderIds = riders.Select(r => r.Id).ToList();
            var riderOrders = await Repository.GetAll()
                .Where(o => o.RiderId.HasValue && riderIds.Contains(o.RiderId.Value))
                .WhereIf(currentOrderId.HasValue, o => o.Id != currentOrderId.Value)
                .Select(o => new
                {
                    RiderId = o.RiderId.Value,
                    Status = o.Status,
                    DeliveryTime = (o.Status == CustomerOrderStatus.Delivered || o.Status == CustomerOrderStatus.Completed) &&
                                   o.LastModificationTime.HasValue
                        ? o.LastModificationTime.Value
                        : (o.RiderAssignedTime ?? o.OrderDate),
                    OrderTime = (o.Status == CustomerOrderStatus.Delivered || o.Status == CustomerOrderStatus.Completed) &&
                                o.LastModificationTime.HasValue
                        ? o.LastModificationTime.Value
                        : (o.RiderAssignedTime ?? o.OrderDate)
                })
                .ToListAsync();

            var lastOrderTimes = riderOrders
                .GroupBy(o => o.RiderId)
                .ToDictionary(g => g.Key, g => g.Max(x => x.OrderTime));

            var todayDeliveredByRider = riderOrders
                .Where(o =>
                    (o.Status == CustomerOrderStatus.Delivered || o.Status == CustomerOrderStatus.Completed) &&
                    (o.DeliveryTime.Date == today))
                .GroupBy(o => o.RiderId)
                .ToDictionary(g => g.Key, g => g.Count());

            var now = Clock.Now;

            return riders.Select(r =>
            {
                var isBusy = activeByRider.TryGetValue(r.Id, out var busyOrder);
                var hasLastOrder = lastOrderTimes.TryGetValue(r.Id, out var lastTime);
                DateTime? lastOrderTime = hasLastOrder ? (DateTime?)lastTime : null;
                string lastOrderText = "No previous orders";
                int idleMins = int.MaxValue;

                if (lastOrderTime.HasValue)
                {
                    var effTime = lastOrderTime.Value;
                    if (effTime < now.AddMinutes(-10) && (now - effTime).TotalHours >= 4 && (now - effTime).TotalHours <= 6)
                    {
                        effTime = effTime.Add(TimeZoneInfo.Local.GetUtcOffset(effTime));
                    }
                    var diff = now - effTime;
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
                    o.Status != CustomerOrderStatus.Completed &&
                    o.Status != CustomerOrderStatus.Rejected);

                if (busyOrder != null)
                {
                    var busyNo = !string.IsNullOrWhiteSpace(busyOrder.OrderNo) ? busyOrder.OrderNo : $"#{busyOrder.Id}";
                    throw new UserFriendlyException($"Rider '{rider.Name}' is already delivering Order #{busyNo}. Cannot assign until that delivery is completed or cancelled.");
                }

                order.RiderId = rider.Id;
                order.RiderName = rider.Name;
                order.RiderPhone = rider.Phone;
                order.RiderAssignedTime = Clock.Now;

                // Advance status to InProgress (Out for delivery) if not already delivered/rejected
                if (order.Status == CustomerOrderStatus.Approved || order.Status == CustomerOrderStatus.Pending)
                {
                    order.Status = CustomerOrderStatus.InProgress;
                }

                await CurrentUnitOfWork.SaveChangesAsync();

                return MapToEntityDto(order);
            }
        }

        private static string NormalizePaymentMethod(string paymentMethod)
        {
            var raw = (paymentMethod ?? string.Empty).Trim();
            if (raw.Equals("OnlinePayment", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("Online", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("Card", StringComparison.OrdinalIgnoreCase))
            {
                return "OnlinePayment";
            }

            return "CashOnDelivery";
        }

        private static string FormatPaymentMethodLabel(string paymentMethod)
        {
            return string.Equals(paymentMethod, "OnlinePayment", StringComparison.OrdinalIgnoreCase)
                ? "Online Payment"
                : "Cash on Delivery";
        }
    }
}
