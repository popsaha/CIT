namespace CIT.API.Models
{
    public class UserMaster
    {
        public int UserID { get; set; }  // Primary key
        public string UserName { get; set; }  // User's username
        public string Email { get; set; }  // User's email address
        public string PasswordHash { get; set; }  // Hashed password
        public int? RoleID { get; set; }  // Foreign key to RoleMaster
        public string DataSource { get; set; }  // Data source tracking
        public bool? IsActive { get; set; }  // Indicates if the user is active
        public int? CreatedBy { get; set; }  // User who created the record
        public DateTime? CreatedOn { get; set; }  // Timestamp of record creation
        public int? ModifiedBy { get; set; }  // User who last modified the record
        public DateTime? ModifiedOn { get; set; }  // Timestamp of last modification
        public int? DeletedBy { get; set; }  // User who deleted the record
        public DateTime? DeletedOn { get; set; }  // Timestamp of deletion
        public bool? IsDeleted { get; set; }  // Soft delete flag

        public RoleMaster Role { get; set; }  // Navigation property to RoleMaster
    }
}
