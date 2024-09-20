namespace CIT.API.Models
{
    public class TaskList
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string OrderNumber { get; set; }
        public string TypeName { get; set; }
        public string PickupType { get; set; }
        public string PickupLocation { get; set; }
        public string DeliveryLocation { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
