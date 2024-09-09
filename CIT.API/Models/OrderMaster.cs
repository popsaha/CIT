namespace CIT.API.Models
{
    public class OrderMaster
    {
        public string OrderNumber { get; set; }
        public int OrderId { get; set; }
        public int OrderTypeId { get; set; }
        public int CustomerId { get; set; }
        public int RouteID { get; set; }
        public int PickUpLocation { get; set; }
        public string IsVault { get; set; }
        public string IsVaultFinal { get; set; }
        public bool FullDayOccupancy { get; set; }
        public int Repeats { get; set; }
        public int PriorityId { get; set; }
        public int CreatedBy { get; set; }
        public DateTime StartDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public DateTime EndTask { get; set; }
        public string RepeatIn { get; set; }
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
    public class CustomerMaster
    {
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
    }

    public class OrderResponse
    {
        public List<OrderMaster> orderlist { get; set; }
        public List<CustomerMaster> customerMasterslist { get; set; }
        public List<OrderTypeMaster> OrdertypeMasterlist { get; set; }
    }
}
