using CIT.API.Models;
using CIT.API.Models.Dto.Police;
using CIT.API.Models.Dto.Vehicle;

namespace CIT.API.Repository.IRepository
{
    public interface IVehicleRepository
    {
        public Task<IEnumerable<Vehicle>> GetAllVehicle();
        Task<int> AddVehicle( VehicleCreateDTO vehicleCreate, int userId);
        Task<Vehicle> UpdateVehicle(Vehicle vehicle);
        Task<int> DeleteVehicle(int vehicleId, int deletedBy);
        Task<Vehicle> GetVehicleById(int vehicleId);
    }
}
