namespace CIT.API.Models.Dto.CrewTaskDetails
{
    public class CrewTaskDetailsDTO
    {
        public int CrewCommanderId { get; set; }
        public int TaskId { get; set; }
        public int OrderId { get; set; }
        public string PickupCustomer { get; set; }
        public string PickupLocation { get; set; }
        public string DeliveryCustomer { get; set; }
        public string DeliveryLocation { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
       
    }
}
