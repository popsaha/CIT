namespace CIT.API.Models.Dto
{
    public class VehicleAssignmentRequestDTO
    {
        public List<int>? LeadID { get; set; } 
        public List<int>? ChaseID { get; set; }  
        public List<int>? CrewCommanderID { get; set; }
    }
}
