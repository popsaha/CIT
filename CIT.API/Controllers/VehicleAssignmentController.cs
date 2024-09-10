using Azure;
using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]
    public class VehicleAssignmentController : Controller
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
                var order = orderAssignService.AddAssignOrder(vehicleAssignRequestDTO);
                if (order == null) 
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("The order could not be processed. Please check the provided information and try again.");
                    return BadRequest(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = order;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpGet]
        [Route("GetAllTaskGroup")]
        public IActionResult GetAllTaskGroup()
        {
            try
            {
                var orderAssignService = _serviceProvider.GetRequiredService<IVehiclesAssignmentRepository>();
                return Ok(orderAssignService.GetAllTaskGroup());
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpPost]
        [Route("AddTaskGroup")]
        public IActionResult AddTaskGroup(TaskGroupingRequestDTO taskGroupingRequestDTO)
        {
            try
            {
                var orderAssignService = _serviceProvider.GetRequiredService<IVehiclesAssignmentRepository>();
                var task = orderAssignService.AddTaskGroup(taskGroupingRequestDTO);
                if (task == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("The task could not be processed. Please check the provided information and try again.");
                    return BadRequest(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = task;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpDelete]
        [Route("DeleteTaskGroup")]
        public IActionResult DeleteTaskGroup(int id)
        {
            try
            {
                var orderAssignService = _serviceProvider.GetRequiredService<IVehiclesAssignmentRepository>();
                var task = orderAssignService.DeleteTaskGroup(id);
                if (!task)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("");
                    return BadRequest(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = $"Task Group is deleted for id:{id}";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }
    }
}
