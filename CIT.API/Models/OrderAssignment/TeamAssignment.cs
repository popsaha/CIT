namespace CIT.API.Models.OrderAssignment
{
    public class TeamAssignment
    {
        public int OrderRouteId { get; set; }
        public int CrewId { get; set; }
        public int LeadVehicleId { get; set; }
        public int ChaseVehicleId { get; set; }
        public DateTime AssignDate { get; set; }

    }
}
