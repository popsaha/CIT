using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class PickupTypesRepository : IPickupTypesRepository
    {
        private readonly DapperContext _db;
        private readonly ILogger<PickupTypesRepository> _logger;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        public PickupTypesRepository(IMapper mapper, DapperContext db , IConfiguration configuration )
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");

        }
        public async Task<IEnumerable<PickupTypes>> GetAllPickupTypesAsync()
        {
           using(var con = _db.CreateConnection())
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("Flag", "A");
                var pickuptypes = await con.QueryAsync<PickupTypes>("spPickupTypes", dynamicParameters, commandType: CommandType.StoredProcedure);
                return pickuptypes.ToList();
            }
        }
    }
}
