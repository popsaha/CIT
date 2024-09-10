namespace CIT.API.Models.Dto
{
    public class VehicleAssignmentRequestDTO
    {
        public int? LeadID { get; set; }
        public int? ChaseID { get; set; }
        public int? CrewCommanderID { get; set; }
        public DateTime VehicleAssignDate { get; set; } = DateTime.Now;
    }
}
