using CIT.API.Models;

namespace CIT.API.Repository.IRepository
{
    public interface IVehiclesAssignRepository
    {
        IEnumerable<VehicleAssignModel> GetGetAllAssignOrder();
        CommonResponseModel AddAssignOrder(VehicleAssignRequestModel vehicleAssignRequestDTO);
        IEnumerable<TaskGroupModel> GetAllTaskGroup();
        CommonResponseModel AddTaskGroup(TaskGroupRequestModel taskGroupRequestModel);
        CommonResponseModel UpdateTaskGroup(TaskGroupModel taskGroupModel);
        CommonResponseModel DeleteTaskGroup(int id);
    }
}
