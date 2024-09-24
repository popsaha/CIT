namespace CIT.API.Models.Dto.Vehicle
{
    public class VehicleDTO
    {
        public int VehicleID { get; set; }
        public string RegistrationNo { get; set; }
        public decimal Capacity { get; set; }
        public DateTime MaintenanceDate { get; set; }
        public string VehicleType { get; set; }
    }
}
