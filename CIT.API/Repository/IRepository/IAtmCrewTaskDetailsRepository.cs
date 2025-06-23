using CIT.API.Models.Dto.AtmCrewTaskDetails;
using CIT.API.Models.Dto.BSSCrewTaskDetails;
using CIT.API.Models.Dto.CrewTaskDetails;

namespace CIT.API.Repository.IRepository
{
    public interface IAtmCrewTaskDetailsRepository
    {
        Task<int> GetUserIdByUuidAsync();
        Task<string> GetCurrentScreenIdByTaskId(int taskId);
        Task<string> GetNextScreenIdByTaskId(int taskId);
        Task<bool> UpdateTaskStatusAsync(int crewCommanderId, int taskId, string status, AtmCrewTaskStatusUpdateDTO updateDTO, string activityType, int userId);
        
        Task<bool> ParcelLoadStatusAsync(int crewCommanderId, int taskId, string status, AtmParcelLoadedDTO cassetteDTO, string activityType, int userId);
        Task<bool> arrivedDeliveryAsync(int crewCommanderId, int taskId, string status, AtmCrewTaskStatusUpdateDTO arrivedDTO, string activityType, int userId);
        Task<bool> ParcelLoadAtAtmStatusAsync(int crewCommanderId, int taskId, string status, AtmParcelLoadedAtATMDTO cassetteDTO, string activityType, int userId);
        Task<IEnumerable<ParcelNo>> GetParcelLoadedAtBankAsync(int taskId, int authenticatedUserId);
        Task<AtmParcelCountDTO> GetParclesCountsByTaskId(int taskId);

        Task<bool> ParcelUnLoadAtAtmStatusAsync(int crewCommanderId, int taskId, string status, AtmParcelLoadedAtATMDTO parcelDTO, string activityType, int userId);

        Task<bool> crewTaskFailedAsync(int crewCommanderId, int taskId, string status, AtmTaskFailedDTO failedDTO, string activityType, int userId);

        Task<bool> ParcelUnLoadAtBankStatusAsync(int crewCommanderId, int taskId, string status, ParcelUnLoadedAtBankDTO cassetteDTO, string activityType, int userId);

        Task<IEnumerable<ParcelNo>> GetParcelUnLoadedAtAtmAsync(int taskId, int authenticatedUserId);

        public Task<IEnumerable<ParcelReceiptNos>> GetParcelAsync(int taskId, int authenticatedUserId, int userIdFromDb);

        public Task<IEnumerable<ParcelNo>> GetParcelUnloadedAsync(int taskId, int authenticatedUserId, int userIdFromDb);

        public Task<bool> AtmOfflineData(int crewCommanderId, int taskId,  AtmCrewTaskOffline atmCrewTaskOffline, int userId);
    }
}
