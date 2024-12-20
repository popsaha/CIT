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
        private readonly ILogger<CustomerRepository> _logger;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        public CustomerRepository(DapperContext db, IMapper mapper, IConfiguration configuration, ILogger<CustomerRepository> logger)
        {
            _db = db;
            _logger = logger;
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

        public async Task<int> AddCustomer(CustomerCreateDTO customerDTO, int userId)
        {
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();

                    parameters.Add("Flag", "C");
                    parameters.Add("CustomerName", customerDTO.CustomerName);
                    parameters.Add("Address", customerDTO.Address);
                    parameters.Add("ContactNumber", customerDTO.ContactNumber);
                    parameters.Add("Email", customerDTO.Email);
                    parameters.Add("CreatedBy", userId);
                    parameters.Add("CustomerCode", customerDTO.CustomerCode);
                    parameters.Add("ReferenceNo1", customerDTO.ReferenceNo1);
                    parameters.Add("Country", customerDTO.Country);
                    parameters.Add("ReferenceNo2", customerDTO.ReferenceNo2);
                    parameters.Add("PostalCode", customerDTO.PostalCode);
                    parameters.Add("TaxNumber", customerDTO.TaxNumber);
                    parameters.Add("City", customerDTO.City);

                    var customerId = await connection.ExecuteScalarAsync<int>("spCustomer", parameters, commandType: CommandType.StoredProcedure);

                    return customerId;
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding a customer. UserId: {UserId}, CustomerName: {CustomerName}", userId, customerDTO.CustomerName);

                throw;
            }
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

        public async Task<CustomerUpdateDTO> UpdateCustomer(CustomerUpdateDTO customer)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", "U");
                    parameters.Add("CustomerID", customer.CustomerId);
                    parameters.Add("CustomerName", customer.CustomerName);
                    parameters.Add("Address", customer.Address);
                    parameters.Add("ContactNumber", customer.ContactNumber);
                    parameters.Add("Email", customer.Email);
                    parameters.Add("CustomerCode", customer.CustomerCode);
                    parameters.Add("ReferenceNo1", customer.ReferenceNo1);
                    parameters.Add("Country", customer.Country);
                    parameters.Add("ReferenceNo2", customer.ReferenceNo2);
                    parameters.Add("PostalCode", customer.PostalCode);
                    parameters.Add("TaxNumber", customer.TaxNumber);
                    parameters.Add("City", customer.City);
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
