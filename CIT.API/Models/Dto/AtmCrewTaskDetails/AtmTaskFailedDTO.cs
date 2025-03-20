using CIT.API.Models.Dto.CrewTaskDetails;
using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.AtmCrewTaskDetails
{
    public class AtmTaskFailedDTO
    {
        
        public string NextScreenId { get; set; }
        [Required]
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public Location Location { get; set; }
        
        public string FailureReason { get; set; }
    }
}
