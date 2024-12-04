
using CIT.API.Models;
using CIT.API.Models.Dto.User;

namespace CIT.API.Repository.IRepository
{
    public interface ILocalUserRepository
    {
        public Task<IEnumerable<User>> GetAllUsers();
        Task<int> AddUser(LocalUserCreateDTO userCreateDTO);
        Task<User> GetUser(int userId);
        Task<User> UpdateUser(User user);
        Task<int> DeleteUser(int userId, int deletedBy);
    }
}
