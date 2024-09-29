using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Repository.IRepository;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace CIT.API.Repository
{
    public class TaskGroupListRepository : ITaskGroupListRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;

        public TaskGroupListRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }

   

        public async Task<IEnumerable<TaskGroupList>> GetAllTaskGroupList()
        {
            using (var con = _db.CreateConnection())
            {
                var taskList = await con.QueryAsync<TaskGroupList>("spTaskGroupList", commandType: CommandType.StoredProcedure);
                return taskList.ToList();
            }
        }
    }
}
