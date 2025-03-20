using CIT.API.Models;

namespace CIT.API.Repository.IRepository
{
    public interface IPickupTypesRepository
    {
        public Task<IEnumerable<PickupTypes>> GetAllPickupTypesAsync();
    }
}
