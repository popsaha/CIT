using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.Order
{
    public class OrderListDTO
    {
        public int OrderID { get; set; }
        public int OrderNumber { get; set; }
        public string? OrderType { get; set; }
        public string? OrderDate { get; set; }
        public string? RouteName { get; set; }

        //public List<TaskListDTO> Tasks { get; set; } = new();
        public int TaskID { get; set; }
        public string? PickupCustomerName { get; set; }
        public string? PickupLocation { get; set; }
        public string? DeliveryCustomerName { get; set; }
        public string? DeliveryLocation { get; set; }
        public string? PickupType { get; set; }
    }
    public class TaskListDTO
    {
        public int TaskID { get; set; }
        public string? PickupCustomerName { get; set; }
        public string? PickupLocation { get; set; }
        public string? DeliveryCustomerName { get; set; }
        public string? DeliveryLocation { get; set; }
        public string? PickupType { get; set; }
    }
    public class OrderDateDTO
    {
        [Required(ErrorMessage = "Date is required.")]
        [RegularExpression(@"\d{4}-\d{2}-\d{2}", ErrorMessage = "The date format must be yyyy-MM-dd.")]
        public string? Date { get; set; }
    }
}
