using CIT.API.Models;

namespace CIT.API.Repository.IRepository
{
    public interface ITaskGroupListRepository
    {
        public Task<IEnumerable<TaskGroupList>> GetAllTaskGroupList();
    }
}
