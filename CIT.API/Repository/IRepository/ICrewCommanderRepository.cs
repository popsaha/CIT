using CIT.API.Models;

namespace CIT.API.Repository.IRepository
{
    public interface ICrewCommanderRepository
    {
        public Task<IEnumerable<UserMaster>> GetAllCrewCommanderList();
    }
}
