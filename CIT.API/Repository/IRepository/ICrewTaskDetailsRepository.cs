﻿using CIT.API.Models.Dto.CrewTaskDetails;
using System.Threading.Tasks;

namespace CIT.API.Repository.IRepository
{
    public interface ICrewTaskDetailsRepository
    {
        
        Task<IEnumerable<CrewTaskDetailsDTO>> GetCrewTasksByCommanderIdAsync(int crewCommanderId, int userId, DateTime? orderDate = null);
        Task<CrewTaskDetailsByTaskIdDTO> GetTaskDetailsByTaskIdAsync(int crewCommanderId,int taskId, int userId); // New method to get task details by TaskId
        Task<bool> UpdateTaskStatusAsync(int crewCommanderId, int taskId, string status, CrewTaskStatusUpdateDTO updateDTO, string activityType, int userId);
        Task<bool> parcelLoadStatusAsync(int crewCommanderId, int taskId, string status, CrewTaskParcelDTO parcelDTO, string activityType, int userId);

        Task<bool> parcelUnLoadStatusAsync(int crewCommanderId, int taskId, string status, CrewTaskUnloadedParcelDTOs parcelDTO, string activityType, int userId);

        Task<bool> crewTaskFailedAsync(int crewCommanderId, int taskId, string status, CrewTaskFailedStatusDTO failedDTO, string activityType, int userId);

        Task<bool> arrivedDeliveryAsync(int crewCommanderId, int taskId, string status, CrewTaskStatusUpdateDTO arrivedDTO,  string activityType, int userId);

        Task<string> GetCurrentScreenIdByTaskId(int taskId);
        Task<string> GetNextScreenIdByTaskId(int taskId);

        Task<int> GetUserIdByUuidAsync ();

        public Task<IEnumerable<ParcelReceiptNo>> GetParcelAsync(int taskId, int authenticatedUserId, int userIdFromDb);

        public Task<ParcelCountDTO> GetParclesCountsByTaskId(int taskId);

        
    }
}
