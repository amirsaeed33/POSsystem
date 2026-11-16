namespace SmartPos.Restaurant
{
    /// <summary>Kitchen prep status for KDS tickets (independent of Open/Billed/Settled).</summary>
    public enum RestaurantKitchenStatus : byte
    {
        None = 0,
        InProgress = 1,
        ReadyToPick = 2
    }
}
