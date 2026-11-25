using System.Collections.Concurrent;
using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

/// <summary>
/// Short TTL cache for staff screens so tab switches reuse the last payload
/// instead of blocking on a full API round-trip every Appearing.
/// </summary>
public sealed class StaffDataCache
{
    private static readonly TimeSpan TablesTtl = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan OrdersTtl = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan RiderTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RiderHistoryTtl = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan AdminTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PosProductsTtl = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<int, Entry<List<RestaurantTableDto>>> _tablesByBranch = new();
    private readonly ConcurrentDictionary<int, Entry<List<RestaurantOrderDto>>> _activeOrdersByBranch = new();
    private readonly ConcurrentDictionary<int, Entry<List<ProductDto>>> _posProductsByBranch = new();
    private Entry<RiderBundle>? _rider;
    private Entry<RiderHistoryBundle>? _riderHistory;
    private Entry<AdminBundle>? _admin;

    public bool TryGetTables(int branchId, out List<RestaurantTableDto>? tables)
    {
        tables = null;
        if (!_tablesByBranch.TryGetValue(branchId, out var entry) || entry.IsExpired)
        {
            return false;
        }

        tables = entry.Value;
        return true;
    }

    public void SetTables(int branchId, IReadOnlyList<RestaurantTableDto> tables) =>
        _tablesByBranch[branchId] = new Entry<List<RestaurantTableDto>>(tables.ToList(), TablesTtl);

    public void InvalidateTables(int? branchId = null)
    {
        if (branchId is int id)
        {
            _tablesByBranch.TryRemove(id, out _);
            return;
        }

        _tablesByBranch.Clear();
    }

    public bool TryGetActiveOrders(int branchId, out List<RestaurantOrderDto>? orders)
    {
        orders = null;
        if (!_activeOrdersByBranch.TryGetValue(branchId, out var entry) || entry.IsExpired)
        {
            return false;
        }

        orders = entry.Value;
        return true;
    }

    public void SetActiveOrders(int branchId, IReadOnlyList<RestaurantOrderDto> orders) =>
        _activeOrdersByBranch[branchId] = new Entry<List<RestaurantOrderDto>>(orders.ToList(), OrdersTtl);

    public void InvalidateActiveOrders(int? branchId = null)
    {
        if (branchId is int id)
        {
            _activeOrdersByBranch.TryRemove(id, out _);
            return;
        }

        _activeOrdersByBranch.Clear();
    }

    public bool TryGetPosProducts(int branchId, out List<ProductDto>? products)
    {
        products = null;
        if (!_posProductsByBranch.TryGetValue(branchId, out var entry) || entry.IsExpired)
        {
            return false;
        }

        products = entry.Value;
        return true;
    }

    public void SetPosProducts(int branchId, IReadOnlyList<ProductDto> products) =>
        _posProductsByBranch[branchId] = new Entry<List<ProductDto>>(products.ToList(), PosProductsTtl);

    public bool TryGetRider(out RiderBundle? bundle)
    {
        bundle = null;
        if (_rider == null || _rider.IsExpired)
        {
            return false;
        }

        bundle = _rider.Value;
        return true;
    }

    public void SetRider(RiderDashboardStatsDto? stats, IReadOnlyList<RiderOrderDto> orders) =>
        _rider = new Entry<RiderBundle>(new RiderBundle(stats, orders.ToList()), RiderTtl);

    public void InvalidateRider() => _rider = null;

    public bool TryGetRiderHistory(string? keyword, out List<RiderOrderDto>? orders)
    {
        orders = null;
        if (_riderHistory == null || _riderHistory.IsExpired)
        {
            return false;
        }

        var key = NormalizeKeyword(keyword);
        if (!string.Equals(_riderHistory.Value.Keyword, key, StringComparison.Ordinal))
        {
            return false;
        }

        orders = _riderHistory.Value.Orders;
        return true;
    }

    public void SetRiderHistory(string? keyword, IReadOnlyList<RiderOrderDto> orders) =>
        _riderHistory = new Entry<RiderHistoryBundle>(
            new RiderHistoryBundle(NormalizeKeyword(keyword), orders.ToList()),
            RiderHistoryTtl);

    public bool TryGetAdmin(out AdminBundle? bundle)
    {
        bundle = null;
        if (_admin == null || _admin.IsExpired)
        {
            return false;
        }

        bundle = _admin.Value;
        return true;
    }

    public void SetAdmin(DashboardDto dashboard, OnlineOrderPipelineDto pipeline) =>
        _admin = new Entry<AdminBundle>(new AdminBundle(dashboard, pipeline), AdminTtl);

    public void InvalidateAdmin() => _admin = null;

    private static string NormalizeKeyword(string? keyword) =>
        (keyword ?? string.Empty).Trim().ToLowerInvariant();

    public sealed record RiderBundle(RiderDashboardStatsDto? Stats, List<RiderOrderDto> Orders);

    public sealed record RiderHistoryBundle(string Keyword, List<RiderOrderDto> Orders);

    public sealed record AdminBundle(DashboardDto Dashboard, OnlineOrderPipelineDto Pipeline);

    private sealed class Entry<T>
    {
        public Entry(T value, TimeSpan ttl)
        {
            Value = value;
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl);
        }

        public T Value { get; }
        public DateTimeOffset ExpiresAt { get; }
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    }
}
