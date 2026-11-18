import 'package:flutter/material.dart';

/// Restaurant vs retail/store UI copy and icons.
/// Food wording/icons only for restaurant branches.
class BranchUiCopy {
  const BranchUiCopy._();

  static String searchHint(bool isRestaurant) => isRestaurant
      ? 'Search delicious meals, burgers, pizza...'
      : 'Search products, brands, categories...';

  static String catalogSearchHint(bool isRestaurant) =>
      isRestaurant ? 'Search menu items...' : 'Search products...';

  static String catalogTabLabel(bool isRestaurant) =>
      isRestaurant ? 'Menu' : 'Shop';

  static IconData catalogTabIcon(bool isRestaurant) => isRestaurant
      ? Icons.restaurant_menu_outlined
      : Icons.storefront_outlined;

  static IconData catalogTabActiveIcon(bool isRestaurant) => isRestaurant
      ? Icons.restaurant_menu_rounded
      : Icons.storefront_rounded;

  static String catalogTitle(bool isRestaurant) =>
      isRestaurant ? 'Full Menu' : 'All Products';

  static String emptyFavoritesHint(bool isRestaurant) => isRestaurant
      ? 'Tap the heart on a meal to save it'
      : 'Tap the heart on a product to save it';

  static String emptyCartHint(bool isRestaurant) => isRestaurant
      ? 'Browse delicious food and add items to your cart!'
      : 'Browse products and add items to your cart!';

  static String exploreCatalogCta(bool isRestaurant) =>
      isRestaurant ? 'Explore Menu' : 'Browse Products';

  static String orderConfirmedSubtitle(bool isRestaurant) => isRestaurant
      ? 'Restaurant accepted your order'
      : 'Store accepted your order';

  static String preparingTitle(bool isRestaurant) =>
      isRestaurant ? 'Preparing Food' : 'Preparing Order';

  static String preparingSubtitle(bool isRestaurant) => isRestaurant
      ? 'Our chefs are cooking your meal'
      : 'Your items are being packed';

  static String deliveredSubtitle(bool isRestaurant) =>
      isRestaurant ? 'Enjoy your food!' : 'Thanks for shopping with us!';
}
