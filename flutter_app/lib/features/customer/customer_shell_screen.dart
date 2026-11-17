import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../core/widgets/custom_bottom_nav.dart';
import '../../models/branch_models.dart';
import '../../services/session_service.dart';
import '../../services/online_catalog_service.dart';
import '../../services/cart_service.dart';
import 'customer_home_tab.dart';
import 'customer_order_tab.dart';
import 'favorites_tab.dart';
import 'customer_profile_tab.dart';
import 'cart_screen.dart';
import 'widgets/branch_picker_dialog.dart';

class CustomerShellScreen extends StatefulWidget {
  final SessionService sessionService;
  final OnlineCatalogService catalogService;
  final CartService cartService;

  const CustomerShellScreen({
    super.key,
    required this.sessionService,
    required this.catalogService,
    required this.cartService,
  });

  @override
  State<CustomerShellScreen> createState() => _CustomerShellScreenState();
}

class _CustomerShellScreenState extends State<CustomerShellScreen> {
  int _currentIndex = 0;
  List<OnlineBranchDto> _branches = [];
  OnlineBranchDto? _selectedBranch;
  bool _isLoadingBranches = true;
  String? _branchLoadError;

  @override
  void initState() {
    super.initState();
    _loadBranches();
  }

  Future<void> _loadBranches() async {
    setState(() {
      _isLoadingBranches = true;
      _branchLoadError = null;
    });
    try {
      final branches = await widget.catalogService.getBranches();
      OnlineBranchDto? selected;

      final savedBranchId = widget.sessionService.branchId;
      if (savedBranchId != null) {
        selected = branches.firstWhere(
          (b) => b.id == savedBranchId,
          orElse: () => branches.isNotEmpty ? branches.first : OnlineBranchDto(id: 1, name: 'Main Branch'),
        );
      } else if (branches.isNotEmpty) {
        selected = branches.first;
      }

      if (selected != null) {
        await widget.sessionService.setBranch(selected.id, selected.name);
      }

      if (mounted) {
        setState(() {
          _branches = branches;
          _selectedBranch = selected;
          _isLoadingBranches = false;
          _branchLoadError = branches.isEmpty ? 'No branches returned from API.' : null;
        });

        // Prompt user if branch isn't selected
        if (savedBranchId == null && branches.length > 1) {
          WidgetsBinding.instance.addPostFrameCallback((_) {
            showDialog(
              context: context,
              barrierDismissible: false,
              builder: (_) => BranchPickerDialog(
                branches: _branches,
                onSelect: (b) {
                  setState(() => _selectedBranch = b);
                  widget.sessionService.setBranch(b.id, b.name);
                },
              ),
            );
          });
        }
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _isLoadingBranches = false;
          _branchLoadError = e.toString();
        });
      }
    }
  }

  void _onBranchChanged(OnlineBranchDto branch) {
    setState(() => _selectedBranch = branch);
    widget.sessionService.setBranch(branch.id, branch.name);
  }

  void _openCart() {
    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => CartScreen(
          cartService: widget.cartService,
          catalogService: widget.catalogService,
          branchId: _selectedBranch?.id,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoadingBranches) {
      return const Scaffold(
        backgroundColor: AppColors.pageBackground,
        body: Center(
          child: CircularProgressIndicator(color: AppColors.primary),
        ),
      );
    }

    if (_branchLoadError != null && _branches.isEmpty) {
      return Scaffold(
        backgroundColor: AppColors.pageBackground,
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.cloud_off, size: 48, color: AppColors.textMuted),
                const SizedBox(height: 12),
                const Text(
                  'Could not load branches from production API',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                ),
                const SizedBox(height: 8),
                Text(
                  _branchLoadError!,
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
                ),
                const SizedBox(height: 16),
                ElevatedButton(
                  onPressed: _loadBranches,
                  child: const Text('Retry'),
                ),
              ],
            ),
          ),
        ),
      );
    }

    final pages = [
      CustomerHomeTab(
        catalogService: widget.catalogService,
        cartService: widget.cartService,
        currentBranch: _selectedBranch,
        branches: _branches,
        onBranchChanged: _onBranchChanged,
        onOpenCart: _openCart,
      ),
      CustomerOrderTab(
        catalogService: widget.catalogService,
        cartService: widget.cartService,
        currentBranch: _selectedBranch,
      ),
      FavoritesTab(
        catalogService: widget.catalogService,
        cartService: widget.cartService,
        branchId: _selectedBranch?.id,
      ),
      CustomerProfileTab(
        sessionService: widget.sessionService,
        catalogService: widget.catalogService,
        currentBranch: _selectedBranch,
        branches: _branches,
        onBranchChanged: _onBranchChanged,
      ),
    ];

    const navItems = [
      BottomNavItem(
        label: 'Home',
        icon: Icons.home_outlined,
        activeIcon: Icons.home_rounded,
      ),
      BottomNavItem(
        label: 'Menu',
        icon: Icons.restaurant_menu_outlined,
        activeIcon: Icons.restaurant_menu_rounded,
      ),
      BottomNavItem(
        label: 'Favorites',
        icon: Icons.favorite_border_rounded,
        activeIcon: Icons.favorite_rounded,
      ),
      BottomNavItem(
        label: 'Profile',
        icon: Icons.person_outline_rounded,
        activeIcon: Icons.person_rounded,
      ),
    ];

    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      extendBody: true,
      body: Padding(
        // Keep tab content clear of the floating fancy nav.
        padding: const EdgeInsets.only(bottom: 88),
        child: IndexedStack(
          index: _currentIndex,
          children: pages,
        ),
      ),
      bottomNavigationBar: CustomBottomNavBar(
        currentIndex: _currentIndex,
        onTap: (index) => setState(() => _currentIndex = index),
        items: navItems,
      ),
    );
  }
}

