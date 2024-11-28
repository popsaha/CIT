using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto.Customer;
using CIT.API.Models.Dto.Police;
using CIT.API.Repository.IRepository;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace CIT.API.Repository
{
    public class PoliceRepository : IPoliceRepository
    {
        private readonly DapperContext _db;
        private readonly ILogger<PoliceRepository> _logger;
        private readonly string _secretKey;
        private readonly IMapper _mapper;

        public PoliceRepository(DapperContext db, IMapper mapper, IConfiguration configuration, ILogger<PoliceRepository> logger)
        {
            _db = db;
            _logger = logger;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }
        public async Task<IEnumerable<PoliceMaster>> GetPolice()
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "A");
                var customers = await con.QueryAsync<PoliceMaster>("spPolice", parameters, commandType: CommandType.StoredProcedure);
                return customers.ToList();
            }
        }
        public async Task<int> AddPolice(PoliceCreateDTO policeDTO, int userId)
        {
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();

                    parameters.Add("Flag", "C");
                    parameters.Add("Name", policeDTO.Name);
                    parameters.Add("Address", policeDTO.Address);
                    parameters.Add("ContactNumber", policeDTO.ContactNumber);
                    parameters.Add("CreatedBy", userId);

                    var policeId = await connection.ExecuteScalarAsync<int>("spPolice", parameters, commandType: CommandType.StoredProcedure);

                    return policeId;
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding a Police. UserId: {UserId}, Name: {Name}", userId, policeDTO.Name);

                throw;
            }
        }

        public async Task<int> DeletePolice(int policeId, int deletedBy)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "D");
                parameters.Add("DeletedBy", deletedBy);
                parameters.Add("PoliceID", policeId);
                Res = await connection.ExecuteScalarAsync<int>("spPolice", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }

        public async Task<PoliceUpdateDTO> UpdatePolice(PoliceUpdateDTO police)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", "U");
                    parameters.Add("PoliceID", police.PoliceId);
                    parameters.Add("Name", police.Name);
                    parameters.Add("Address", police.Address);
                    parameters.Add("ContactNumber", police.ContactNumber);
                    //parameters.Add("ModifiedBy", police.ModifiedBy);

                    Res = await connection.ExecuteScalarAsync<int>("spPolice", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return police;
        }

        public async Task<PoliceMaster> GetPoliceById(int policeId)
        {
            PoliceMaster police = new PoliceMaster();
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "R");
                parameters.Add("PoliceID", policeId);
                police = await connection.QuerySingleOrDefaultAsync<PoliceMaster>("spPolice", parameters, commandType: CommandType.StoredProcedure);
            }
            return police;
        }
    }
}
