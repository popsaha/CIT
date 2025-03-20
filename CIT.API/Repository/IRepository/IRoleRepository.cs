using CIT.API.Models;

namespace CIT.API.Repository.IRepository
{
    public interface IRoleRepository
    {
        Task<IEnumerable<RoleMaster>> GetAllRole();
    }
}
