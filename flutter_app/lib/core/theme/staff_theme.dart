import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../constants/app_colors.dart';

/// Shared visual language for Waiter / Rider / Admin (Aljannat staff apps).
class StaffTheme {
  StaffTheme._();

  // Warm hospitality (waiter)
  static const Color waiterInk = Color(0xFF1A1410);
  static const Color waiterPaper = Color(0xFFFFF8F3);
  static const Color waiterCard = Color(0xFFFFFCF9);
  static const Color waiterAccent = Color(0xFFC84B11);
  static const Color waiterSage = Color(0xFF3D6B5A);

  // OSM / route-inspired (rider)
  static const Color riderInk = Color(0xFF12202B);
  static const Color riderPaper = Color(0xFFF3F7F4);
  static const Color riderCard = Color(0xFFFFFFFF);
  static const Color riderRoute = Color(0xFF2A7F62);
  static const Color riderWater = Color(0xFF5B8FA8);
  static const Color riderRoad = Color(0xFFD9C4A5);

  // Executive command (admin)
  static const Color adminInk = Color(0xFF14100E);
  static const Color adminPaper = Color(0xFF1C1714);
  static const Color adminCard = Color(0xFF26201C);
  static const Color adminGold = Color(0xFFE0A45A);
  static const Color adminCream = Color(0xFFF6EDE4);
  static const Color adminLine = Color(0xFF3A322C);

  static TextStyle display({Color? color, double size = 28, FontWeight weight = FontWeight.w700}) =>
      GoogleFonts.fraunces(
        color: color ?? waiterInk,
        fontSize: size,
        fontWeight: weight,
        height: 1.1,
      );

  static TextStyle title({Color? color, double size = 18, FontWeight weight = FontWeight.w700}) =>
      GoogleFonts.manrope(
        color: color ?? waiterInk,
        fontSize: size,
        fontWeight: weight,
      );

  static TextStyle body({Color? color, double size = 14, FontWeight weight = FontWeight.w500}) =>
      GoogleFonts.manrope(
        color: color ?? AppColors.textSecondary,
        fontSize: size,
        fontWeight: weight,
      );

  static BoxDecoration softCard({Color? color, Color? border}) => BoxDecoration(
        color: color ?? waiterCard,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: border ?? AppColors.borderSubtle),
        boxShadow: const [
          BoxShadow(color: Color(0x14000000), blurRadius: 18, offset: Offset(0, 8)),
        ],
      );

  static BoxDecoration heroWash({required List<Color> colors}) => BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: colors,
        ),
      );
}

class StaffMetricTile extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color accent;
  final Color? background;
  final Color? foreground;

  const StaffMetricTile({
    super.key,
    required this.label,
    required this.value,
    required this.icon,
    required this.accent,
    this.background,
    this.foreground,
  });

  @override
  Widget build(BuildContext context) {
    final fg = foreground ?? StaffTheme.waiterInk;
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: StaffTheme.softCard(color: background),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: accent.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(icon, color: accent, size: 20),
          ),
          const SizedBox(height: 14),
          Text(value, style: StaffTheme.title(color: fg, size: 22)),
          const SizedBox(height: 4),
          Text(label, style: StaffTheme.body(color: fg.withValues(alpha: 0.65), size: 12)),
        ],
      ),
    );
  }
}
