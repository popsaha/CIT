using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace CIT.API.Controllers
{
    [Route("api/Order")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        public readonly IOrderRepository _orderRepository;
        protected APIResponse _response;
        public OrderController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
            _response = new();
        }

        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDTO orderDTO)
        {
            int Res = 0;
            try
            {
                if (ModelState.IsValid)
                {
                    Res = await _orderRepository.CreateOrder(orderDTO);
                }
                return Ok(Res);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpPost("GetOrderDetails")]
        public async Task<IActionResult> GetOrderDetails(int ResourceId)
        {
            OrderResponse orderResponse = new OrderResponse();
            int Res = 0;
            try
            {
                if (ModelState.IsValid)
                {
                    orderResponse = await _orderRepository.GetOrderDetails(ResourceId);
                }
                return Ok(Res);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }
    }
}