using CIT.API.Models.Dto;
using CIT.API.Models;

namespace CIT.API.Repository.IRepository
{
    public interface ITaskGroupRepository
    {
        IEnumerable<TaskGrouping> GetAllTaskGroup();

        List<TaskGroupingRequestDTO> AddTaskGroups(List<TaskGroupingRequestDTO> taskGroupingRequestDTOs);

        bool DeleteTaskGroup(int id);
    }
}
