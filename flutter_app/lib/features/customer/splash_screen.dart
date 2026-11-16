import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../services/session_service.dart';
import '../../services/online_catalog_service.dart';
import '../../services/cart_service.dart';
import '../../services/waiter_service.dart';
import '../../services/rider_service.dart';
import '../../services/dashboard_service.dart';
import '../customer/customer_shell_screen.dart';
import '../waiter/waiter_tables_screen.dart';
import '../rider/rider_dashboard_screen.dart';
import '../admin/admin_dashboard_screen.dart';

class SplashScreen extends StatefulWidget {
  final SessionService sessionService;
  final OnlineCatalogService catalogService;
  final CartService cartService;
  final WaiterService waiterService;
  final RiderService riderService;
  final DashboardService dashboardService;

  const SplashScreen({
    super.key,
    required this.sessionService,
    required this.catalogService,
    required this.cartService,
    required this.waiterService,
    required this.riderService,
    required this.dashboardService,
  });

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> {
  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    await Future.delayed(const Duration(milliseconds: 1400));
    await widget.sessionService.init();
    if (!mounted) return;

    if (widget.sessionService.isWaiter) {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => WaiterTablesScreen(
            sessionService: widget.sessionService,
            waiterService: widget.waiterService,
          ),
        ),
      );
    } else if (widget.sessionService.isRider) {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => RiderDashboardScreen(
            sessionService: widget.sessionService,
            riderService: widget.riderService,
          ),
        ),
      );
    } else if (widget.sessionService.isAdmin) {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => AdminDashboardScreen(
            sessionService: widget.sessionService,
            dashboardService: widget.dashboardService,
          ),
        ),
      );
    } else {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => CustomerShellScreen(
            sessionService: widget.sessionService,
            catalogService: widget.catalogService,
            cartService: widget.cartService,
          ),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              width: 90,
              height: 90,
              decoration: BoxDecoration(
                color: AppColors.primaryLight,
                shape: BoxShape.circle,
                border: Border.all(color: AppColors.primary, width: 2),
              ),
              child: const Center(child: Text('🍲', style: TextStyle(fontSize: 44))),
            ),
            const SizedBox(height: 20),
            const Text(
              'ALJANNAT',
              style: TextStyle(
                fontSize: 26,
                fontWeight: FontWeight.bold,
                letterSpacing: 2,
                color: AppColors.primary,
              ),
            ),
            const SizedBox(height: 6),
            const Text(
              'POS & Online Ordering',
              style: TextStyle(fontSize: 13, letterSpacing: 1.2, color: AppColors.textSecondary),
            ),
            const SizedBox(height: 36),
            const SizedBox(
              width: 24,
              height: 24,
              child: CircularProgressIndicator(strokeWidth: 2.5, color: AppColors.primary),
            ),
          ],
        ),
      ),
    );
  }
}
