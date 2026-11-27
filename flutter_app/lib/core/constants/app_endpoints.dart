/// API routes aligned with MAUI `SmartPos.Mobile` services (production ABP).
class AppEndpoints {
  AppEndpoints._();

  static const String defaultBaseUrl = 'https://aljannatapp.com';

  // Auth & Session
  static const String authenticate = 'api/TokenAuth/Authenticate';
  static const String isTenantAvailable = 'api/services/app/Account/IsTenantAvailable';
  static const String getCurrentLogin = 'api/services/app/Session/GetCurrentLoginInformations';

  // Customer Online Ordering (OnlineOrderService.cs)
  static const String getOnlineBranches = 'api/services/app/CustomerOrder/GetOnlineBranches';
  static const String getOnlineStoreHeader = 'api/services/app/CustomerOrder/GetOnlineStoreHeader';
  static const String getOnlineCatalog = 'api/services/app/CustomerOrder/GetOnlineCatalog';
  static const String getOnlineCatalogPaged = 'api/services/app/CustomerOrder/GetOnlineCatalogPaged';
  static const String getOnlineCategories = 'api/services/app/CustomerOrder/GetOnlineCategories';
  static const String createOnlineOrder = 'api/services/app/CustomerOrder/CreateOnlineOrder';
  static const String getOrdersByPhone = 'api/services/app/CustomerOrder/GetOrdersByPhone';
  static const String trackOrder = 'api/services/app/CustomerOrder/TrackOrder';
  static const String getHeroSlides = 'api/services/app/OnlineOrderHeroSlide/GetActiveByBranch';
  static const String getDeals = 'api/services/app/Deal/GetAll';
  static const String getDealByProduct = 'api/services/app/Deal/GetByProductId';
  static const String getFavoriteProductIds = 'api/services/app/CustomerOrder/GetFavoriteProductIds';
  static const String toggleFavorite = 'api/services/app/CustomerOrder/ToggleFavorite';
  static const String getBranchFeedbacks = 'api/services/app/CustomerOrder/GetBranchFeedbacks';
  static const String getDeliveryAreas = 'api/services/app/DeliveryArea/GetActiveAreasForBranch';
  static const String getSavedAddresses = 'api/services/app/CustomerOrder/GetSavedAddresses';
  static const String saveSavedAddress = 'api/services/app/CustomerOrder/SaveSavedAddress';
  static const String getModifiersForProduct = 'api/services/app/ProductModifier/GetByProduct';

  // Waiter & Tables (RestaurantService.cs)
  static const String getTables = 'api/services/app/RestaurantTable/GetTables';
  static const String assignWaiter = 'api/services/app/RestaurantTable/AssignWaiter';
  static const String changeTableStatus = 'api/services/app/RestaurantTable/ChangeStatus';
  static const String getActiveOrders = 'api/services/app/RestaurantOrder/GetActiveOrders';
  static const String getOrder = 'api/services/app/RestaurantOrder/Get';
  static const String getOrderByTable = 'api/services/app/RestaurantOrder/GetByTable';
  static const String openTableSession = 'api/services/app/RestaurantOrder/OpenTableSession';
  static const String saveOrder = 'api/services/app/RestaurantOrder/SaveOrder';
  static const String sendToKitchen = 'api/services/app/RestaurantOrder/SendToKitchen';
  static const String confirmKotPrint = 'api/services/app/RestaurantOrder/ConfirmKotPrint';
  static const String printBill = 'api/services/app/RestaurantOrder/PrintBill';
  static const String settleOrder = 'api/services/app/RestaurantOrder/SettleOrder';
  static const String cancelOrder = 'api/services/app/RestaurantOrder/CancelOrder';
  static const String splitBill = 'api/services/app/RestaurantOrder/SplitBill';
  static const String getPosProducts = 'api/services/app/Sale/GetPosProducts';
  static const String getPosCustomers = 'api/services/app/Sale/GetPosCustomers';

  // Rider (RiderOrderService.cs)
  static const String getRiderDashboardStats = 'api/services/app/RiderOrder/GetDashboardStats';
  static const String getRiderOrders = 'api/services/app/RiderOrder/GetAssignedOrders';
  static const String getRiderHistory = 'api/services/app/RiderOrder/GetOrderHistory';
  static const String acceptRiderOrder = 'api/services/app/RiderOrder/AcceptOrder';
  static const String rejectRiderOrder = 'api/services/app/RiderOrder/RejectOrder';
  static const String completeDelivery = 'api/services/app/RiderOrder/CompleteDelivery';

  // Admin (DashboardService.cs)
  static const String getDashboard = 'api/services/app/Dashboard/Get';
  static const String getOnlineOrderPipeline = 'api/services/app/Dashboard/GetOnlineOrderPipeline';
}
