using CIT.API.Models.Dto.Vehicle;
using CIT.API.Models;
using CIT.API.Models.Dto.ChaseVehicle;

namespace CIT.API.Repository.IRepository
{
    public interface IChaseVehicleRepository
    {
        public Task<IEnumerable<ChaseVehicle>> GetAllVehicle();
        Task<int> AddVehicle(ChaseVehicleCreateDTO vehicleCreate, int userId);
        Task<ChaseVehicle> UpdateVehicle(ChaseVehicle vehicle);
        Task<int> DeleteVehicle(int vehicleId, int deletedBy);
        Task<ChaseVehicle> GetVehicleById(int vehicleId);
    }
}
