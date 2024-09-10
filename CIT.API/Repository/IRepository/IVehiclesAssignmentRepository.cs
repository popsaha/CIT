using CIT.API.Models;
using CIT.API.Models.Dto;

namespace CIT.API.Repository.IRepository
{
    public interface IVehiclesAssignmentRepository
    {
        IEnumerable<VehicleAssignment> GetAllAssignOrder();

        //CommonResponseModel AddAssignOrder(VehicleAssignment vehicleAssignRequestDTO);
        //IEnumerable<TaskGrouping> GetAllTaskGroup();
        //CommonResponseModel AddTaskGroup(TaskGroupingRequestDTO taskGroupRequestModel);
        //CommonResponseModel UpdateTaskGroup(TaskGrouping taskGroupModel);
        //CommonResponseModel DeleteTaskGroup(int id);
    }
}
