namespace CIT.API.Models.OrderAssignment
{
    public class OrderRoutes
    {
        public int OrderId { get; set; }
        public int OrderRouteId { get; set; }
        public int IsFullDayAssignment { get; set; } // 1 for full-day, 0 otherwise
        public string? PickupTypeName { get; set; }

    }
}
