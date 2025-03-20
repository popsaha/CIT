using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace CIT.API.Controllers
{
    public class ItemsController : ControllerBase
    {
        private readonly ICustomerRepository _customerRepo;
        public ItemsController(ICustomerRepository customerRepo)
        {
            _customerRepo = customerRepo;
        }
    }
}
