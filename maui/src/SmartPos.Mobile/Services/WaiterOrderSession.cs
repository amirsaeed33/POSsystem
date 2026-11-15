using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public static class WaiterOrderSession
{
    public static RestaurantOrderDto? CurrentOrder { get; set; }
    public static RestaurantTableDto? CurrentTable { get; set; }

    public static void Clear()
    {
        CurrentOrder = null;
        CurrentTable = null;
    }
}
