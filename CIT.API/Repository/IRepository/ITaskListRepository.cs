using CIT.API.Models;

namespace CIT.API.Repository.IRepository
{
    public interface ITaskListRepository
    {
        public Task<IEnumerable<TaskList>> GetAllTaskList(DateTime date);

    }
}
