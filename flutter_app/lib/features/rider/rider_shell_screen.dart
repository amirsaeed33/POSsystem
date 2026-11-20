import 'package:flutter/material.dart';
import '../../core/theme/staff_theme.dart';
import '../../services/cart_service.dart';
import '../../services/online_catalog_service.dart';
import '../../services/rider_service.dart';
import '../../services/session_service.dart';
import '../customer/customer_shell_screen.dart';
import 'rider_dashboard_screen.dart';
import 'rider_history_screen.dart';

/// Delivery Rider portal — same as web: only Dashboard + Order History.
class RiderShellScreen extends StatefulWidget {
  final SessionService sessionService;
  final RiderService riderService;

  const RiderShellScreen({
    super.key,
    required this.sessionService,
    required this.riderService,
  });

  @override
  State<RiderShellScreen> createState() => _RiderShellScreenState();
}

class _RiderShellScreenState extends State<RiderShellScreen> {
  int _index = 0;

  Future<void> _logout() async {
    await widget.sessionService.logout();
    if (!mounted) return;
    final api = widget.sessionService.apiClient;
    Navigator.of(context).pushAndRemoveUntil(
      MaterialPageRoute(
        builder: (_) => CustomerShellScreen(
          sessionService: widget.sessionService,
          catalogService: OnlineCatalogService(api),
          cartService: CartService(),
        ),
      ),
      (_) => false,
    );
  }

  @override
  Widget build(BuildContext context) {
    final pages = [
      RiderDashboardScreen(
        sessionService: widget.sessionService,
        riderService: widget.riderService,
        onLogout: _logout,
        embedded: true,
      ),
      RiderHistoryScreen(
        sessionService: widget.sessionService,
        riderService: widget.riderService,
        embedded: true,
      ),
    ];

    return Scaffold(
      backgroundColor: StaffTheme.riderPaper,
      body: IndexedStack(index: _index, children: pages),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: (i) => setState(() => _index = i),
        backgroundColor: Colors.white,
        indicatorColor: StaffTheme.riderRoute.withValues(alpha: 0.15),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.dashboard_outlined),
            selectedIcon: Icon(Icons.dashboard, color: StaffTheme.riderRoute),
            label: 'My Dashboard',
          ),
          NavigationDestination(
            icon: Icon(Icons.history_outlined),
            selectedIcon: Icon(Icons.history, color: StaffTheme.riderRoute),
            label: 'Order History',
          ),
        ],
      ),
    );
  }
}
