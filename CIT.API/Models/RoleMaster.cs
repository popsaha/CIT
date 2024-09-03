namespace CIT.API.Models
{
    public class RoleMaster
    {
        public int RoleID { get; set; }  // Primary key
        public string RoleName { get; set; }  // Role name
        public string DataSource { get; set; }  // Data source tracking
        public bool? IsActive { get; set; }  // Indicates if the role is active
        public int? CreatedBy { get; set; }  // User who created the record
        public DateTime? CreatedOn { get; set; }  // Timestamp of record creation
        public int? ModifiedBy { get; set; }  // User who last modified the record
        public DateTime? ModifiedOn { get; set; }  // Timestamp of last modification
        public int? DeletedBy { get; set; }  // User who deleted the record
        public DateTime? DeletedOn { get; set; }  // Timestamp of deletion
        public bool? IsDeleted { get; set; }  // Soft delete flag
    }
}
