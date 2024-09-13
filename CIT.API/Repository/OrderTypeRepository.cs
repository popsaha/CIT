using AutoMapper;
using CIT.API.Context;
using CIT.API.Models.Dto;
using CIT.API.Models;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class OrderTypeRepository : IOrderTypeRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        public OrderTypeRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }
        public async Task<int> AddOrderType(OrderTypeDTO orderTypeDTO)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "AddOrderType");
                parameters.Add("TypeName", orderTypeDTO.TypeName);
                parameters.Add("DataSource", orderTypeDTO.DataSource);             
                parameters.Add("CreatedBy", orderTypeDTO.CreatedBy);              
                Res = await connection.ExecuteScalarAsync<int>("spOrderType", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }

        public async Task<IEnumerable<OrderTypeMaster>> GetAllOrderType()
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "GetOrderTypeData");
                var customers = await con.QueryAsync<OrderTypeMaster>("spOrderType", parameters, commandType: CommandType.StoredProcedure);
                return customers.ToList();
            }
        }
        public async Task<OrderTypeMaster> GetOrderType(int OrderTypeID)
        {
            OrderTypeMaster orderTypeMaster = new OrderTypeMaster();
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "GetOrderTypeById");
                parameters.Add("OrderTypeID", OrderTypeID);
                orderTypeMaster = await connection.QuerySingleOrDefaultAsync<OrderTypeMaster>("spOrderType", parameters, commandType: CommandType.StoredProcedure);
            }
            return orderTypeMaster;
        }
        public async Task<int> DeleteOrderType(int OrderTypeID, int deletedBy)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "DeleteOrderType");
                parameters.Add("CreatedBy", deletedBy);
                parameters.Add("OrderTypeID", OrderTypeID);
                Res = await connection.ExecuteScalarAsync<int>("spOrderType", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }
        public async Task<int> UpdateOrderType(OrderTypeDTO orderTypeDTO)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("flag", "UpdateOrderType");
                    parameters.Add("OrderTypeID", orderTypeDTO.OrderTypeID);
                    parameters.Add("TypeName", orderTypeDTO.TypeName);
                    parameters.Add("DataSource", orderTypeDTO.DataSource);
                    parameters.Add("CreatedBy", orderTypeDTO.CreatedBy);
                    Res = await connection.ExecuteScalarAsync<int>("spOrderType", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Res;
        }
    }
}
