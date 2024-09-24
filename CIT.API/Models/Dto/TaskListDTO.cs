namespace CIT.API.Models.Dto
{
    public class TaskListDTO
    {
        public int TaskId { get; set; }
        public string OrderType { get; set; }
        public string PickupCustomerName { get; set; }
        public string DeliveryCustomerName { get; set; }
        public string OrderNumber { get; set; }
        public string PickupType { get; set; }
        public string PickupLocation { get; set; }
        public string DeliveryLocation { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
