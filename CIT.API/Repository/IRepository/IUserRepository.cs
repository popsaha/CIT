using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Models.Dto.UserMasterApi;
using CIT.API.Models.Dto.Login;
using CIT.API.Models.Dto.Registration;
using CIT.API.Models.Dto.UserMasterApi;

namespace CIT.API.Repository.IRepository
{
    public interface IUserRepository
    {
        bool IsUniqueUser(string username);
        Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO);
        Task<LocalUser> Register(RegisterationRequestDTO registerationRequestDTO);

        Task<UserCreateDTO> CrewUserCreate(UserCreateDTO crewUserDTO);
        Task<IEnumerable<UserMasterModel>> GetAllUsers();
        Task<UserMasterModel> UpdateUser(UserMasterModel usermaster);
        Task<UserMasterModel> GetUserById(int userId);
        Task<int> DeleteUser(int userId);
    }
}
