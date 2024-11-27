namespace CIT.API.Models
{
    public class Vehicle
    {
        public int VehicleID { get; set; }
        public string RegistrationNo { get; set; }
        public decimal Capacity { get; set; }
        public DateTime? MaintenanceDate { get; set; }
        public string? RestrictionFlag { get; set; }
        public string? RestrictionValue { get; set; }
        public string VehicleType { get; set; }
        public string? DataSource { get; set; }
        public int? IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? IsDeleted { get; set; }
    }
}
