using CIT.API.Models;
using CIT.API.Models.Dto;

namespace CIT.API.Repository.IRepository
{
    public interface ICustomerRepository
    {
        public Task<IEnumerable<Customer>> GetCustomers();
        Task<int> AddCustomer(CustomerDTO customerDTO);
        Task<Customer> GetCustomer(int customerId);
        Task<int> UpdateCustomer(CustomerDTO customerDTO);
        Task<int> DeleteCustomer(int customerId, int deletedBy);
    }
}
