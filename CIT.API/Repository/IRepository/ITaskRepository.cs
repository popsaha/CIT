using CIT.API.Models;
using CIT.API.Models.Dto.OrderType;
using CIT.API.Models.Dto.Task;

namespace CIT.API.Repository.IRepository
{
    public interface ITaskRepository
    {      
      public Task<IEnumerable<TaskBranch>> GetBranchById(int CustomerID);

        public Task<IEnumerable<VaultLovationMaster>> GetVaultLocation();
        Task<int> CreateTask(TaskMaster taskmaster);
    }
}
