namespace CIT.API.Models.Dto
{
    public class OrderDTO
    {
        public int OrderTypeId { get; set; }
        public int CustomerId { get; set; }
        public string Repeats { get; set; }
        public List<TaskModel> taskmodellist { get; set; }
    }

    public class TaskModel
    {
        public int TaskID { get; set; }
        public string PickupType { get; set; }
        public string RequesterName { get; set; }
        public string PickupLocation { get; set; }
        public string VaultLocation { get; set; }
        public string RecipientName { get; set; }
        public string DeliveryLocation { get; set; }
        public DateTime PickupTime { get; set; }
        public DateTime DeliveryTime { get; set; }
        public string NoOfVehicles { get; set; }
    }
}
