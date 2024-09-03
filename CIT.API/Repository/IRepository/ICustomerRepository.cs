using CIT.API.Models;

namespace CIT.API.Repository.IRepository
{
    public interface ICustomerRepository
    {
        public Task<IEnumerable<Customer>> GetCustomers();
    }
}
