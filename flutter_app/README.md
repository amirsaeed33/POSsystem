# Aljannat POS & Online Ordering - Flutter Mobile App

This is the complete Flutter rewrite of the **SmartPos.Mobile** .NET MAUI application, designed for superior performance (60/120 FPS), instantaneous Hot Reload, and a clean reactive architecture.

---

## 🚀 Features Ported from MAUI

1. **Customer Experience**:
   - **Splash Screen**: Auto-boots and detects active role or guest mode.
   - **Branch Selection**: Select any branch or restaurant location (`OnlineBranchDto`).
   - **Home Storefront**: Hero banner slides, category chips, search bar, deals, and product grid.
   - **Interactive Product Modal**: Modifier groups (required/optional, single vs multi-choice), special cooking notes, quantity stepper, and live subtotal calculation.
   - **Cart & Checkout**: Delivery 🛵 vs Takeaway 🛍️ selector, delivery fee calculation, customer contact form, order submission.
   - **Order Tracking**: Visual status timeline (Placed -> Confirmed -> Preparing -> Out for Delivery -> Delivered) with rider contact details.
   - **Favorites & History**: Saved favorite items and past order tracking.

2. **Waiter Mode**:
   - Tables Grid with live status (Available, Occupied, Reserved, Billed).
   - Open table session with guest/covers counter.
   - Send to Kitchen (KOT), Print Customer Bill, and Settle Order.

3. **Rider Mode**:
   - Assigned delivery list with customer contact button (dialer) and address.
   - One-tap status updates (Out for delivery, Delivered).

4. **Admin Dashboard**:
   - Quick metrics: Today's sales, active orders, table occupancy, and quick actions.

5. **ABP Backend Client**:
   - Connects to `https://aljannatapp.com`
   - Handles ABP envelope responses (`{ result, success, error }`).
   - Automatically injects `Authorization: Bearer <token>`, `Abp.TenantId`, and `SmartPos.BranchId`.

---

## 📁 Project Structure

```text
flutter_app/
├── assets/
│   ├── images/       # All 78 SVGs, logos, food photos ported from MAUI
│   └── fonts/        # OpenSans fonts ported from MAUI
├── lib/
│   ├── core/
│   │   ├── api/          # AbpApiClient with Dio interceptors
│   │   ├── constants/    # AppColors (#C84B11), AppEndpoints
│   │   ├── storage/      # SecureStorage for tokens, tenant, branch
│   │   ├── theme/        # Material 3 light theme
│   │   └── widgets/      # AppButton, CustomBottomNavBar
│   ├── models/           # Dart DTOs matching MAUI models
│   ├── services/         # SessionService, CatalogService, CartService, WaiterService, RiderService
│   ├── features/
│   │   ├── auth/         # LoginScreen with tenant discovery & role routing
│   │   ├── customer/     # Storefront, Menu, Cart, ProductDetailSheet, TrackOrder
│   │   ├── waiter/       # WaiterTablesScreen
│   │   ├── rider/        # RiderDashboardScreen
│   │   └── admin/        # AdminDashboardScreen
│   └── main.dart         # App entry point
└── pubspec.yaml
```

---

## 🛠️ Running the App in VS Code

1. Open `e:\Github\POSsystem\flutter_app` in **VS Code**.
2. Make sure your Android device or emulator is connected.
3. Press **F5** (or `Run -> Start Debugging`).

