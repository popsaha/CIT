namespace CIT.API.Models.Dto.UserMasterApi
{
    public class UserListDTO
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        //public string RoleName { get; set; }
        public int? CreadedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool? IsActive { get; set; }
    }
}
