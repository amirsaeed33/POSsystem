import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../core/constants/app_colors.dart';
import '../../core/widgets/app_button.dart';
import '../../models/order_models.dart';
import '../../services/online_catalog_service.dart';

class OrderTrackingScreen extends StatefulWidget {
  final int orderId;
  final String orderNo;
  final OnlineCatalogService catalogService;

  const OrderTrackingScreen({
    super.key,
    required this.orderId,
    required this.orderNo,
    required this.catalogService,
  });

  @override
  State<OrderTrackingScreen> createState() => _OrderTrackingScreenState();
}

class _OrderTrackingScreenState extends State<OrderTrackingScreen> {
  bool _isLoading = true;
  OrderTrackingDto? _tracking;

  @override
  void initState() {
    super.initState();
    _loadStatus();
  }

  Future<void> _loadStatus() async {
    setState(() => _isLoading = true);
    // MAUI: POST TrackOrder?orderNo=
    final order = await widget.catalogService.trackOrder(widget.orderNo);
    if (mounted) {
      setState(() {
        _tracking = order != null
            ? OrderTrackingDto.fromOrder(order)
            : OrderTrackingDto(
                orderId: widget.orderId,
                orderNo: widget.orderNo,
                status: 1,
                statusText: 'Order Confirmed',
                estimatedMinutes: 25,
              );
        _isLoading = false;
      });
    }
  }

  Future<void> _callRider(String phone) async {
    final uri = Uri.parse('tel:$phone');
    if (await canLaunchUrl(uri)) {
      await launchUrl(uri);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(
        title: Text('Track Order ${widget.orderNo}'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh, color: AppColors.primary),
            onPressed: _loadStatus,
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : ListView(
              padding: const EdgeInsets.all(20),
              children: [
                // Top Header Card
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: AppColors.cardBackground,
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: AppColors.borderSubtle),
                  ),
                  child: Column(
                    children: [
                      Container(
                        width: 60,
                        height: 60,
                        decoration: BoxDecoration(
                          color: AppColors.primaryLight,
                          shape: BoxShape.circle,
                        ),
                        child: const Center(
                          child: Icon(Icons.delivery_dining, color: AppColors.primary, size: 34),
                        ),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        _tracking?.statusText ?? 'Order Confirmed',
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: AppColors.textPrimary,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        'Estimated time: ~${_tracking?.estimatedMinutes ?? 30} minutes',
                        style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                      ),
                    ],
                  ),
                ),

                const SizedBox(height: 20),

                // Stepper Timeline
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: AppColors.cardBackground,
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: AppColors.borderSubtle),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Order Progress',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                      ),
                      const SizedBox(height: 16),
                      _buildStep(
                        title: 'Order Placed',
                        subtitle: 'We received your order',
                        isPassed: (_tracking?.status ?? 0) >= 0,
                        isCurrent: (_tracking?.status ?? 0) == 0,
                        isLast: false,
                      ),
                      _buildStep(
                        title: 'Order Confirmed',
                        subtitle: 'Restaurant accepted your order',
                        isPassed: (_tracking?.status ?? 0) >= 1,
                        isCurrent: (_tracking?.status ?? 0) == 1,
                        isLast: false,
                      ),
                      _buildStep(
                        title: 'Preparing Food',
                        subtitle: 'Our chefs are cooking your meal',
                        isPassed: (_tracking?.status ?? 0) >= 2,
                        isCurrent: (_tracking?.status ?? 0) == 2,
                        isLast: false,
                      ),
                      _buildStep(
                        title: 'Out for Delivery',
                        subtitle: 'Rider is on the way to your door',
                        isPassed: (_tracking?.status ?? 0) >= 3,
                        isCurrent: (_tracking?.status ?? 0) == 3,
                        isLast: false,
                      ),
                      _buildStep(
                        title: 'Delivered',
                        subtitle: 'Enjoy your food!',
                        isPassed: (_tracking?.status ?? 0) >= 4,
                        isCurrent: (_tracking?.status ?? 0) == 4,
                        isLast: true,
                      ),
                    ],
                  ),
                ),

                const SizedBox(height: 20),

                // Rider Info (if assigned)
                if (_tracking?.riderName != null) ...[
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.cardBackground,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: AppColors.borderSubtle),
                    ),
                    child: Row(
                      children: [
                        CircleAvatar(
                          backgroundColor: AppColors.primaryLight,
                          child: const Icon(Icons.person, color: AppColors.primary),
                        ),
                        const SizedBox(width: 14),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                _tracking!.riderName!,
                                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                              ),
                              const Text('Delivery Rider', style: TextStyle(fontSize: 12, color: AppColors.textMuted)),
                            ],
                          ),
                        ),
                        if (_tracking?.riderPhone != null)
                          IconButton(
                            icon: const Icon(Icons.phone, color: AppColors.primary),
                            onPressed: () => _callRider(_tracking!.riderPhone!),
                          ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),
                ],

                AppButton(
                  text: 'Back to Home',
                  variant: AppButtonVariant.outline,
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ],
            ),
    );
  }

  Widget _buildStep({
    required String title,
    required String subtitle,
    required bool isPassed,
    required bool isCurrent,
    required bool isLast,
  }) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Column(
          children: [
            Container(
              width: 24,
              height: 24,
              decoration: BoxDecoration(
                color: isPassed ? AppColors.primary : AppColors.gray200,
                shape: BoxShape.circle,
              ),
              child: Center(
                child: isPassed
                    ? const Icon(Icons.check, size: 14, color: Colors.white)
                    : Container(
                        width: 8,
                        height: 8,
                        decoration: const BoxDecoration(
                          color: AppColors.gray400,
                          shape: BoxShape.circle,
                        ),
                      ),
              ),
            ),
            if (!isLast)
              Container(
                width: 2,
                height: 42,
                color: isPassed ? AppColors.primary : AppColors.gray200,
              ),
          ],
        ),
        const SizedBox(width: 14),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: isPassed ? AppColors.textPrimary : AppColors.textMuted,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                subtitle,
                style: const TextStyle(fontSize: 12, color: AppColors.textSecondary),
              ),
              const SizedBox(height: 16),
            ],
          ),
        ),
      ],
    );
  }
}

