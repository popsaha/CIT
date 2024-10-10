namespace CIT.API.Models.OrderAssignment
{
    public class OrderRoutes
    {
        public int OrderId { get; set; }
        public int OrderRouteId { get; set; }
        public bool IsFullDayAssignment { get; set; }
        public string? PickupTypeName { get; set; }

    }
}
