using CIT.API.Models;
using CIT.API.Models.Dto;

namespace CIT.API.Repository.IRepository
{
    public interface IVehiclesAssignmentRepository
    {
        IEnumerable<VehicleAssignment> GetAllAssignOrder();

        VehicleAssignmentRequestDTO AddAssignOrder(VehicleAssignmentRequestDTO vehicleAssignRequestDTO);
        
        IEnumerable<TaskGrouping> GetAllTaskGroup();

        TaskGroupingRequestDTO AddTaskGroup(TaskGroupingRequestDTO taskGroupingRequestDTO);
        
        bool DeleteTaskGroup(int id);
    }
}
