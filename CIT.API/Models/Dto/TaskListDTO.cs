namespace CIT.API.Models.Dto
{
    public class TaskListDTO
    {
        public string CustomerName { get; set; }
        public string OrderNumber { get; set; }
        public string TypeName { get; set; }
        public string PickupType { get; set; }
        public string PickupLocation { get; set; }
        public string DeliveryLocation { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
