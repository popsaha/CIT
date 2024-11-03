using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.Task
{
    public class TaskCreateDTO
    {
        public string OrderNumber { get; set; }
        public string OrderId { get; set; }
        [Required]
        public int OrderTypeID { get; set; }
        [Required]
        public int PriorityId { get; set; }
        [Required]
        public int PickUpTypeId { get; set; }
        [Required]
        public int CustomerId { get; set; }
        [Required]
        public int BranchID { get; set; }
        public int CustomerRecipiantId { get; set; }
        public int CustomerRecipiantLocationId { get; set; }
        public int RepeatId { get; set; }
        public string OrderCreateDate { get; set; }
        public string RepeatDaysName { get; set; }
        public string EndOnDate { get; set; }
        public int VaultID { get; set; }
        public bool isVault { get; set; }
        public bool isVaultFinal { get; set; }
        public int OrderRouteId { get; set; }
        public bool NewVehicleRequired { get; set; }
        public bool fullDayCheck { get; set; }
        public bool OrderCreateStatus { get; set; }
        public int IsEditTask { get; set; }
        public int TaskId { get; set; }
        public int CreatedBy { get; set; }
    }
}
