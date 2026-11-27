import 'dart:ui';
import 'package:flutter/material.dart';
import '../constants/app_colors.dart';

class BottomNavItem {
  final String label;
  final IconData icon;
  final IconData? activeIcon;
  final int badgeCount;

  const BottomNavItem({
    required this.label,
    required this.icon,
    this.activeIcon,
    this.badgeCount = 0,
  });
}

class CustomBottomNavBar extends StatelessWidget {
  final int currentIndex;
  final ValueChanged<int> onTap;
  final List<BottomNavItem> items;

  const CustomBottomNavBar({
    super.key,
    required this.currentIndex,
    required this.onTap,
    required this.items,
  });

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.of(context).padding.bottom;

    return Padding(
      padding: EdgeInsets.fromLTRB(20, 0, 20, bottomInset > 0 ? bottomInset : 16),
      child: DecoratedBox(
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(36),
          boxShadow: const [
            BoxShadow(
              color: Color(0x28000000),
              blurRadius: 32,
              offset: Offset(0, 16),
              spreadRadius: -8,
            ),
            BoxShadow(
              color: Color(0x22C84B11),
              blurRadius: 24,
              offset: Offset(0, 10),
            ),
            BoxShadow(
              color: Color(0x14FFFFFF),
              blurRadius: 1,
              offset: Offset(0, -1),
            ),
          ],
        ),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(36),
          child: BackdropFilter(
            filter: ImageFilter.blur(sigmaX: 22, sigmaY: 22),
            child: Container(
              height: 68,
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(36),
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [
                    Colors.white.withValues(alpha: 0.96),
                    const Color(0xFFFFF6F0).withValues(alpha: 0.90),
                  ],
                ),
                border: Border.all(
                  color: Colors.white.withValues(alpha: 0.95),
                  width: 1.4,
                ),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                children: List.generate(items.length, (index) {
                  final selected = index == currentIndex;
                  return _FancyNavTile(
                    item: items[index],
                    selected: selected,
                    onTap: () => onTap(index),
                  );
                }),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _FancyNavTile extends StatefulWidget {
  final BottomNavItem item;
  final bool selected;
  final VoidCallback onTap;

  const _FancyNavTile({
    required this.item,
    required this.selected,
    required this.onTap,
  });

  @override
  State<_FancyNavTile> createState() => _FancyNavTileState();
}

class _FancyNavTileState extends State<_FancyNavTile>
    with SingleTickerProviderStateMixin {
  late final AnimationController _press;

  @override
  void initState() {
    super.initState();
    _press = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 110),
      lowerBound: 0.92,
      upperBound: 1,
      value: 1,
    );
  }

  @override
  void dispose() {
    _press.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final iconData = widget.selected
        ? (widget.item.activeIcon ?? widget.item.icon)
        : widget.item.icon;

    return GestureDetector(
      onTapDown: (_) => _press.reverse(),
      onTapUp: (_) {
        _press.forward();
        widget.onTap();
      },
      onTapCancel: () => _press.forward(),
      behavior: HitTestBehavior.opaque,
      child: Semantics(
        label: widget.item.label,
        button: true,
        selected: widget.selected,
        child: ScaleTransition(
          scale: _press,
          child: AnimatedSize(
            duration: const Duration(milliseconds: 380),
            curve: Curves.easeOutCubic,
            alignment: Alignment.center,
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 380),
              curve: Curves.easeOutCubic,
              height: 52,
              padding: EdgeInsets.symmetric(
                horizontal: widget.selected ? 16 : 14,
              ),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(28),
                gradient: widget.selected
                    ? const LinearGradient(
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                        colors: [
                          Color(0xFFFFF3EB),
                          Color(0xFFF7D9C4),
                        ],
                      )
                    : null,
                boxShadow: widget.selected
                    ? const [
                        BoxShadow(
                          color: Color(0x28C84B11),
                          blurRadius: 14,
                          offset: Offset(0, 5),
                        ),
                      ]
                    : null,
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  AnimatedScale(
                    scale: widget.selected ? 1.06 : 1,
                    duration: const Duration(milliseconds: 320),
                    curve: Curves.easeOutBack,
                    child: Stack(
                      clipBehavior: Clip.none,
                      children: [
                        Icon(
                          iconData,
                          size: 23,
                          color: widget.selected
                              ? AppColors.primary
                              : const Color(0xFF9A8B80),
                        ),
                        if (widget.item.badgeCount > 0)
                          Positioned(
                            top: -5,
                            right: -8,
                            child: Container(
                              padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
                              constraints: const BoxConstraints(minWidth: 16, minHeight: 16),
                              decoration: const BoxDecoration(
                                color: AppColors.primary,
                                shape: BoxShape.circle,
                              ),
                              alignment: Alignment.center,
                              child: Text(
                                widget.item.badgeCount > 99
                                    ? '99+'
                                    : '${widget.item.badgeCount}',
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 9,
                                  fontWeight: FontWeight.w800,
                                  height: 1.1,
                                ),
                              ),
                            ),
                          ),
                      ],
                    ),
                  ),
                  ClipRect(
                    child: AnimatedAlign(
                      duration: const Duration(milliseconds: 380),
                      curve: Curves.easeOutCubic,
                      alignment: Alignment.centerLeft,
                      widthFactor: widget.selected ? 1 : 0,
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const SizedBox(width: 8),
                          Text(
                            widget.item.label,
                            maxLines: 1,
                            softWrap: false,
                            style: const TextStyle(
                              color: AppColors.primary,
                              fontWeight: FontWeight.w700,
                              fontSize: 13.5,
                              letterSpacing: 0.15,
                              height: 1.1,
                            ),
                          ),
                        ],
                      ),
                    ),
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
