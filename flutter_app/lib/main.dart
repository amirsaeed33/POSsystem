import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'core/api/abp_api_client.dart';
import 'core/storage/session_storage.dart';
import 'core/theme/app_theme.dart';
import 'services/session_service.dart';
import 'services/online_catalog_service.dart';
import 'services/cart_service.dart';
import 'services/waiter_service.dart';
import 'services/rider_service.dart';
import 'services/dashboard_service.dart';
import 'features/customer/splash_screen.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  SystemChrome.setSystemUIOverlayStyle(
    const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.dark,
    ),
  );

  final storage = SessionStorage();
  await storage.init();
  await storage.setBaseUrl('https://aljannatapp.com');

  final apiClient = AbpApiClient(storage);
  apiClient.updateBaseUrl('https://aljannatapp.com');

  final sessionService = SessionService(apiClient, storage);
  final catalogService = OnlineCatalogService(apiClient);
  final cartService = CartService();
  final waiterService = WaiterService(apiClient);
  final riderService = RiderService(apiClient);
  final dashboardService = DashboardService(apiClient);

  runApp(
    AljannatPosApp(
      sessionService: sessionService,
      catalogService: catalogService,
      cartService: cartService,
      waiterService: waiterService,
      riderService: riderService,
      dashboardService: dashboardService,
    ),
  );
}

class AljannatPosApp extends StatelessWidget {
  final SessionService sessionService;
  final OnlineCatalogService catalogService;
  final CartService cartService;
  final WaiterService waiterService;
  final RiderService riderService;
  final DashboardService dashboardService;

  const AljannatPosApp({
    super.key,
    required this.sessionService,
    required this.catalogService,
    required this.cartService,
    required this.waiterService,
    required this.riderService,
    required this.dashboardService,
  });

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Aljannat POS',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      home: SplashScreen(
        sessionService: sessionService,
        catalogService: catalogService,
        cartService: cartService,
        waiterService: waiterService,
        riderService: riderService,
        dashboardService: dashboardService,
      ),
    );
  }
}
