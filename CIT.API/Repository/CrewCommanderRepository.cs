using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Repository.IRepository;
using Dapper;

namespace CIT.API.Repository
{
    public class CrewCommanderRepository : ICrewCommanderRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;

        public CrewCommanderRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }

        public async Task<IEnumerable<UserMaster>> GetAllCrewCommanderList()
        {
            using (var con = _db.CreateConnection())
            {
                var query = @"SELECT UM.UserID, UM.UserName
                              FROM UserMaster UM
                              INNER JOIN UserRoleMapping URM ON UM.UserID = URM.UserId
                              INNER JOIN RoleMaster RM ON RM.RoleID = URM.RoleId
                              WHERE RM.RoleID = 4";
                var crewCommanderList = await con.QueryAsync<UserMaster>(query);

                return crewCommanderList.ToList();
            }
        }
    }
}
