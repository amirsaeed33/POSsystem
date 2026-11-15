using System.Collections.ObjectModel;
using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

/// <summary>
/// Central facade over session, cart, and waiter order session for consistent app-wide state access.
/// </summary>
public sealed class AppState
{
    private readonly SessionService _session;
    private readonly CartService _cart;

    public AppState(SessionService session, CartService cart)
    {
        _session = session;
        _cart = cart;

        _session.SessionChanged += (_, _) => SessionChanged?.Invoke(this, EventArgs.Empty);
        _cart.CartChanged += (_, _) => CartChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? SessionChanged;
    public event EventHandler? CartChanged;

    // --- Session ---
    public SessionService Session => _session;
    public bool IsAuthenticated => _session.IsAuthenticated;
    public UserLoginInfoDto? User => _session.User;
    public int? BranchId => _session.BranchId;
    public string? TenancyName => _session.TenancyName;
    public AppRoleMode RoleMode => _session.ResolveRoleMode();

    public Task LoadSessionAsync() => _session.LoadAsync();
    public Task LogoutAsync(bool keepGuestTenant = true) => _session.LogoutAsync(keepGuestTenant);
    public Task SetBranchAsync(int branchId) => _session.SetBranchAsync(branchId);

    // --- Cart ---
    public CartService Cart => _cart;
    public ObservableCollection<CartLine> CartLines => _cart.Lines;
    public int CartItemCount => _cart.ItemCount;
    public decimal CartSubtotal => _cart.Subtotal;

    public void ClearCart() => _cart.Clear();

    // --- Waiter session ---
    public RestaurantOrderDto? WaiterCurrentOrder
    {
        get => WaiterOrderSession.CurrentOrder;
        set => WaiterOrderSession.CurrentOrder = value;
    }

    public RestaurantTableDto? WaiterCurrentTable
    {
        get => WaiterOrderSession.CurrentTable;
        set => WaiterOrderSession.CurrentTable = value;
    }

    public void ClearWaiterSession() => WaiterOrderSession.Clear();

    public void ClearStaffWorkingState()
    {
        ClearCart();
        ClearWaiterSession();
    }
}
