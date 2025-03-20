namespace CIT.API.Models.Dto.Task
{
    public class OrderTaskGetData
    {
        public int orderId { get; set; }
        public string OrderNumber { get; set; }
        public int OrderTypeID { get; set; }
        public int PickUpTypeId { get; set; }
        public int Priority { get; }
        public DateTime OrderCreateDate { get; set; }
        public int OrderType { get; set; }
        public bool IsFullDayAssignment { get; set; }
        public int OrderRouteID { get; set; }
        public int OrderSubRouteID { get; set; }
        public int NoOfVehicles { get; set; }
        public int SubRouteNumber { get; set; }
    }
}
