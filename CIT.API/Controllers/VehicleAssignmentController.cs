using Azure;
using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Repository.IRepository;
using CIT.API.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]
    public class VehicleAssignmentController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly APIResponse _response;

        public VehicleAssignmentController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _response = new();
        }

        [HttpGet]
        [Route("GetAllAssignOrder")]
        public IActionResult GetAllAssignOrder()
        {
            try
            {
                var orderAssignService = _serviceProvider.GetRequiredService<IVehiclesAssignmentRepository>();
                return Ok(orderAssignService.GetAllAssignOrder());
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpPost]
        [Route("AddAssignOrder")]
        public IActionResult AddOrderDistribute(VehicleAssignmentRequestDTO vehicleAssignRequestDTO)
        {
            try
            {
                var orderAssignService = _serviceProvider.GetRequiredService<IVehiclesAssignmentRepository>();
                //Validate Request
                var validationErrors = orderAssignService.ValidateVehicleAssignmentRequest(vehicleAssignRequestDTO);
                if (validationErrors.Any())
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.AddRange(validationErrors);
                    return BadRequest(_response);
                }
                
                var order = orderAssignService.AddAssignOrder(vehicleAssignRequestDTO);
                if (order == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("The order could not be processed. Please check the provided information and try again.");
                    return BadRequest(_response);
                }
                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccess = true;
                _response.Result = null;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }
    }
}
