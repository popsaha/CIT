using CIT.API.Models.Dto;

namespace CIT.API.Repository.IRepository
{
    public interface IUserRepository
    {
        bool IsUniqueUser(string username);
        Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO);
        //Task<UserDTO> Register(RegisterationRequestDTO registerationRequestDTO);
    }
}
