using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace CIT.API.Controllers
{
    [Route("api/customers")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerRepository _customerRepo;
        public CustomersController(ICustomerRepository customerRepo)
        {
            _customerRepo = customerRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _customerRepo.GetCustomers();
            return Ok(customers);
        }
    }
}
