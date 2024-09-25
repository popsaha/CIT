using CIT.API.Models;
using CIT.API.Models.Dto;

namespace CIT.API.Repository.IRepository
{
    public interface ITaskListRepository
    {
        public Task<IEnumerable<TaskList>> GetAllTaskList();

    }
}
