using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class TaskListRepository : ITaskListRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        public TaskListRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }

        public async Task<IEnumerable<TaskList>> GetAllTaskList(DateTime date)
        {
            using (var con = _db.CreateConnection())
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SelectedDate", date, DbType.Date);

                var taskList = await con.QueryAsync<TaskList>(
                    "spTaskList",
                     parameters,
                    commandType: CommandType.StoredProcedure
                    );
                return taskList.ToList();
            }
        }
    }
}
