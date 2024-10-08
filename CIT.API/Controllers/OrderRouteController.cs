using CIT.API.Models;
using CIT.API.Models.Dto.OrderRoute;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/OrderRoute")]
    [ApiController]
    public class OrderRouteController : ControllerBase
    {
        public readonly IOrderRouteRepository _orderRoutesRepository;
        protected APIResponse _response;
        private readonly ILogger<OrderRouteController> _logger;

        public OrderRouteController(IOrderRouteRepository orderRouteRepo, ILogger<OrderRouteController> logger)
        {
            _orderRoutesRepository = orderRouteRepo;
            _response = new();
            _logger = logger;
        }

        [HttpGet("GetAllOrderRoutes")]
        public async Task<IActionResult> GetAllOrderRoutes()
        {
            try
            {
                // Fetching all order routes
                var orderRoutes = await _orderRoutesRepository.GetAllOrderRoutesAsync();

                if (orderRoutes == null || !orderRoutes.Any())
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No order routes found.");
                    _response.StatusCode = HttpStatusCode.NotFound;
                }
                else
                {
                    _response.Result = orderRoutes;
                    _response.StatusCode = HttpStatusCode.OK;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.StatusCode = HttpStatusCode.InternalServerError;
            }

            return StatusCode((int)_response.StatusCode, _response);
        }

        /// <summary>
        /// If user wants to update/add the route of any order.
        /// </summary>
        /// <param name="orderUpdateRouteDTO"></param>
        /// <returns></returns>
        [HttpPost("updateRoute")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateOrderRoute([FromBody] OrderRouteUpdateDTO orderUpdateRouteDTO)
        {
            if (orderUpdateRouteDTO == null || !orderUpdateRouteDTO.OrderIds.Any())
            {
                _logger.LogWarning("Invalid input data: OrderRouteUpdateDTO is null or contains no OrderIds");
                return BadRequest(new APIResponse
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    IsSuccess = false,
                    ErrorMessages = new List<string> { "Invalid input data" }
                });
            }

            // Trim the RouteName before calling the method
            orderUpdateRouteDTO.RouteName = orderUpdateRouteDTO.RouteName?.Trim();

            var response = await _orderRoutesRepository.UpdateOrderRouteAsync(orderUpdateRouteDTO);

            if (!response.IsSuccess)
            {
                _logger.LogError("Failed to update order route. StatusCode: {StatusCode}, Errors: {Errors}",
                        response.StatusCode, string.Join(", ", response.ErrorMessages));

                return StatusCode((int)response.StatusCode, response);
            }

            return Ok(response);

        }

    }
}
