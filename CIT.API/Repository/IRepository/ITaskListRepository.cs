using CIT.API.Models.Dto;

namespace CIT.API.Repository.IRepository
{
    public interface ITaskListRepository
    {
        public Task<IEnumerable<TaskListDTO>> GetAllTaskList();

    }
}
