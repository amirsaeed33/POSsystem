import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../models/branch_models.dart';
import '../../services/session_service.dart';
import '../../services/online_catalog_service.dart';
import '../auth/login_screen.dart';
import 'order_history_tab.dart';
import 'widgets/branch_picker_dialog.dart';

class CustomerProfileTab extends StatelessWidget {
  final SessionService sessionService;
  final OnlineCatalogService catalogService;
  final OnlineBranchDto? currentBranch;
  final List<OnlineBranchDto> branches;
  final ValueChanged<OnlineBranchDto> onBranchChanged;

  const CustomerProfileTab({
    super.key,
    required this.sessionService,
    required this.catalogService,
    required this.currentBranch,
    required this.branches,
    required this.onBranchChanged,
  });

  @override
  Widget build(BuildContext context) {
    final user = sessionService.currentUser;
    final isAuth = sessionService.isAuthenticated;

    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(title: const Text('My Profile')),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          // Profile Card
          Container(
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: AppColors.cardBackground,
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: AppColors.borderSubtle),
            ),
            child: Row(
              children: [
                CircleAvatar(
                  radius: 30,
                  backgroundColor: AppColors.primaryLight,
                  child: Text(
                    isAuth ? (user?.displayName.substring(0, 1).toUpperCase() ?? 'U') : 'G',
                    style: const TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                      color: AppColors.primary,
                    ),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        isAuth ? user?.displayName ?? 'Customer' : 'Guest Customer',
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: AppColors.textPrimary,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        isAuth
                            ? (user?.emailAddress ?? user?.userName ?? '')
                            : 'Order quickly without an account',
                        style: const TextStyle(fontSize: 12, color: AppColors.textSecondary),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),

          const SizedBox(height: 20),

          // Branch Settings
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: AppColors.cardBackground,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: AppColors.borderSubtle),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Selected Branch',
                  style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textMuted),
                ),
                const SizedBox(height: 10),
                Row(
                  children: [
                    const Icon(Icons.storefront, color: AppColors.primary),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        currentBranch?.name ?? 'No branch selected',
                        style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                      ),
                    ),
                    TextButton(
                      onPressed: () {
                        showDialog(
                          context: context,
                          builder: (_) => BranchPickerDialog(
                            branches: branches,
                            onSelect: onBranchChanged,
                          ),
                        );
                      },
                      child: const Text('Change'),
                    ),
                  ],
                ),
              ],
            ),
          ),

          const SizedBox(height: 20),

          // Order history (MAUI OrderHistoryPage)
          Container(
            margin: const EdgeInsets.only(bottom: 16),
            decoration: BoxDecoration(
              color: AppColors.cardBackground,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: AppColors.borderSubtle),
            ),
            child: Material(
              color: Colors.transparent,
              child: ListTile(
                contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                leading: const Icon(Icons.receipt_long_outlined, color: AppColors.primary),
                title: const Text('Order History', style: TextStyle(fontWeight: FontWeight.bold)),
                subtitle: const Text('Track past orders by phone number'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => OrderHistoryTab(
                        catalogService: catalogService,
                        branchId: currentBranch?.id,
                      ),
                    ),
                  );
                },
              ),
            ),
          ),

          // Portal Switch (Staff / Waiter / Rider Login)
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: AppColors.cardBackground,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: AppColors.borderSubtle),
            ),
            child: Material(
              color: Colors.transparent,
              child: Column(
              children: [
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: AppColors.primaryLight,
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: const Icon(Icons.badge_outlined, color: AppColors.primary),
                  ),
                  title: Text(
                    isAuth ? 'Switch Role / Staff Portal' : 'Staff & Rider Login',
                    style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                  ),
                  subtitle: const Text(
                    'Access Waiter, Rider, or POS Manager dashboard',
                    style: TextStyle(fontSize: 12, color: AppColors.textMuted),
                  ),
                  trailing: const Icon(Icons.chevron_right, color: AppColors.textMuted),
                  onTap: () {
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => LoginScreen(sessionService: sessionService),
                      ),
                    );
                  },
                ),
                if (isAuth) ...[
                  const Divider(color: AppColors.borderSubtle),
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: AppColors.dangerBg,
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: const Icon(Icons.logout, color: AppColors.badgeRed),
                    ),
                    title: const Text(
                      'Logout',
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 14,
                        color: AppColors.badgeRed,
                      ),
                    ),
                    onTap: () => sessionService.logout(),
                  ),
                ],
              ],
            ),
            ),
          ),

          const SizedBox(height: 30),

          // App Info
          Center(
            child: Column(
              children: [
                Text(
                  'Aljannat POS & Online Ordering',
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textMuted.withOpacity(0.8),
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Version 1.0.0 (Flutter)',
                  style: TextStyle(
                    fontSize: 11,
                    color: AppColors.textMuted.withOpacity(0.6),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

