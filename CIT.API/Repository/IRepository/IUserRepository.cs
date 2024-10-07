using CIT.API.Models;
using CIT.API.Models.Dto.Login;
using CIT.API.Models.Dto.Registration;

namespace CIT.API.Repository.IRepository
{
    public interface IUserRepository
    {
        bool IsUniqueUser(string username);
        Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO);
        Task<LocalUser> Register(RegisterationRequestDTO registerationRequestDTO);
    }
}
