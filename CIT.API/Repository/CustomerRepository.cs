using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Repository.IRepository;
using Dapper;

namespace CIT.API.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly DapperContext _db;
        public CustomerRepository(DapperContext db) => _db = db;

        public async Task<IEnumerable<Customer>> GetCustomers()
        {
            var query = "SELECT * FROM Customers";

            using (var con = _db.CreateConnection())
            {
                var customers = await con.QueryAsync<Customer>(query);

                return customers.ToList();
            }
        }
    }
}
