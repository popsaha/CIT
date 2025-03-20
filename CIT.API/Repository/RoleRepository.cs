using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class RoleRepository : IRoleRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        public RoleRepository(DapperContext db, IConfiguration configuration, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }
        public async Task<IEnumerable<RoleMaster>> GetAllRole()
        {
            IEnumerable<RoleMaster> roleMasterlist;

            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "A");
                roleMasterlist = await connection.QueryAsync<RoleMaster>("spRoleMaster", parameters, commandType: CommandType.StoredProcedure);
            }
            return roleMasterlist;
        }
    }
}
