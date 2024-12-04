namespace CIT.API.Models.Dto.UserMasterApi
{
    public class UserCreateDTO
    {
        public string? UserName { get; set; }
        public string Password { get; set; }
        public string RoleName { get; set; }
    }
}
