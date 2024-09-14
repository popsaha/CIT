using CIT.API.Models;
using CIT.API.Models.Dto.Region;

namespace CIT.API.Repository.IRepository
{
    public interface IRegionRepository
    {
        public Task<IEnumerable<RegionMaster>> GetallRegion();
        Task<int> AddRegion(RegionCreateDTO regionDTO);
        Task<RegionMaster> GetRegion(int RegionID);
        Task<int> UpdateRegion(RegionMaster regionDTO);
        Task<int> DeleteRegion(int RegionID, int deletedBy);
    }
}
