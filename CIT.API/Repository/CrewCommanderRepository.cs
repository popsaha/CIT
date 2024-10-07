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

        public async Task<IEnumerable<LocalUser>> GetAllCrewCommanderList()
        {
            using (var con = _db.CreateConnection())
            {
                var query = @"SELECT Id, UserName, Name, Role 
                            FROM LocalUser 
                            WHERE Role = 'crew'";
                var crewCommanderList = await con.QueryAsync<LocalUser>(query);

                return crewCommanderList.ToList();
            }
        }
    }
}
