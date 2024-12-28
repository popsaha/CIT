using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto.ChaseVehicle;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class ChaseVehicleRepository : IChaseVehicleRepository
    {

        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;

        public ChaseVehicleRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }
        public async Task<int> AddVehicle(ChaseVehicleCreateDTO vehicleCreate, int userId)
        {
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();

                parameters.Add("Flag", "C");
                parameters.Add("RegistrationNo", vehicleCreate.RegistrationNo);
                parameters.Add("MaintenanceDate", vehicleCreate.MaintenanceDate);
                parameters.Add("Capacity", vehicleCreate.Capacity);
                parameters.Add("VehicleType", vehicleCreate.VehicleType);
                parameters.Add("CreatedBy", userId);

                var vehicleId = await connection.ExecuteScalarAsync<int>("spVehicle", parameters, commandType: CommandType.StoredProcedure);

                return vehicleId;
            };
        }

        public async Task<int> DeleteVehicle(int vehicleId, int deletedBy)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "D");
                parameters.Add("DeletedBy", deletedBy);
                parameters.Add("VehicleID", vehicleId);
                Res = await connection.ExecuteScalarAsync<int>("spVehicle", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }

        public async Task<IEnumerable<ChaseVehicle>> GetAllVehicle()
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "B");
                var vehicle = await con.QueryAsync<ChaseVehicle>("spVehicle", parameters, commandType: CommandType.StoredProcedure);
                return vehicle.ToList();
            }
        }

        public async Task<ChaseVehicle> GetVehicleById(int vehicleId)
        {
            ChaseVehicle vehicle = new ChaseVehicle();
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "R");
                parameters.Add("VehicleID", vehicleId);
                vehicle = await connection.QuerySingleOrDefaultAsync<ChaseVehicle>("spVehicle", parameters, commandType: CommandType.StoredProcedure);
            }
            return vehicle;
        }

        public async Task<ChaseVehicle> UpdateVehicle(ChaseVehicle vehicle)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", "U");
                    parameters.Add("VehicleID", vehicle.VehicleID);
                    parameters.Add("RegistrationNo", vehicle.RegistrationNo);
                    parameters.Add("MaintenanceDate", vehicle.MaintenanceDate);
                    parameters.Add("Capacity", vehicle.Capacity);
                    parameters.Add("VehicleType", vehicle.VehicleType);
                    parameters.Add("IsActive", vehicle.IsActive);
                    //parameters.Add("ModifiedBy", police.ModifiedBy);

                    Res = await connection.ExecuteScalarAsync<int>("spVehicle", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return vehicle;
        }
    }
}
