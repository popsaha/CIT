namespace CIT.API.Models.Dto
{
    public class OrderDTO
    {
        //public string OrderNumber { get; set; }
        //public int OrderId { get; set; }
        public int OrderTypeId { get; set; }
        public int CustomerId { get; set; }
        //public int RouteID { get; set; }
        //public int PickUpLocation { get; set; }
        //public string IsVault { get; set; }
        //public string IsVaultFinal { get; set; }
        //public bool FullDayOccupancy { get; set; }
        public string Repeats { get; set; }
        //public int PriorityId { get; set; }
        //public int CreatedBy { get; set; }
        //public DateTime StartDate { get; set; }
        //public string StartTime { get; set; }
        //public string EndTime { get; set; }
        //public DateTime EndTask { get; set; }
        //public string RepeatIn { get; set; }
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
