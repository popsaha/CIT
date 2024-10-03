using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto
{
    public class TaskListsDTO
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

    public class TaskDateDTO
    {
        [Required(ErrorMessage = "Date is required.")]
        [RegularExpression(@"\d{4}-\d{2}-\d{2}", ErrorMessage = "The date format must be yyyy-MM-dd.")]
        public string Date { get; set; }
    }

}
