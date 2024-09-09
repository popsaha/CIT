﻿using CIT.API.Models.Dto;
using CIT.API.Models;
using CIT.API.Repository;
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

        [HttpGet("GetCustomersAPI")]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _customerRepo.GetCustomers();
            return Ok(customers);
        }

        [HttpPost("AddCustomerAPI")]
        public async Task<IActionResult> AddCustomer(CustomerDTO customerDTO)
        {
            int Res = 0;
            try
            {
                if (ModelState.IsValid)
                {

                    Res = await _customerRepo.AddCustomer(customerDTO);

                }
                return Ok(Res);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }

        }

        [HttpGet("GetCustomerAPI")]
        public async Task<IActionResult> GetCustomer([FromRoute(Name = "customerId")] int customerId)
        {
            Customer customer = new Customer();
            try
            {
                customer = await _customerRepo.GetCustomer(customerId);
                return Ok(customer);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpPut("UpdateCustomerAPI")]
        public async Task<IActionResult> UpdateCustomer(CustomerDTO customerDTO)
        {
            int Res = 0;
            try
            {
                Res = await _customerRepo.UpdateCustomer(customerDTO);
                return Ok(Res);
            }
            catch (Exception ex)
            {

                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpDelete("DeleteCustomerAPI")]
        public async Task<IActionResult> DeleteCustomer(int customerId, int deletedBy)
        {
            int Res = 0;
            try
            {
                Res = await _customerRepo.DeleteCustomer(customerId, deletedBy);
                return Ok("The user is Deleted");
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);

            }
        }
    }
}
