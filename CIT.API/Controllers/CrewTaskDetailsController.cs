using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.CrewTaskDetails;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CIT.API.Controllers
{
    [Authorize] 
    [Route("api/[controller]")]
    [ApiController]
    public class CrewTaskDetailsController : ControllerBase
    {
        private readonly ICrewTaskDetailsRepository _crewTaskDetailsRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;

        public CrewTaskDetailsController(ICrewTaskDetailsRepository crewTaskDetailsRepository, IMapper mapper)
        {
            _crewTaskDetailsRepository = crewTaskDetailsRepository;
            _mapper = mapper;
            _response = new APIResponse();
        }

        
        [HttpGet("GetCrewTasks")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetCrewTasks()
        {
            try
            {
                
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                if (userIdClaim == null)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_response);
                }

                
                int crewCommanderId;
                if (!int.TryParse(userIdClaim.Value, out crewCommanderId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid user ID.");
                    return BadRequest(_response); // Handle parsing failure
                }

                // Use the crewCommanderId to fetch tasks from the repository
                var crewTasks = await _crewTaskDetailsRepository.GetCrewTasksByCommanderIdAsync(crewCommanderId);

                if (crewTasks == null || !crewTasks.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No tasks found for this crew commander.");
                    return NotFound(_response);
                }

                // Map the result to DTOs and return the API response
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<CrewTaskDetailsDTO>>(crewTasks);

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);

                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet("GetTaskDetails/{taskId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetTaskDetails(int taskId)
        {
            try
            {
                // Retrieve the claim for the crew commander ID from the token
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name); // or another claim type, based on your token

                // Check if the user is authenticated and has the correct claim
                if (userIdClaim == null)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_response);
                }

                // Parse the crewCommanderId from the claim
                if (!int.TryParse(userIdClaim.Value, out int crewCommanderId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid user ID.");
                    return BadRequest(_response);
                }

                // Fetch task details using the repository method
                var taskDetails = await _crewTaskDetailsRepository.GetTaskDetailsByTaskIdAsync(crewCommanderId, taskId);

                // Handle case where the task is not found or unauthorized access is attempted
                if (taskDetails == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task not found or unauthorized access.");
                    return NotFound(_response);
                }

                // Successfully found the task details
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = taskDetails;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                // Handle any exceptions during the process
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("{taskId}/start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> StartTask(int taskId)
        {
            try
            {
                // Validate taskId
                if (taskId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    return BadRequest(_response);
                }

                // Retrieve the claim for the logged-in crew commander ID from the token
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                if (userIdClaim == null)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_response);
                }

                // Parse the crewCommanderId from the claim
                if (!int.TryParse(userIdClaim.Value, out int crewCommanderId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid user ID.");
                    return BadRequest(_response);
                }

                // Retrieve the task details by taskId to ensure it belongs to the logged-in commander
                var task = await _crewTaskDetailsRepository.GetTaskDetailsByTaskIdAsync(crewCommanderId, taskId);


                if (task == null)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden; // Return 403 if the task doesn't belong to this commander
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }


                // Automatically set status to "InProcess"
                string status = "InProcess";

                // Call the repository method to update the task status using taskId and "InProcess" status
                bool updateResult = await _crewTaskDetailsRepository.UpdateTaskStatusAsync(crewCommanderId, taskId, status);

                if (!updateResult)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task not found or update failed.");
                    return NotFound(_response);
                }

                // Successfully updated the task status to "InProcess"
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new
                {
                    taskId = taskId,
                    status = status
                };

                return Ok(_response);
            }
            catch (Exception ex)
            {
                // Handle any exceptions during the process
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("{taskId}/Arrived")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> ArriveTask(int taskId)
        {
            try
            {
                // Validate taskId
                if (taskId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    return BadRequest(_response);
                }

                // Retrieve the claim for the crew commander ID from the token
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                if (userIdClaim == null)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_response);
                }

                // Parse the crewCommanderId from the claim
                if (!int.TryParse(userIdClaim.Value, out int crewCommanderId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid user ID.");
                    return BadRequest(_response);
                }

                // Use the repository method to get the task details
                var task = await _crewTaskDetailsRepository.GetTaskDetailsByTaskIdAsync(crewCommanderId, taskId);

                if (task == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add(" You are not authorized to update this task.");
                    return NotFound(_response);
                }

                // Check if the logged-in crew commander is the owner of the task
                if (task.CrewCommanderId != crewCommanderId)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not authorized to update this task.");
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                // Set the status to "Arrived"
                string status = "Arrived";

                // Call the repository method to update the task status
                bool updateResult = await _crewTaskDetailsRepository.UpdateTaskStatusAsync(crewCommanderId, taskId, status);

                if (!updateResult)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task update failed.");
                    return NotFound(_response);
                }

                // Successfully updated the task status to "Arrived"
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new
                {
                    taskId = taskId,
                    status = status
                };

                return Ok(_response);
            }
            catch (Exception ex)
            {
                // Handle any exceptions during the process
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }



        //[HttpPut("Start")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult<APIResponse>> StartTask([FromBody] CrewTaskStatusUpdateDTO taskDetails)
        //{
        //    try
        //    {
        //        // Validate the input
        //        if (taskDetails == null || taskDetails.TaskId <= 0)
        //        {
        //            _response.StatusCode = HttpStatusCode.BadRequest;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add("Invalid task details provided.");
        //            return BadRequest(_response);
        //        }

        //        // Retrieve the claim for the crew commander ID from the token
        //        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

        //        if (userIdClaim == null)
        //        {
        //            _response.StatusCode = HttpStatusCode.Unauthorized;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add("User is not authorized.");
        //            return Unauthorized(_response);
        //        }

        //        // Parse the crewCommanderId from the claim
        //        if (!int.TryParse(userIdClaim.Value, out int crewCommanderId))
        //        {
        //            _response.StatusCode = HttpStatusCode.BadRequest;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add("Invalid user ID.");
        //            return BadRequest(_response);
        //        }

        //        // Use the repository method to get the task details
        //        var task = await _crewTaskDetailsRepository.GetTaskDetailsByTaskIdAsync(crewCommanderId, taskDetails.TaskId);

        //        if (task == null)
        //        {
        //            _response.StatusCode = HttpStatusCode.NotFound;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add("Task not found or you are not authorized to update this task.");
        //            return NotFound(_response);
        //        }

        //        // Check if the logged-in crew commander is the owner of the task
        //        if (task.CrewCommanderId != crewCommanderId)
        //        {
        //            _response.StatusCode = HttpStatusCode.Forbidden;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add("You are not authorized to update this task.");
        //            return StatusCode((int)HttpStatusCode.Forbidden, _response);
        //        }

        //        // Set the status to "InProcess" before updating (if needed)
        //        string status = "InProcess";

        //        // Call the repository method to update the task status
        //        bool updateResult = await _crewTaskDetailsRepository.UpdateTaskStatusAsync(crewCommanderId, taskDetails.TaskId, status);

        //        if (!updateResult)
        //        {
        //            _response.StatusCode = HttpStatusCode.NotFound;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add("Task not found or update failed.");
        //            return NotFound(_response);
        //        }

        //        // Successfully updated the task status
        //        _response.StatusCode = HttpStatusCode.OK;
        //        _response.IsSuccess = true;
        //        _response.Result = new
        //        {
        //            taskId = taskDetails.TaskId,
        //            status =status
        //        };

        //        return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle any exceptions during the process
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages.Add(ex.Message);
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}



        //[HttpPut("Arrived")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult<APIResponse>> ArriveTask([FromBody] CrewTaskStatusUpdateDTO taskDetails)
        //{
        //    try
        //    {
        //        // Validate the input
        //        if (taskDetails == null || taskDetails.TaskId <= 0)
        //        {
        //            _response.StatusCode = HttpStatusCode.BadRequest;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add("Invalid task details provided.");
        //            return BadRequest(_response);
        //        }

        //        // Retrieve the claim for the crew commander ID from the token
        //        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

        //        if (userIdClaim == null)
        //        {
        //            _response.StatusCode = HttpStatusCode.Unauthorized;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add("User is not authorized.");
        //            return Unauthorized(_response);
        //        }

        //        // Parse the crewCommanderId from the claim
        //        if (!int.TryParse(userIdClaim.Value, out int crewCommanderId))
        //        {
        //            _response.StatusCode = HttpStatusCode.BadRequest;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add("Invalid user ID.");
        //            return BadRequest(_response);
        //        }

        //        // Use the repository method to get the task details
        //        var task = await _crewTaskDetailsRepository.GetTaskDetailsByTaskIdAsync(crewCommanderId, taskDetails.TaskId);

        //        if (task == null)
        //        {
        //            _response.StatusCode = HttpStatusCode.NotFound;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add("Task not found or you are not authorized to update this task.");
        //            return NotFound(_response);
        //        }

        //        // Check if the logged-in crew commander is the owner of the task
        //        if (task.CrewCommanderId != crewCommanderId)
        //        {
        //            _response.StatusCode = HttpStatusCode.Forbidden;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add("You are not authorized to update this task.");
        //            return StatusCode((int)HttpStatusCode.Forbidden, _response);
        //        }

        //        // Set the status to "Arrived"
        //         string status = "Arrived";

        //        // Call the repository method to update the task status
        //        bool updateResult = await _crewTaskDetailsRepository.UpdateTaskStatusAsync(crewCommanderId, taskDetails.TaskId, status);

        //        if (!updateResult)
        //        {
        //            _response.StatusCode = HttpStatusCode.NotFound;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages.Add("Task update failed.");
        //            return NotFound(_response);
        //        }

        //        // Successfully updated the task status to "Arrived"
        //        _response.StatusCode = HttpStatusCode.OK;
        //        _response.IsSuccess = true;
        //        _response.Result = new
        //        {
        //            taskId = taskDetails.TaskId,
        //            status = status
        //        };

        //        return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle any exceptions during the process
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages.Add(ex.Message);
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}


    }
}
