using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.OrderAssignment
{
    public class AssignmentRequestDto
    {
        [Required]
        public List<int> CrewId { get; set; }

        [Required]
        public List<int> LeadVehicleId { get; set; }

        [Required]
        public List<int> ChaseVehicleId { get; set; }

        [Required]
        public DateTime AssignDate { get; set; }
    }
}
