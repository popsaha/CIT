using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto.Order;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderRepository> _logger;
        public OrderRepository(DapperContext db, IMapper mapper, IConfiguration configuration, ILogger<OrderRepository> logger)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _logger = logger;
        }

        public async Task<int> CreateOrder(OrderDTO orderDTO)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "4");
                parameters.Add("OrderTypeId", orderDTO.OrderTypeId);
                parameters.Add("RepeatFrequency", orderDTO.Repeats);
                parameters.Add("CustomerId", 1);
                parameters.Add("UserRegionID", 1);
                parameters.Add("RouteID", 1);
                parameters.Add("IsVault", "1");
                parameters.Add("IsFullDayOccupancy", true);
                parameters.Add("Priority", 1);
                parameters.Add("IsVaultFinal", "1");


                //parameters.Add("UserRegionID", orderDTO.UserRegionID);
                //parameters.Add("RouteID", orderDTO.RouteID);
                //parameters.Add("IsVault", orderDTO.IsVault);
                //parameters.Add("IsFullDayOccupancy", orderDTO.FullDayOccupancy);
                //parameters.Add("Priority", orderDTO.PriorityId);
                //parameters.Add("IsVaultFinal", orderDTO.IsVaultFinal);
                //parameters.Add("StartDate", orderDTO.StartDate);
                //parameters.Add("StartTime", orderDTO.StartTime);
                //parameters.Add("EndTime", orderDTO.EndTime);
                //parameters.Add("EndTask", orderDTO.EndTask);
                //parameters.Add("RepeatIn", orderDTO.RepeatIn);
                //parameters.Add("CreatedBy", orderDTO.CreatedBy);
                int OrderId = await connection.ExecuteScalarAsync<int>("usp_Order", parameters, commandType: CommandType.StoredProcedure);

                int tskboj = AddTask(OrderId, orderDTO.taskmodellist);
            };

            //;

            return Res;
        }

        public int AddTask(int OrderId, List<Models.Dto.Order.TaskModel> taskmodellist)
        {
            int res = 0;
            foreach (var taskobj in taskmodellist)
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("flag", "5");
                    parameters.Add("OrderID", OrderId);
                    parameters.Add("askID", taskobj.TaskID);
                    parameters.Add("PickUpLocation", taskobj.PickupType);
                    parameters.Add("DeliveryLocation", taskobj.RequesterName);
                    parameters.Add("TaskSequence", taskobj.PickupLocation);
                    parameters.Add("IsRecurring", taskobj.VaultLocation);
                    parameters.Add("DataSource", taskobj.RecipientName);
                    parameters.Add("PickupType", taskobj.DeliveryLocation);
                    parameters.Add("CreatedBy", taskobj.PickupTime);
                    parameters.Add("DeliveryTime", taskobj.DeliveryTime);
                    parameters.Add("NoOfVehicles", taskobj.NoOfVehicles);
                    var taskmodel = connection.ExecuteScalar<int>("usp_Order", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            return res = 0;
        }

        public async Task<OrderResponse> GetOrderDetails(int ResourceId)
        {
            OrderResponse Orderresponse = new OrderResponse();
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "6");
                parameters.Add("ResponseId", ResourceId);
                Orderresponse = await connection.ExecuteScalarAsync<OrderResponse>("usp_Order", parameters, commandType: CommandType.StoredProcedure);
            }
            return Orderresponse;
        }

        public async Task<IEnumerable<OrderListDTO>> GetOrdersWithTaskListAsync(DateTime selectedDate, int regionId)
        {
            //var sql = @"cit.spOrderList";  // Stored procedure name

            using (var connection = _db.CreateConnection())
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SelectedDate", selectedDate, DbType.Date);
                parameters.Add("@regionId", regionId, DbType.Int32);

                // Execute the stored procedure
                var orders = await connection.QueryAsync<OrderListDTO>(
                    "cit.spOrderList", // Name of your stored procedure
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return orders;

                //Dictionary<int, OrderListDTO> orderDictionary = new();

                //var orders = await connection.QueryAsync<OrderListDTO, TaskListDTO, OrderListDTO>(
                //    sql,
                //    (order, task) =>
                //    {
                //        // Check if the order already exists in the dictionary
                //        if (!orderDictionary.TryGetValue(order.OrderID, out var currentOrder))
                //        {
                //            currentOrder = order;
                //            currentOrder.Tasks = new List<TaskListDTO>();
                //            orderDictionary.Add(order.OrderID, currentOrder);
                //        }

                //        // Add tasks to the current order
                //        currentOrder.Tasks.Add(task);
                //        return currentOrder;
                //    },
                //    parameters,
                //    splitOn: "TaskId",  // Field to split on
                //    commandType: CommandType.StoredProcedure
                //);

                //return orderDictionary.Values;
            }
        }


    }
}
