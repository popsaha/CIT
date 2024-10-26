namespace CIT.API.Models.Dto.Login
{
    public class LoginResponseDTO
    {
        public UserMaster User { get; set; }
        public string Token { get; set; }
    }
}
