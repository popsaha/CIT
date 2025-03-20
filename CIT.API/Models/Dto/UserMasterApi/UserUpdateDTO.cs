namespace CIT.API.Models.Dto.UserMasterApi
{
    public class UserUpdateDTO
    {
        public int? UserId {  get; set; } 
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? RoleName { get; set; }
        public string RegionName { get; set; }
        public bool? IsActive { get; set; }
    }
}
