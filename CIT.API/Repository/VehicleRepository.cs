using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;

        public VehicleRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }
        public async Task<IEnumerable<Vehicle>> GetAllVehicle()
        {
            using (var con = _db.CreateConnection())
            {
                var taskList = await con.QueryAsync<Vehicle>("spVehicle", commandType: CommandType.StoredProcedure);
                return taskList.ToList();
            }
        }
    }
}
