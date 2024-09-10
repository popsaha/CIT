namespace CIT.API.Models.Dto
{
    public class VehicleAssignmentRequestDTO
    {
        public int? GroupID { get; set; }
        public int? VehicleID { get; set; }
        public int? CrewCommanderID { get; set; }
        public int? PoliceID { get; set; }
        public int? TaskID { get; set; }
    }
}
