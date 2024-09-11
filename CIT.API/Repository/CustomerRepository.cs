using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto.Customer;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        public CustomerRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }

        public async Task<IEnumerable<Customer>> GetCustomers()
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "A");
                var customers = await con.QueryAsync<Customer>("spCustomer", parameters, commandType: CommandType.StoredProcedure);
                return customers.ToList();
            }
        }

        public async Task<Customer> GetCustomer(int customerid)
        {
            Customer customer = new Customer();
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "R");
                parameters.Add("CustomerID", customerid);
                customer = await connection.QuerySingleOrDefaultAsync<Customer>("spCustomer", parameters, commandType: CommandType.StoredProcedure);
            }
            return customer;
        }

        public async Task<int> AddCustomer(CustomerCreateDTO customerDTO)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();

                parameters.Add("Flag", "C");
                parameters.Add("CustomerName", customerDTO.CustomerName);
                parameters.Add("Address", customerDTO.Address);
                parameters.Add("ContactNumber", customerDTO.ContactNumber);
                parameters.Add("Email", customerDTO.Email);
                //parameters.Add("DataSource", customerDTO.DataSource);
                //parameters.Add("IsActive", customerDTO.IsActive);
                //parameters.Add("CreatedBy", customerDTO.CreatedBy);
                //parameters.Add("ModifiedBy", customerDTO.ModifiedBy);
                //parameters.Add("DeletedBy", customerDTO.DeletedBy);

                var customerId = await connection.ExecuteScalarAsync<int>("spCustomer", parameters, commandType: CommandType.StoredProcedure);

                return customerId;
            };


        }

        public async Task<int> DeleteCustomer(int customerid, int deletedBy)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "D");
                parameters.Add("DeletedBy", deletedBy);
                parameters.Add("CustomerID", customerid);
                Res = await connection.ExecuteScalarAsync<int>("spCustomer", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }

        public async Task<Customer> UpdateCustomer(Customer customer)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", "U");
                    parameters.Add("CustomerName", customer.CustomerName);
                    parameters.Add("Address", customer.Address);
                    parameters.Add("ContactNumber", customer.ContactNumber);
                    parameters.Add("Email", customer.Email);
                    //parameters.Add("ModifiedBy", customerDTO.ModifiedBy);

                    Res = await connection.ExecuteScalarAsync<int>("spCustomer", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return customer;
        }

        public async Task<Customer> GetCustomerByName(string customerName)
        {
            using (var connection = _db.CreateConnection())
            {
                // Create a parameterized query to prevent SQL injection
                var query = "SELECT * FROM [cit].[Customer] WHERE CustomerName = @CustomerName";

                // Execute the query and return the first matching customer, if found
                var customer = await connection.QueryFirstOrDefaultAsync<Customer>(
                    query,
                    new { CustomerName = customerName }
                );

                return customer;  // Returns the customer object or null if not found
            }
        }
    }
}
