using CIT.API.Models;
using CIT.API.Models.Dto;

namespace CIT.API.Repository.IRepository
{
    public interface IVehiclesAssignmentRepository
    {
        IEnumerable<VehicleAssignment> GetAllAssignOrder();

        List<VehicleAssignmentRequestDTO> AddAssignOrder(List<VehicleAssignmentRequestDTO> vehicleAssignRequestDTO);
    }
}
