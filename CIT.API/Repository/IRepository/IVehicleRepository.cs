using CIT.API.Models;

namespace CIT.API.Repository.IRepository
{
    public interface IVehicleRepository
    {
        public Task<IEnumerable<Vehicle>> GetAllVehicle(); 
    }
}
