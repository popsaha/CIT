using CIT.API.Models.Dto.CrewTaskDetails;
using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.AtmCrewTaskDetails
{
    public class AtmCrewTaskStatusUpdateDTO
    {
        public string NextScreenId { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public Location Location { get; set; }
    }

    
}
