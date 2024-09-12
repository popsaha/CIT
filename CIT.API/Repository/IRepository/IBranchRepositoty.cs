using CIT.API.Models;
using CIT.API.Models.Dto;

namespace CIT.API.Repository.IRepository
{
    public interface IBranchRepositoty
    {
        Task<IEnumerable<BranchMaster>> GetAllBranch();
        Task<int> AddBranch(BranchDTO branchdto);
        Task<BranchMaster> GetBranch(int branchId);
        Task<int> UpdateBranch(BranchMaster branchDTO);
        Task<int> DeleteBranch(int branchId, int deletedBy);
    }
}
