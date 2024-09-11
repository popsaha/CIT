using CIT.API.Models;
using CIT.API.Models.Dto.Customer;

namespace CIT.API.Repository.IRepository
{
    public interface ICustomerRepository
    {
        public Task<IEnumerable<Customer>> GetCustomers();
        Task<int> AddCustomer(CustomerCreateDTO customerDTO);
        Task<Customer> GetCustomer(int customerId);
        Task<Customer> UpdateCustomer(Customer customer);
        Task<int> DeleteCustomer(int customerId, int deletedBy);
        Task<Customer> GetCustomerByName(string customerName);
    }
}
