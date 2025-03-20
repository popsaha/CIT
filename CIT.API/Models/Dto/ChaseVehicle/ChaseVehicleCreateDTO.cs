﻿namespace CIT.API.Models.Dto.ChaseVehicle
{
    public class ChaseVehicleCreateDTO
    {
        public string RegistrationNo { get; set; }
        public decimal Capacity { get; set; }
        public DateTime? MaintenanceDate { get; set; }
        public string VehicleType { get; set; }
    }
}
