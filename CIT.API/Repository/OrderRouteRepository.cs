using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto.OrderRoute;
using CIT.API.Repository.IRepository;
using Dapper;
using Hangfire;
using Microsoft.AspNetCore.Routing;
using System.Data;
using System.Net;

namespace CIT.API.Repository
{
    public class OrderRouteRepository : IOrderRouteRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderRepository> _logger;
        public OrderRouteRepository(DapperContext db, IMapper mapper, IConfiguration configuration, ILogger<OrderRepository> logger)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _logger = logger;
        }

        public async Task<int> CreateOrderRoutesAsync(OrderRouteCreateDTO routeCreateDTO, int userId)
        {
            using (var connection = _db.CreateConnection()) 
            { 
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "C");
                parameters.Add("OrderRouteId", routeCreateDTO.OrderRouteId);
                parameters.Add("RouteName", routeCreateDTO.RouteName);
                parameters.Add("Description", routeCreateDTO.RouteDescription);
                parameters.Add("CreatedBy", userId);
                var orderRoutes = await connection.ExecuteScalarAsync<int>("spOrdersRoute", parameters, commandType: CommandType.StoredProcedure);
                return orderRoutes;
            }
        }

        public async Task<int> DeleteOrderAsync(int id, int deletedBy)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "D");
                parameters.Add("DeletedBy", deletedBy);
                parameters.Add("OrderRouteId", id);
                Res = await connection.ExecuteScalarAsync<int>("spOrdersRoute", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }

        public async Task<IEnumerable<OrderRouteDTO>> GetAllOrderRoutesAsync()
        {
            var query = @"SELECT OrderRouteId, RouteName,Description as RouteDescription  FROM OrderRoutes WHERE IsActive=1";

            using (var connection = _db.CreateConnection())
            {
                var orderRoutes = await connection.QueryAsync<OrderRouteDTO>(query);
                return orderRoutes;
            }
        }

        public async Task<OrderRouteDTO> GetSingleOrderRoutesAsync(int id)
        {
            OrderRouteDTO route = new OrderRouteDTO();
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "R");
                parameters.Add("OrderRouteID", id);
                route = await connection.QuerySingleOrDefaultAsync<OrderRouteDTO>("spOrdersRoute", parameters, commandType: CommandType.StoredProcedure);
            }
            return route;
        }

        public async Task<RouteUpdateDTO> RouteOrderUpdateAsync(RouteUpdateDTO routeUpdate)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", "U");
                    parameters.Add("OrderRouteID", routeUpdate.OrderRouteId);
                    parameters.Add("RouteName", routeUpdate.RouteName);
                    parameters.Add("Description", routeUpdate.RouteDescription);
                    parameters.Add("IsActive", routeUpdate.IsActive);
                    
                    Res = await connection.ExecuteScalarAsync<int>("spOrdersRoute", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return routeUpdate;
        }

        public async Task<APIResponse> UpdateOrderRouteAsync(OrderRouteUpdateDTO updateRouteDTO)
        {
            try
            {
                // Convert list of order IDs to a comma-separated string
                string orderIds = string.Join(",", updateRouteDTO.OrderIds);

                using (var connection = _db.CreateConnection())
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@OrderIds", orderIds);
                    parameters.Add("@RouteName", updateRouteDTO.RouteName);

                    // Call the stored procedure to update the routes                  

                    var result = await connection.QueryAsync<string>(
                        "spUpdateOrdersRoute",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return new APIResponse
                    {
                        StatusCode = HttpStatusCode.OK,
                        IsSuccess = true,
                        Result = result.FirstOrDefault()
                    };
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order route");

                return new APIResponse
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    IsSuccess = false,
                    ErrorMessages = new List<string> { "Error updating order route" }
                };
            }
        }
    }
}
