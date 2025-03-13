using CIT.API.Models.Dto.CrewTaskDetails;
using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.BSSCrewTaskDetails
{
    public class BssTaskFailedDTO
    {
        [Required(ErrorMessage = "ScreenId is required.")]
        public string NextScreenId { get; set; }
        [Required]
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public Location Location { get; set; }
        [Required(ErrorMessage = "FailureReason is required.")]
        public string FailureReason { get; set; }
    }
}
