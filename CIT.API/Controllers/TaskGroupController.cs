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
    public class TaskGroupController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly APIResponse _response;

        public TaskGroupController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _response = new();
        }

        [HttpGet]
        [Route("GetAllTaskGroup")]
        public IActionResult GetAllTaskGroup()
        {
            try
            {
                var orderAssignService = _serviceProvider.GetRequiredService<ITaskGroupRepository>();
                return Ok(orderAssignService.GetAllTaskGroup());
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpPost]
        [Route("AddTaskGroup")]
        public IActionResult AddTaskGroups(List<TaskGroupingRequestDTO> taskGroupingRequestDTOs)
        {
            try
            {
                var orderAssignService = _serviceProvider.GetRequiredService<ITaskGroupRepository>();
                var tasks = orderAssignService.AddTaskGroups(taskGroupingRequestDTOs);

                if (tasks == null || !tasks.Any())
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Please check the provided information and try again.");
                    return BadRequest(_response);
                }

                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccess = true;
                _response.Result = null;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("An error occurred while processing your request.");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpDelete]
        [Route("DeleteTaskGroup")]
        public IActionResult DeleteTaskGroup(int id)
        {
            try
            {
                var orderAssignService = _serviceProvider.GetRequiredService<ITaskGroupRepository>();
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
