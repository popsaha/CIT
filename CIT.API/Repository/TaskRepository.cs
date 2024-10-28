using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Models.Dto.Branch;
using CIT.API.Models.Dto.Task;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;
using static System.Net.Mime.MediaTypeNames;

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
                    parameters.Add("flag", taskmaster.IsEditTask == 1 ? "10" : "4");
                    parameters.Add("OrderId", taskmaster.OrderId);
                    parameters.Add("OrderTypeID", taskmaster.OrderTypeID);
                    parameters.Add("PriorityId", taskmaster.PriorityId);
                    parameters.Add("OrderNumber", taskmaster.OrderNumber == "" ? 0 : Convert.ToInt64(taskmaster.OrderNumber));
                    parameters.Add("PickUpTypeId", taskmaster.PickUpTypeId);
                    parameters.Add("CustomerId", taskmaster.CustomerId == 0 ? null : taskmaster.CustomerId);
                    parameters.Add("BranchID", taskmaster.BranchID == 0 ? null : taskmaster.BranchID);
                    parameters.Add("CustomerRecipiantId", taskmaster.CustomerRecipiantId == 0 ? null : taskmaster.CustomerRecipiantId);
                    parameters.Add("CustomerRecipiantLocationId", taskmaster.CustomerRecipiantLocationId == 0 ? null : taskmaster.CustomerRecipiantLocationId);
                    parameters.Add("RepeatId", taskmaster.RepeatId);
                    parameters.Add("OrderCreateDate", ConvertOrderDate(taskmaster.OrderCreateDate));
                    parameters.Add("RepeatDaysName", taskmaster.RepeatDaysName);
                    parameters.Add("EndOnDate", taskmaster.EndOnDate);
                    parameters.Add("isVault", taskmaster.isVault);
                    parameters.Add("VaultID", taskmaster.VaultID);
                    parameters.Add("isVaultFinal", taskmaster.isVaultFinal);
                    parameters.Add("OrderRouteId", taskmaster.OrderRouteId);
                    parameters.Add("NewVehicleRequired", taskmaster.NewVehicleRequired);
                    parameters.Add("IsFullDayAssignment", taskmaster.fullDayCheck);
                    parameters.Add("TaskId", taskmaster.TaskId);
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
                parameters.Add("CustomerId", CustomerID);
                var branches = (List<TaskBranch>)await connection.QueryAsync<TaskBranch>("usp_Order", parameters, commandType: CommandType.StoredProcedure);
                return branches.ToList();
            }
        }

        public async Task<IEnumerable<OrderRoutes>> GetOrderRoutes()
        {
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "6");
                var orderroutes = (List<OrderRoutes>)await connection.QueryAsync<OrderRoutes>("usp_Order", parameters, commandType: CommandType.StoredProcedure);
                return orderroutes.ToList();
            }
        }

        public async Task<TaskMaster> GetOrderTaskData(string OrderNumber)
        {
            try
            {
                TaskMaster taskMaster = new TaskMaster();
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("flag", "8");
                    parameters.Add("OrderNumber", OrderNumber);
                    taskMaster = await connection.QueryFirstOrDefaultAsync<TaskMaster>("usp_Order", parameters, commandType: CommandType.StoredProcedure);
                    return taskMaster;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IEnumerable<VaultLocationMaster>> GetVaultLocation()
        {
            VaultLocationMaster VaultLovationMaster = new VaultLocationMaster();

            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "5");
                var vaultLovations = (List<VaultLocationMaster>)await connection.QueryAsync<VaultLocationMaster>("usp_Order", parameters, commandType: CommandType.StoredProcedure);
                return vaultLovations.ToList();
            }
        }

        public string ConvertOrderDate(string orderdate)
        {
            string Result = "";
            string[] GetDate = orderdate.Split("-");
            string day = GetDate[0];
            string month = GetDate[1];
            string year = GetDate[2];
            Result = year + "-" + month + "-" + day;
            return Result;
        }

        public async Task<TaskMaster> GetEditTask_Details(int TaskId)
        {
            try
            {
                TaskMaster taskMaster = new TaskMaster();
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("flag", "9");
                    parameters.Add("TaskId", TaskId);
                    taskMaster = await connection.QueryFirstOrDefaultAsync<TaskMaster>("usp_Order", parameters, commandType: CommandType.StoredProcedure);
                    return taskMaster;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
