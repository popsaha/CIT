using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using Dapper;
using System.Data;
using CIT.API.Repository.IRepository;
using CIT.API.Models.Dto.Region;

namespace CIT.API.Repository
{
    public class RegionRepository: IRegionRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        public RegionRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }
        public async Task<int> AddRegion(RegionDTO regionDTO)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "AddRegion");
                parameters.Add("RegionName", regionDTO.RegionName);
                parameters.Add("DataSource", regionDTO.DataSource);
                parameters.Add("CreatedBy", regionDTO.CreatedBy);
                Res = await connection.ExecuteScalarAsync<int>("spRegoin", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }
        public async Task<IEnumerable<RegionMaster>> GetallRegion()
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "GetRegion");
                var regionMasters = await con.QueryAsync<RegionMaster>("spRegoin", parameters, commandType: CommandType.StoredProcedure);
                return regionMasters.ToList();
            }
        }
        public async Task<RegionMaster> GetRegion(int RegionID)
        {
            RegionMaster customer = new RegionMaster();
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "GetRegionById");
                parameters.Add("RegionID", RegionID);
                customer = await connection.QuerySingleOrDefaultAsync<RegionMaster>("spRegoin", parameters, commandType: CommandType.StoredProcedure);
            }
            return customer;
        }
        public async Task<int> DeleteRegion(int RegionID, int deletedBy)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("flag", "DeleteRegion");
                parameters.Add("CreatedBy", deletedBy);
                parameters.Add("RegionID", RegionID);
                Res = await connection.ExecuteScalarAsync<int>("spRegoin", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }
        public async Task<int> UpdateRegion(RegionMaster regionDTO)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", "UpdateRegion");
                    parameters.Add("RegionID", regionDTO.RegionID);
                    parameters.Add("RegionName", regionDTO.RegionName);
                    parameters.Add("DataSource", regionDTO.DataSource);
                    parameters.Add("CreatedBy", regionDTO.ModifiedBy);
                    Res = await connection.ExecuteScalarAsync<int>("spRegoin", parameters, commandType: CommandType.StoredProcedure);
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
