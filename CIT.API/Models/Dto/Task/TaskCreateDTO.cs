using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.Task
{
    public class TaskCreateDTO
    {
        //[Required]
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
        //[Required]
        public int CustomerRecipiantId { get; set; }
        //[Required]
        public int CustomerRecipiantLocationId { get; set; }
        //[Required]
        public int RepeatId { get; set; }
        [Required]
        public string OrderCreateDate { get; set; }
        public string RepeatDaysName { get; set; }     
        public string EndOnDate { get; set; }
        public int VaultID { get; set; }
        public bool isVault { get; set; }
        public bool isVaultFinal { get; set; }
    }
}
