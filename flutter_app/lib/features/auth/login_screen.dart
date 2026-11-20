import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../core/widgets/app_button.dart';
import '../../services/session_service.dart';
import '../../services/waiter_service.dart';
import '../../services/rider_service.dart';
import '../../services/dashboard_service.dart';
import '../waiter/waiter_tables_screen.dart';
import '../rider/rider_shell_screen.dart';
import '../admin/admin_dashboard_screen.dart';

class LoginScreen extends StatefulWidget {
  final SessionService sessionService;

  const LoginScreen({super.key, required this.sessionService});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final TextEditingController _tenantController = TextEditingController();
  final TextEditingController _userController = TextEditingController();
  final TextEditingController _passwordController = TextEditingController();
  bool _obscurePassword = true;
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _tenantController.text = widget.sessionService.tenancyName ?? '';
  }

  @override
  void dispose() {
    _tenantController.dispose();
    _userController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _handleLogin() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _isLoading = true);

    try {
      await widget.sessionService.login(
        tenancyName: _tenantController.text.trim().isNotEmpty ? _tenantController.text.trim() : null,
        userName: _userController.text.trim(),
        password: _passwordController.text,
      );

      if (mounted) {
        setState(() => _isLoading = false);

        final api = widget.sessionService.apiClient;
        // Same priority as web/MAUI: rider → waiter → admin → customer
        final role = widget.sessionService.staffHomeRole;
        if (role == 'rider') {
          Navigator.of(context).pushAndRemoveUntil(
            MaterialPageRoute(
              builder: (_) => RiderShellScreen(
                sessionService: widget.sessionService,
                riderService: RiderService(api),
              ),
            ),
            (_) => false,
          );
        } else if (role == 'waiter') {
          Navigator.of(context).pushReplacement(
            MaterialPageRoute(
              builder: (_) => WaiterTablesScreen(
                sessionService: widget.sessionService,
                waiterService: WaiterService(api),
              ),
            ),
          );
        } else if (role == 'admin') {
          Navigator.of(context).pushReplacement(
            MaterialPageRoute(
              builder: (_) => AdminDashboardScreen(
                sessionService: widget.sessionService,
                dashboardService: DashboardService(api),
              ),
            ),
          );
        } else {
          Navigator.of(context).pop();
        }
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isLoading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.toString()),
            backgroundColor: AppColors.badgeRed,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(
        title: const Text('Staff & Rider Portal'),
      ),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Center(
                    child: Container(
                      width: 72,
                      height: 72,
                      decoration: BoxDecoration(
                        color: AppColors.primaryLight,
                        borderRadius: BorderRadius.circular(22),
                      ),
                      child: const Center(
                        child: Icon(Icons.lock_person_outlined, color: AppColors.primary, size: 36),
                      ),
                    ),
                  ),
                  const SizedBox(height: 20),
                  const Text(
                    'Welcome Back',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 6),
                  const Text(
                    'Sign in with your POS staff, waiter, or rider credentials',
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 13, color: AppColors.textSecondary),
                  ),
                  const SizedBox(height: 28),

                  // Tenancy Input
                  TextFormField(
                    controller: _tenantController,
                    decoration: const InputDecoration(
                      labelText: 'Tenancy Name (Optional)',
                      hintText: 'Leave empty for default tenancy',
                      prefixIcon: Icon(Icons.business_outlined, color: AppColors.textMuted),
                    ),
                  ),
                  const SizedBox(height: 14),

                  // Username / Email Input
                  TextFormField(
                    controller: _userController,
                    decoration: const InputDecoration(
                      labelText: 'Username or Email *',
                      prefixIcon: Icon(Icons.person_outline, color: AppColors.textMuted),
                    ),
                    validator: (v) =>
                        v == null || v.trim().isEmpty ? 'Please enter your username' : null,
                  ),
                  const SizedBox(height: 14),

                  // Password Input
                  TextFormField(
                    controller: _passwordController,
                    obscureText: _obscurePassword,
                    decoration: InputDecoration(
                      labelText: 'Password *',
                      prefixIcon: const Icon(Icons.lock_outline, color: AppColors.textMuted),
                      suffixIcon: IconButton(
                        icon: Icon(
                          _obscurePassword ? Icons.visibility_outlined : Icons.visibility_off_outlined,
                          color: AppColors.textMuted,
                        ),
                        onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                      ),
                    ),
                    validator: (v) =>
                        v == null || v.isEmpty ? 'Please enter your password' : null,
                  ),
                  const SizedBox(height: 24),

                  // Sign In Button
                  AppButton(
                    text: 'Sign In',
                    isLoading: _isLoading,
                    onPressed: _handleLogin,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

