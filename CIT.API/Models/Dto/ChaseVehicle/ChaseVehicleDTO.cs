namespace CIT.API.Models.Dto.ChaseVehicle
{
    public class ChaseVehicleDTO
    {
        public int VehicleID { get; set; }
        public string RegistrationNo { get; set; }
        public decimal Capacity { get; set; }
        public DateTime? MaintenanceDate { get; set; }
        public string VehicleType { get; set; }
        public bool? IsActive { get; set; }
    }
}
