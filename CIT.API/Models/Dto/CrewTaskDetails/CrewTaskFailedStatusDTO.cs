using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.CrewTaskDetails
{
    public class CrewTaskFailedStatusDTO
    {
        //[Required(ErrorMessage = "UserId is required.")]
        //public Guid UUID { get; set; }
        [Required(ErrorMessage = "ScreenId is required.")]
        public string NextScreenId { get; set; }
        [Required]
        public DateTime Time { get; set; } = DateTime.UtcNow;
        
        public Location Location { get; set; }
        [Required(ErrorMessage = "FailureReason is required.")]
        public string FailureReason { get; set; }


    }
}