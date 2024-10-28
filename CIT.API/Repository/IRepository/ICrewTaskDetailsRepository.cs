using CIT.API.Models.Dto.CrewTaskDetails;

namespace CIT.API.Repository.IRepository
{
    public interface ICrewTaskDetailsRepository
    {
        
        Task<IEnumerable<CrewTaskDetailsDTO>> GetCrewTasksByCommanderIdAsync(int crewCommanderId);
        Task<CrewTaskDetailsDTO> GetTaskDetailsByTaskIdAsync(int crewCommanderId,int taskId); // New method to get task details by TaskId
        Task<bool> UpdateTaskStatusAsync(int crewCommanderId, int taskId, string status);


    }
}
