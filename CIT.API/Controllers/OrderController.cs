using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Models.Dto.Order;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/Order")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        public readonly IOrderRepository _orderRepository;
        protected APIResponse _response;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderRepository orderRepository, ILogger<OrderController> logger)
        {
            _orderRepository = orderRepository;
            _response = new();
            _logger = logger;
        }

        [HttpPost("CreateOrder")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        // POST: api/order/updateroute
        [HttpPost("updateRoute")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateOrderRoute([FromBody] OrderRouteUpdateDTO orderUpdateRouteDTO)
        {
            if (orderUpdateRouteDTO == null || !orderUpdateRouteDTO.OrderIds.Any())
            {
                return BadRequest(new APIResponse
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    IsSuccess = false,
                    ErrorMessages = new List<string> { "Invalid input data" }
                });
            }

            var response = await _orderRepository.UpdateOrderRouteAsync(orderUpdateRouteDTO);

            if (!response.IsSuccess)
            {
                return StatusCode((int)response.StatusCode, response);
            }

            return Ok(response);

        }
    }
}