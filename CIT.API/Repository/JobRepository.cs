using CIT.API.Context;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class JobRepository : IJobRepository
    {
        private readonly DapperContext _db;

        public JobRepository(DapperContext db)
        {
            _db = db;
        }
        public async Task GenerateRecurringOrdersAsync()
        {
            using (var connection = _db.CreateConnection())
            {
                await connection.ExecuteAsync("spProcessRecurringOrders", commandType: CommandType.StoredProcedure);
            }
        }
    }
}
