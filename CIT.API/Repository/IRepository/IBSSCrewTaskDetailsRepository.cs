using CIT.API.Context;
using CIT.API.Models.Dto.BSSCrewTaskDetails;
using CIT.API.Models.Dto.CrewTaskDetails;

namespace CIT.API.Repository.IRepository
{
    public interface IBSSCrewTaskDetailsRepository
    {
        Task<int> GetUserIdByUuidAsync();
        Task<string> GetCurrentScreenIdByTaskId(int taskId);
        Task<string> GetNextScreenIdByTaskId(int taskId);
        Task<bool> UpdateTaskStatusAsync(int crewCommanderId, int taskId, string status, BSSCrewTaskStatusUpdateDTO updateDTO, string activityType, int userId);
        Task<bool> SaveAmountAsync(int crewCommanderId, int taskId, string status, BssSaveAmountDTO bssCountStatusDTO, string activityType, int userId);
        Task<bool> parcelLoadStatusAsync(int crewCommanderId, int taskId, string status, BssParcelLoadDTO parcelDTO, string activityType, int userId);
        Task<bool> arrivedDeliveryAsync(int crewCommanderId, int taskId, string status, BSSCrewTaskStatusUpdateDTO arrivedDTO, string activityType, int userId);
        public Task<IEnumerable<ParcelReceiptNo>> GetParcelAsync(int taskId, int authenticatedUserId, int userIdFromDb);
        Task<bool> parcelUnLoadStatusAsync(int crewCommanderId, int taskId, string status, BssParcelUnloadDTO parcelDTO, string activityType, int userId);
        public Task<BssParcelCountDTO> GetParclesCountsByTaskId(int taskId);
        Task<bool> crewTaskFailedAsync(int crewCommanderId, int taskId, string status, BssTaskFailedDTO failedDTO, string activityType, int userId);
        public Task<int> GetTotalAmountByTaskId(int taskId);
             
    }
}
