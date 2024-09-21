namespace CIT.API.Models
{
    public class TaskGroupList
    {
        public int TaskId { get; set; }
        public string PickupType { get; set; }
        public string PickupCustomerName { get; set; }
        public string PickupLocation { get; set; }
        public string DeliveryCustomerName { get; set; }
        public string DeliveryLocation { get;set; }
        public string GroupName { get; set; }
    }
}
