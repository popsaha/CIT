using CIT.API.Models;
using CIT.API.Models.Dto;

namespace CIT.API.Repository.IRepository
{
    public interface IVehiclesAssignmentRepository
    {
        IEnumerable<VehicleAssignment> GetAllAssignOrder();
        List<VehicleAssignmentRequestDTO> AddAssignOrder(VehicleAssignmentRequestDTO vehicleAssignRequestDTO);
        List<string> ValidateVehicleAssignmentRequest(VehicleAssignmentRequestDTO vehicleAssignRequestDTO);
    }
}
