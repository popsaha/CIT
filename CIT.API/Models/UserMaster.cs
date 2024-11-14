namespace CIT.API.Models
{
    public class UserMaster
    {
        public int UserID { get; set; }  // Primary key
        public string UserName { get; set; }  // User's username
        //public string PasswordHash { get; set; }  // Hashed password
        public string Role { get; set; }  // Foreign key to RoleMaster
        public int RegionID { get; set; }
        public Guid UUID { get; set; }

    }
}
