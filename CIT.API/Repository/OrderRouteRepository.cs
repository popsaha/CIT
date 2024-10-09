using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto.OrderRoute;
using CIT.API.Repository.IRepository;
using Dapper;
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

        public async Task<IEnumerable<OrderRouteDTO>> GetAllOrderRoutesAsync()
        {
            var query = @"SELECT OrderRouteId, RouteName FROM OrderRoutes";

            using (var connection = _db.CreateConnection())
            {
                var orderRoutes = await connection.QueryAsync<OrderRouteDTO>(query);
                return orderRoutes;
            }
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
