﻿using CIT.API.Models.Dto.CrewTaskDetails;

namespace CIT.API.Repository.IRepository
{
    public interface ICrewTaskDetailsRepository
    {
        
        Task<IEnumerable<CrewTaskDetailsDTO>> GetCrewTasksByCommanderIdAsync(int crewCommanderId,int userId);
        Task<CrewTaskDetailsDTO> GetTaskDetailsByTaskIdAsync(int crewCommanderId,int taskId, int userId); // New method to get task details by TaskId
        Task<bool> UpdateTaskStatusAsync(int crewCommanderId, int taskId, string status, CrewTaskStatusUpdateDTO updateDTO, string activityType);
        Task<bool> parcelLoadStatusAsync(int crewCommanderId, int taskId, string status, CrewTaskParcelDTO parcelDTO, string activityType);

        Task<bool> crewTaskFailedAsync(int crewCommanderId, int taskId, string status, CrewTaskFailedStatusDTO failedDTO, string activityType);

     

    }
}
