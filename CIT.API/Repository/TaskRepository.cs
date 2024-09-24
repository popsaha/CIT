using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto.Branch;
using CIT.API.Models.Dto.Task;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class TaskRepository : ITaskRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        public TaskRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }
        public async Task<int> CreateTask(TaskMaster taskmaster)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();

                    parameters.Add("flag", "4");
                    parameters.Add("OrderId", taskmaster.OrderId);
                    parameters.Add("OrderTypeID", taskmaster.OrderTypeID);
                    parameters.Add("PriorityId", taskmaster.PriorityId);
                    parameters.Add("OrderNumber", taskmaster.OrderId);
                    parameters.Add("PickUpTypeId", taskmaster.PickUpTypeId);
                    parameters.Add("CustomerId", taskmaster.CustomerId);
                    parameters.Add("BranchID", taskmaster.BranchID);
                    parameters.Add("CustomerRecipiantId", taskmaster.CustomerRecipiantId);
                    parameters.Add("CustomerRecipiantLocationId", taskmaster.CustomerRecipiantLocationId);
                    parameters.Add("RepeatId", taskmaster.RepeatId);
                    parameters.Add("OrderCreateDate", taskmaster.OrderCreateDate);
                    parameters.Add("RepeatDaysName", taskmaster.RepeatDaysName);
                    parameters.Add("EndOnDate", taskmaster.EndOnDate);
                    parameters.Add("isVault", taskmaster.isVault);
                    parameters.Add("VaultID", taskmaster.VaultID);
                    parameters.Add("isVaultFinal", taskmaster.isVaultFinal);

                    Res = await connection.ExecuteScalarAsync<int>("usp_Order", parameters, commandType: CommandType.StoredProcedure);
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
            return Res;
        }

        public async Task<IEnumerable<TaskBranch>> GetBranchById(int CustomerID)
        {
            TaskBranch taskbranchlist = new TaskBranch();

            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "7");
                parameters.Add("CustomerID", CustomerID);
                var branches = (List<TaskBranch>)await connection.QueryAsync<TaskBranch>("usp_Order", parameters, commandType: CommandType.StoredProcedure);
                return branches.ToList();
            }
        }

        public async Task<IEnumerable<VaultLovationMaster>> GetVaultLocation()
        {
            VaultLovationMaster VaultLovationMaster = new VaultLovationMaster();

            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "5");
                var vaultLovations = (List<VaultLovationMaster>)await connection.QueryAsync<VaultLovationMaster>("usp_Order", parameters, commandType: CommandType.StoredProcedure);
                return vaultLovations.ToList();
            }
        }
    }
}
