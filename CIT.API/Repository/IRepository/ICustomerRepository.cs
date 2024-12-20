using CIT.API.Models;
using CIT.API.Models.Dto.Customer;

namespace CIT.API.Repository.IRepository
{
    public interface ICustomerRepository
    {
        public Task<IEnumerable<Customer>> GetCustomers();
        Task<int> AddCustomer(CustomerCreateDTO customerDTO, int userId);
        Task<Customer> GetCustomer(int customerId);
        Task<CustomerUpdateDTO> UpdateCustomer(CustomerUpdateDTO customer);
        Task<int> DeleteCustomer(int customerId, int deletedBy);
        Task<Customer> GetCustomerByName(string customerName);
    }
}
