using CIT.API.Models;
using CIT.API.Models.Dto;

namespace CIT.API.Repository.IRepository
{
    public interface IVehiclesAssignmentRepository
    {
        IEnumerable<VehicleAssignment> GetAllAssignOrder();

        VehicleAssignmentRequestDTO AddAssignOrder(VehicleAssignmentRequestDTO vehicleAssignRequestDTO);

        IEnumerable<TaskGrouping> GetAllTaskGroup();

        List<TaskGroupingRequestDTO> AddTaskGroups(List<TaskGroupingRequestDTO> taskGroupingRequestDTOs);


        bool DeleteTaskGroup(int id);
    }
}
