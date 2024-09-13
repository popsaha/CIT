using CIT.API.Models.Dto;
using CIT.API.Models;

namespace CIT.API.Repository.IRepository
{
    public interface IRegionRepository
    {
        public Task<IEnumerable<RegionMaster>> GetallRegion();
        Task<int> AddRegion(RegionDTO regionDTO);
        Task<RegionMaster> GetRegion(int RegionID);
        Task<int> UpdateRegion(RegionMaster regionDTO);
        Task<int> DeleteRegion(int RegionID, int deletedBy);
    }
}
