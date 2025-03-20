using AutoMapper;
using Azure.Core;
using CIT.API.Models;
using CIT.API.Models.Dto.CrewTaskDetails;
using CIT.API.Repository;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
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
        private readonly ILogger<CrewTaskDetailsController> _logger;

        public CrewTaskDetailsController(ICrewTaskDetailsRepository crewTaskDetailsRepository, IMapper mapper, ILogger<CrewTaskDetailsController> logger)
        {
            _crewTaskDetailsRepository = crewTaskDetailsRepository;
            _mapper = mapper;
            _response = new APIResponse();
            _logger = logger;
        }


        [HttpGet("GetCrewTasks")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetCrewTasks( DateTime? orderDate = null)
        {
            _logger.LogInformation("fetching all crew task with orderDate: {OrderDate}", orderDate);
            try
            {

                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogDebug("Retrieved userId: {UserId}", userId);

                //if (!userId)
                //{
                //    _response.StatusCode = HttpStatusCode.NotFound;
                //    _response.IsSuccess = false;
                //    _response.ErrorMessages.Add("User ID not found for the provided UUID.");
                //    return NotFound(_response);
                //}


                // Retrieve the authenticated user's ID from the claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

                if (userIdClaim == null)
                {
                    _logger.LogWarning("Unauthorized request: User claim is missing");
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    _response.Result = new object[0];
                    return Unauthorized(_response);
                }

                // Parse user ID from claim
                int authenticatedUserId;
                if (!int.TryParse(userIdClaim, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _logger.LogWarning("Unauthorized access attempt by userId: {UserId}", authenticatedUserId);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    _response.Result = new object[0];
                    return Unauthorized(_response);
                }

                // Retrieve assigned tasks from repository based on user ID
                var crewTasks = await _crewTaskDetailsRepository.GetCrewTasksByCommanderIdAsync(authenticatedUserId, userId, orderDate);

                // Ensure tasks are sorted: Move 'Completed' tasks to the end
                // Sort by PickupTime (earliest first) and move 'Completed' tasks to the end
                crewTasks = crewTasks.OrderBy(t => t.Status == "Completed")
                                     .ThenBy(t => t.PickupTime)
                                     .ToList();


                if (crewTasks == null || !crewTasks.Any())
                {
                    _logger.LogInformation("No tasks found for userId: {UserId}", authenticatedUserId);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No tasks found for this user.");
                    _response.Result = new object[0];
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
                _logger.LogError(ex, "Error retrieving crew tasks");
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.Result = new object[0];
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
            _logger.LogInformation("Get Crew Task Details with taskId: {TaskId}", taskId);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogDebug("Retrieved userId: {UserId} from UUID", userId);

                // Retrieve the claim for the crew commander ID from the token
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name); // or another claim type, based on your token


                int authenticatedUserId;
                if (!int.TryParse(userIdClaim.Value, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _logger.LogWarning("Unauthorized access attempt: AuthenticatedUserId {AuthenticatedUserId} does not match userId {UserId}", authenticatedUserId, userId);

                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    _response.Result = new object[0];
                    return Unauthorized(_response);
                }
                // Check if the user is authenticated and has the correct claim
                if (userIdClaim == null)
                {
                    _logger.LogWarning("Unauthorized access attempt: User claim not found.");
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    _response.Result = new object[0];
                    return Unauthorized(_response);
                }

                // Parse the crewCommanderId from the claim
                if (!int.TryParse(userIdClaim.Value, out int crewCommanderId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid user ID.");
                    _response.Result = new object[0];
                    return BadRequest(_response);
                }

                // Fetch task details using the repository method
                var taskDetails = await _crewTaskDetailsRepository.GetTaskDetailsByTaskIdAsync(crewCommanderId, taskId, userId);

                // Handle case where the task is not found or unauthorized access is attempted
                if (taskDetails == null)
                {
                    _logger.LogWarning("Task not found or unauthorized access. TaskId: {TaskId}, UserId: {UserId}", taskId, userId);

                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task not found or unauthorized access.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return NotFound(_response);
                }

                _logger.LogInformation("Task details retrieved successfully for TaskId: {TaskId}", taskId);
                // Successfully found the task details
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = taskDetails;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching task details for TaskId: {TaskId}", taskId);

                // Handle any exceptions during the process
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.Result = new object[0];
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("{taskId}/Start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> StartTask(int taskId, [FromBody] CrewTaskStatusUpdateDTO updateDTO)
        {
            _logger.LogInformation("Start Task  api called with taskId: {TaskId}, FieldData : {FieldData} ", taskId, updateDTO);

            try
            {   

                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogDebug("Retrieved userId: {UserId} from UUID", userId);
                if (taskId <= 0)
                {
                    _logger.LogWarning("Invalid taskId: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                int authenticatedUserId;
                if (!int.TryParse(userIdClaim.Value, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _logger.LogWarning("Unauthorized access attempt. AuthenticatedUserId: {AuthenticatedUserId}, Expected UserId: {UserId}", authenticatedUserId, userId);

                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }
                // Check if the user is authenticated and has the correct claim
                if (userIdClaim == null)
                {
                    _logger.LogWarning("User claim not found, unauthorized access.");
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_response);
                }

                if (updateDTO.Location.Long == "" || updateDTO.Location.Lat == "")
                {
                    _logger.LogWarning("Invalid location data received: Lat={Lat}, Long={Long}", updateDTO.Location.Lat, updateDTO.Location.Long);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (updateDTO.Location.Long == "string" || updateDTO.Location.Lat == "string")
                {
                    _logger.LogWarning("Invalid location data received: Lat={Lat}, Long={Long}", updateDTO.Location.Lat, updateDTO.Location.Long);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);              
                }

                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);               
                if (currentScreenId == null)
                {
                    _logger.LogWarning("Task screen ID could not be retrieved for TaskId: {TaskId}", taskId);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                   
                }


                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "1")
                {
                    _logger.LogWarning("TaskId: {TaskId} is already marked as completed.", taskId);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);                 
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "-1")
                {
                    _logger.LogWarning("TaskId: {TaskId} is already marked as failed.", taskId);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task has already been marked as failed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);                 
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (updateDTO.NextScreenId != expectedNextScreenId)
                {
                    _logger.LogWarning("Invalid screen transition for TaskId: {TaskId}. ExpectedNextScreenId: {ExpectedNextScreenId}, ReceivedNextScreenId: {ReceivedNextScreenId}",
                     taskId, expectedNextScreenId, updateDTO.NextScreenId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid screen transition or The task has already passed this stage.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);

                }


                string status = "Started";
                string activityType = "Start";
                bool updateResult = await _crewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

                if (!updateResult)
                {
                    _logger.LogWarning("UserId: {UserId} is not allowed to update TaskId: {TaskId}", authenticatedUserId, taskId);

                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                _logger.LogInformation("TaskId: {TaskId} successfully started by UserId: {UserId}", taskId, authenticatedUserId);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new
                {
                    status = status,
                    time = updateDTO.Time.ToString("MM/dd/yyyy HH:mm:ss")
                };
                return Ok(_response);
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                _logger.LogError(ex, "SQL Exception occurred while processing TaskId: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while starting TaskId: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }



        [HttpPost("{taskId}/Arrived")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> ArriveTask(int taskId, [FromBody] CrewTaskStatusUpdateDTO updateDTO)
        {
            _logger.LogInformation("ArriveTask endpoint hit with taskId: {TaskId}, FieldData : {FieldData} ", taskId, updateDTO);
            try
            {

                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogDebug("Retrieved userId: {UserId} for UUID", userId);
                if (taskId <= 0)
                {
                    _logger.LogWarning("Invalid task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                int authenticatedUserId;
                if (!int.TryParse(userIdClaim.Value, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _logger.LogWarning("Unauthorized access attempt. AuthenticatedUserId: {AuthenticatedUserId}, ExpectedUserId: {ExpectedUserId}", authenticatedUserId, userId);

                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }
                // Check if the user is authenticated and has the correct claim
                if (userIdClaim == null)
                {
                    _logger.LogWarning("Unauthorized access attempt. AuthenticatedUserId: {AuthenticatedUserId}, ExpectedUserId: {ExpectedUserId}", authenticatedUserId, userId);

                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_response);
                }

                if (updateDTO.Location.Long == "" || updateDTO.Location.Lat == "")
                {
                    _logger.LogWarning("Invalid location data received: Lat={Lat}, Long={Long}", updateDTO.Location.Lat, updateDTO.Location.Long);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (updateDTO.Location.Long == "string" || updateDTO.Location.Lat == "string")
                {
                    _logger.LogWarning("Invalid location data provided: {Location}", updateDTO.Location);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }
             

                //// Retrieve the current ScreenId for validation
                //var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                //string expectedNextScreenId = "CIT-3"; // Define the expected ScreenId based on your workflow

                //if (currentScreenId != null && currentScreenId != expectedNextScreenId)
                //{
                //    return BadRequest(new { message = "Invalid screen transition. The task has already passed this stage." });
                //}


                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    _logger.LogWarning("Task screen ID could not be retrieved for TaskId: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "1")
                {
                    _logger.LogWarning("Task {TaskId} is already completed and cannot be modified.", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "-1")
                {
                    _logger.LogWarning("Task {TaskId} has already been marked as failed.", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task has already been marked as failed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (updateDTO.NextScreenId != expectedNextScreenId)
                {
                    _logger.LogWarning("Invalid screen transition for TaskId: {TaskId}. Expected: {ExpectedNextScreenId}, Provided: {ProvidedScreenId}",
                     taskId, expectedNextScreenId, updateDTO.NextScreenId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid screen transition or The task has already passed this stage.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                string status = "Arrived";
                string activityType = "Arrived";
                bool updateResult = await _crewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

                if (!updateResult)
                {
                    _logger.LogWarning("User {UserId} is not allowed to update task {TaskId}.", authenticatedUserId, taskId);
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                _logger.LogInformation("Task {TaskId} successfully marked as {Status} by User {UserId}.", taskId, status, authenticatedUserId);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new
                {
                    status = status,
                    time = updateDTO.Time.ToString("MM/dd/yyyy HH:mm:ss")
                };
                return Ok(_response);
            }
            catch (SqlException ex) when (ex.Number == 50000) // Check for the custom SQL error number
            {
                _logger.LogError(ex, "SQL error occurred while processing ArriveTask for TaskId: {TaskId}", taskId);

                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Result = new object[0]; // Set Result to an empty array.
                _response.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                return BadRequest(_response);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing ArriveTask for TaskId: {TaskId}", taskId);

                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.Result = new object[0]; // Set Result to an empty array.
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost("{taskId}/Fail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> FailedTask(int taskId, [FromBody] CrewTaskFailedStatusDTO failedDTO)
        {
            _logger.LogInformation("FailedTask endpoint hit with taskId: {TaskId}, FieldData : {FieldData} ", taskId, failedDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogDebug("Retrieved userId: {UserId} for UUID", userId);

                if (taskId <= 0)
                {
                    _logger.LogWarning("Invalid task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                int authenticatedUserId;
                if (!int.TryParse(userIdClaim.Value, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _logger.LogWarning("Unauthorized access attempt. AuthenticatedUserId: {AuthenticatedUserId}, ExpectedUserId: {ExpectedUserId}", authenticatedUserId, userId);

                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }
                // Check if the user is authenticated and has the correct claim
                if (userIdClaim == null)
                {
                    _logger.LogWarning("Unauthorized access attempt. AuthenticatedUserId: {AuthenticatedUserId}, ExpectedUserId: {ExpectedUserId}", authenticatedUserId, userId);

                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_response);
                }

                if (failedDTO.Location.Long == "" || failedDTO.Location.Lat == "")
                {
                    _logger.LogWarning("Invalid location data received: Lat={Lat}, Long={Long}", failedDTO.Location.Lat, failedDTO.Location.Long);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (failedDTO.Location.Long == "string" || failedDTO.Location.Lat == "string" )
                {
                    _logger.LogWarning("Invalid location data provided: {Location}", failedDTO.Location);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }
                else if (failedDTO.FailureReason == "string")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("FailureReason is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);

                }
                else if (failedDTO.NextScreenId == "string")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("ScreenId is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }
              

                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);


                // Prevent further modification if ScreenId is already "CIT-7"
                if (currentScreenId == null)
                {
                    _logger.LogWarning("Task screen ID could not be retrieved for TaskId: {TaskId}", taskId);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "1")
                {
                    _logger.LogWarning("Task {TaskId} is already completed and cannot be modified.", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "-1")
                {
                    _logger.LogWarning("Task {TaskId} has already been marked as failed.", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task has already been marked as failed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                //if (failedDTO.ScreenId != expectedNextScreenId)
                //{
                //    return BadRequest(new { message = "Invalid screen transition. The task has already passed this stage." });
                //}

                string status = "Failed";
                string activityType = "Failed";

                failedDTO.NextScreenId = "-1";
                bool updateResult = await _crewTaskDetailsRepository.crewTaskFailedAsync(authenticatedUserId, taskId, status, failedDTO, activityType, userId);

                if (!updateResult)
                {
                    _logger.LogWarning("User {UserId} is not allowed to update task {TaskId}.", authenticatedUserId, taskId);
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                _logger.LogInformation("Task {TaskId} successfully marked as {Status} by User {UserId}.", taskId, status, authenticatedUserId);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new
                {
                    status = status,
                    time = failedDTO.Time.ToString("MM/dd/yyyy HH:mm:ss")
                };
                return Ok(_response);
            }
            catch (SqlException ex) when (ex.Number == 50000) // Check for the custom SQL error number
            {
                _logger.LogError(ex, "SQL error occurred while processing FailedTask for TaskId: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                return BadRequest(_response);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing FailedTask for TaskId: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.Result = new object[0]; // Set Result to an empty array.
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("{taskId}/Loaded")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> LoadedTask(int taskId, [FromBody] CrewTaskParcelDTO parcelDTO)
        {

            _logger.LogInformation("LoadedTask endpoint hit with taskId: {TaskId}, FieldData : {FieldData} ", taskId, parcelDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogDebug("Retrieved userId: {userId}", userId);

                // Validate for duplicate ParcelQR values
                var parcelQRs = parcelDTO.Parcels.Select(p => p.ParcelQR).ToList();
                if (parcelQRs.Count != parcelQRs.Distinct().Count())
                {
                    _logger.LogWarning("Duplicate Parcel QR codes detected.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Duplicate Parcel QR codes detected.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (parcelDTO.Parcels.Any(p => p.ParcelQR == ""))
                {
                    _logger.LogWarning("Parcel Number cannot be empty.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Parcel Number cannot be empty. Please enter parcel number ");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Check if any ParcelQR has the value "string"
                if (parcelDTO.Parcels.Any(p => p.ParcelQR == "string"))
                {
                    _logger.LogWarning("ParcelQR cannot have the value 'string'.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("ParcelQR cannot have the value 'string'");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (parcelDTO.PickupReceiptNumber== "string" || parcelDTO.PickupReceiptNumber =="")
                {
                    _logger.LogWarning("PickupReceiptNumber is required.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("PickupReceiptNumber Required");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (taskId <= 0)
                {
                    _logger.LogWarning("Invalid task ID: {taskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                int authenticatedUserId;
                if (!int.TryParse(userIdClaim.Value, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _logger.LogWarning("Unauthorized access attempt by userId: {authenticatedUserId}", authenticatedUserId);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }
                // Check if the user is authenticated and has the correct claim
                if (userIdClaim == null)
                {
                    _logger.LogWarning("Unauthorized access attempt by userId: {authenticatedUserId}", authenticatedUserId);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_response);
                }

                if (parcelDTO.Location.Long == "" || parcelDTO.Location.Lat == "")
                {
                    _logger.LogWarning("Invalid location data received: Lat={Lat}, Long={Long}", parcelDTO.Location.Lat, parcelDTO.Location.Long);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (parcelDTO.Location.Long == "string" || parcelDTO.Location.Lat == "string")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }


                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    _logger.LogWarning("Task screen ID could not be retrieved for taskId: {taskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "1")
                {
                    _logger.LogWarning("Task {taskId} is already completed.", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "-1")
                {
                    _logger.LogWarning("Task {taskId} is already marked as failed.", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task has already been marked as failed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (parcelDTO.NextScreenId != expectedNextScreenId)
                {
                    _logger.LogWarning("Invalid screen transition for taskId: {taskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid screen transition. The task has already passed this stage.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                string status = "Loaded";
                string activityType = "Loaded";
                bool updateResult = await _crewTaskDetailsRepository.parcelLoadStatusAsync(authenticatedUserId, taskId, status, parcelDTO, activityType, userId);

                if (!updateResult)
                {
                    _logger.LogWarning("User {userId} is not allowed to update task {taskId}.", userId, taskId);
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                _logger.LogInformation("Task {taskId} successfully marked as Loaded.", taskId);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new
                {
                    status = status,
                    time = parcelDTO.Time.ToString("MM/dd/yyyy HH:mm:ss")
                };
                return Ok(_response);
            }

            catch (SqlException ex) when (ex.Number == 50000) // Check for the custom SQL error number
            {
                _logger.LogError(ex, "SQL Exception occurred for taskId: {taskId}", taskId);
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                _response.Result = new object[0]; // Set Result to an empty array.
                return BadRequest(_response);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing taskId: {taskId}", taskId);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.Result = new object[0]; // Set Result to an empty array.
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("{taskId}/Arrived_at_Delivery")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> ArrivedDeliveryTask(int taskId, [FromBody] CrewTaskStatusUpdateDTO arrivedDTO)
        {
            _logger.LogInformation("ArrivedDeliveryTask started for taskId: {TaskId}, FieldData : {FieldData} ", taskId, arrivedDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogDebug("Retrieved userId: {UserId} for taskId: {TaskId}", userId, taskId);


                if (taskId <= 0)
                {
                    _logger.LogWarning("Invalid task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                int authenticatedUserId;
                if (!int.TryParse(userIdClaim.Value, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _logger.LogWarning("Unauthorized access attempt by userId: {UserId}", authenticatedUserId);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }
                // Check if the user is authenticated and has the correct claim
                if (userIdClaim == null)
                {
                    _logger.LogWarning("Unauthorized access attempt by userId: {UserId}", authenticatedUserId);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_response);
                }

                if (arrivedDTO.Location.Long == "" || arrivedDTO.Location.Lat == "")
                {
                    _logger.LogWarning("Invalid location data received: Lat={Lat}, Long={Long}", arrivedDTO.Location.Lat, arrivedDTO.Location.Long);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (arrivedDTO.Location.Long == "string" || arrivedDTO.Location.Lat == "string")
                {
                    _logger.LogWarning("Invalid location data received for taskId: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                _logger.LogDebug("Current screen ID for taskId {TaskId}: {ScreenId}", taskId, currentScreenId);
                if (currentScreenId == null)
                {
                    _logger.LogWarning("TaskId: {TaskId} is already completed or failed (ScreenId: {ScreenId})", taskId, currentScreenId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "1")
                {
                    _logger.LogWarning("TaskId: {TaskId} is already completed (ScreenId: {ScreenId})", taskId, currentScreenId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "-1")
                {
                    _logger.LogWarning("TaskId: {TaskId} is already failed (ScreenId: {ScreenId})", taskId, currentScreenId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task has already been marked as failed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (arrivedDTO.NextScreenId != expectedNextScreenId)
                {
                    _logger.LogWarning("Invalid screen transition for taskId {TaskId}: Expected {Expected}, Received {Received}", taskId, expectedNextScreenId, arrivedDTO.NextScreenId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid screen transition. The task has already passed this stage.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }


                string status = "ArrivedAtDelivery";
                string activityType = "ArrivedDelivery";
                bool updateResult = await _crewTaskDetailsRepository.arrivedDeliveryAsync(authenticatedUserId, taskId, status, arrivedDTO, activityType, userId);

                if (!updateResult)
                {
                    _logger.LogWarning("User {UserId} is not allowed to update taskId {TaskId}", authenticatedUserId, taskId);
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                // Fetch parcel data from repository (stored as comma-separated values in CITTASKDETAIL)
                var parcelData = await _crewTaskDetailsRepository.GetParcelAsync(taskId, authenticatedUserId, userId);
                _logger.LogInformation("Task {TaskId} successfully updated to ArrivedAtDelivery", taskId);
                // Format parcel data for response
                //List<object> parcels = parcelData != null
                //    ? parcelData.Split(',').Select(qrCode => new { parcelQR = qrCode }).Cast<object>().ToList()
                //    : new List<object>();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new
                {
                    status = status,
                    time = arrivedDTO.Time.ToString("MM/dd/yyyy HH:mm:ss"),
                    parcels = parcelData
                };

                return Ok(_response);
            }

            catch (SqlException ex) when (ex.Number == 50000) // Check for the custom SQL error number
            {
                _logger.LogError(ex, "SQL Exception occurred for TaskId: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                _response.Result = new object[0]; // Set Result to an empty array.
                return BadRequest(_response);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred for TaskId: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.Result = new object[0]; // Set Result to an empty array.
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost("{taskId}/Unloaded")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] 
        public async Task<ActionResult<APIResponse>> UnloadParcel(int taskId, CrewTaskUnloadedParcelDTOs parcelDTO)
        {
            _logger.LogInformation("UnloadParcel method started for taskId: {TaskId}, FieldData : {FieldData} ", taskId, parcelDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogDebug("Retrieved User ID: {UserId}", userId);
                // Validate for duplicate ParcelQR values
                var parcelQRs = parcelDTO.Parcels.Select(p => p.ParcelQR).ToList();
                if (parcelQRs.Count != parcelQRs.Distinct().Count())
                {
                    _logger.LogWarning("Duplicate Parcel QR codes detected for Task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Duplicate Parcel QR codes detected.");
                    _response.Result = new object[0];
                    return BadRequest(_response);
                }
                // Check if any ParcelQR has the value "string"
                if (parcelDTO.Parcels.Any(p => p.ParcelQR == "string"))
                {
                    _logger.LogWarning("Invalid Parcel QR value for Task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("ParcelQR cannot have the value 'string'");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (parcelDTO.DeliveryReceiptNumber == "string" || parcelDTO.DeliveryReceiptNumber == "")
                {
                    _logger.LogWarning("Missing Delivery Receipt Number for Task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("DeliveryReceiptNumber Required");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }


                if (taskId <= 0)
                {
                    _logger.LogWarning("Invalid Task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    _response.Result = new object[0];
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                int authenticatedUserId;
                if (!int.TryParse(userIdClaim.Value, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _logger.LogWarning("Unauthorized access attempt by User ID: {UserId} on Task ID: {TaskId}", userId, taskId);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }
                // Check if the user is authenticated and has the correct claim
                if (userIdClaim == null)
                {
                    _logger.LogWarning("Unauthorized access attempt by User ID: {UserId} on Task ID: {TaskId}", userId, taskId);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_response);
                }

                if (parcelDTO.Location.Long == "" || parcelDTO.Location.Lat == "")
                {
                    _logger.LogWarning("Invalid location data received: Lat={Lat}, Long={Long}", parcelDTO.Location.Lat, parcelDTO.Location.Long);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (parcelDTO.Location.Long == "string" || parcelDTO.Location.Lat == "string")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    _logger.LogWarning("Could not retrieve screen ID for Task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "1")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "-1")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task has already been marked as failed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (parcelDTO.NextScreenId != expectedNextScreenId)
                {
                    _logger.LogWarning("Invalid screen transition for Task ID: {TaskId}. Expected: {Expected}, Provided: {Provided}", taskId, expectedNextScreenId, parcelDTO.NextScreenId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid screen transition. The task has already passed this stage.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Fetch loaded parcels for the task
                var loadedParcels = await _crewTaskDetailsRepository.GetParcelAsync(taskId, authenticatedUserId, userId);
                var loadedParcelQRs = loadedParcels.Select(p => p.ParcelQR).ToHashSet();

                // Compare unloaded parcels with loaded parcels
                var unloadedParcelQRs = parcelDTO.Parcels.Select(p => p.ParcelQR).ToHashSet();
                var unmatchedParcels = unloadedParcelQRs.Except(loadedParcelQRs).ToList();

                if (unmatchedParcels.Any())
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add($"The following ParcelQRs are not loaded: {string.Join(", ", unmatchedParcels)}");
                    return BadRequest(_response);
                }
             
                var parcelCounts = await _crewTaskDetailsRepository.GetParclesCountsByTaskId(taskId);
                string status;
                string activityType;
                if (parcelCounts.ParcelsLoaded != parcelCounts.ParcelsUnloaded)
                {
                    parcelDTO.NextScreenId = "CIT-5";
                    status = "Unloaded";
                    activityType = "Unloaded";
                }
                else
                {
                     status = "Unloaded";
                     activityType = "Unloaded";
                }
           
                bool updateResult = await _crewTaskDetailsRepository.parcelUnLoadStatusAsync(authenticatedUserId, taskId, status, parcelDTO, activityType, userId);

                if (!updateResult)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    _response.Result = new object[0];
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                _logger.LogInformation("UnloadParcel successfully completed for Task ID: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new
                {
                    status = status,
                    time = parcelDTO.Time.ToString("MM/dd/yyyy HH:mm:ss")
                };
                return Ok(_response);
            }

            catch (SqlException ex) when (ex.Number == 50000) // Check for the custom SQL error number
            {
                _logger.LogError(ex, "SQL Exception in UnloadParcel for Task ID: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                _response.Result = new object[0];
                return BadRequest(_response);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UnloadParcel for Task ID: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.Result = new object[0];
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost("{taskId}/Completed")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CompletedTask(int taskId, [FromBody] CrewTaskStatusUpdateDTO updateDTO)
        {
            _logger.LogInformation("Completed method started for taskId: {TaskId}, FieldData : {FieldData} ", taskId, updateDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogDebug("Retrieved user ID: {UserId}", userId);

                if (taskId <= 0)
                {
                    _logger.LogWarning("Invalid task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    _response.Result = new object[0];
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name); //code tries to find the user's ID from their claims (data associated with their login session).

                int authenticatedUserId;
                if (!int.TryParse(userIdClaim.Value, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _logger.LogWarning("Unauthorized access attempt by user ID: {UserId}", authenticatedUserId);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }

                // Check if the user is authenticated and has the correct claim
                if (userIdClaim == null)
                {
                    _logger.LogWarning("Unauthorized access attempt by user ID: {UserId}", authenticatedUserId);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_response);
                }

                if (updateDTO.Location.Long == "" || updateDTO.Location.Lat == "")
                {
                    _logger.LogWarning("Invalid location data received: Lat={Lat}, Long={Long}", updateDTO.Location.Lat, updateDTO.Location.Long);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (updateDTO.Location.Long == "string" || updateDTO.Location.Lat == "string")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "1")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "-1")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task has already been marked as failed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 2: Calculate the next expected screen ID
                //var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                //if (updateDTO.ScreenId != expectedNextScreenId)
                //{
                //    _response.StatusCode = HttpStatusCode.BadRequest;
                //    _response.IsSuccess = false;
                //    _response.ErrorMessages.Add("Invalid screen transition. The task has already passed this stage.");
                //    _response.Result = new object[0]; // Set Result to an empty array.
                //    return BadRequest(_response);
                //}


                _logger.LogInformation("User {UserId} attempting to complete task {TaskId}.", authenticatedUserId, taskId);
                var parcelCounts = await _crewTaskDetailsRepository.GetParclesCountsByTaskId(taskId);
                string status;
                string activityType;
                if (parcelCounts.ParcelsLoaded != parcelCounts.ParcelsUnloaded)
                {
                    updateDTO.NextScreenId = "CIT-5";
                    status = "PartialCompleted";
                    activityType = "PartialCompleted";

                    bool updateResultData = await _crewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

                    _response.StatusCode = HttpStatusCode.PartialContent; // 206 Status Code
                    _response.IsSuccess = true;
                    _response.Result = new
                    {
                        status = status,
                        message = "Some parcels are missing. This task cannot be fully completed.",
                        time = updateDTO.Time.ToString("MM/dd/yyyy HH:mm:ss")
                    };
                    return StatusCode((int)HttpStatusCode.PartialContent, _response); // Return 206 response
                }
                else
                {
                    updateDTO.NextScreenId = "1";
                    status = "Completed";
                    activityType = "Completed";
                }

                bool updateResult = await _crewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

                if (!updateResult)
                {
                    _logger.LogWarning("Task completion failed for Task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    _response.Result = new object[0];
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                _logger.LogInformation("Successfully completed Task ID: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new
                {
                    status = status,
                    time = updateDTO.Time.ToString("MM/dd/yyyy HH:mm:ss")
                };
                return Ok(_response);
            }

            catch (SqlException ex) when (ex.Number == 50000) // Check for the custom SQL error number
            {
                _logger.LogError(ex, "SQL Exception in completing for Task ID: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                _response.Result = new object[0];
                return BadRequest(_response);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in CompletedTask for Task ID: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.Result = new object[0];
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("GetParcels/{taskId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetParcels(int taskId)
        {
            _logger.LogInformation("GetParcels method called with taskId: {TaskId}", taskId);
            try
            {
                // Check if the user is authenticated
                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("User is not authenticated.");
                    _response.StatusCode = HttpStatusCode.Unauthorized; 
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authenticated.");
                    _response.Result = new object[0];
                    return Unauthorized(_response);
                }

                // Retrieve user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int authenticatedUserId))
                {
                    _logger.LogWarning("Invalid or missing user claim.");
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid or missing user claim.");
                    _response.Result = new object[0];
                    return Unauthorized(_response);
                }
                _logger.LogInformation("Authenticated User ID: {UserId}", authenticatedUserId);

                // Get user ID from the database
                int userIdFromDb = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();

                // Check if the authenticated user matches the database user
                if (authenticatedUserId != userIdFromDb)
                {
                    _logger.LogWarning("User {UserId} does not have access to task {TaskId}", authenticatedUserId, taskId);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User does not have access to this task.");
                    _response.Result = new object[0];
                    return Unauthorized(_response);
                }

                // Validate task ID
                if (taskId <= 0)
                {
                    _logger.LogWarning("Invalid task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    _response.Result = new object[0];
                    return BadRequest(_response);
                }

                _logger.LogInformation("Fetching parcels for task {TaskId}", taskId);
                // Fetch parcels from the repository
                var parcels = await _crewTaskDetailsRepository.GetParcelAsync(taskId, authenticatedUserId, userIdFromDb);
                _logger.LogInformation("Successfully fetched {ParcelCount} parcels for task {TaskId}", parcels.Count(), taskId);
                
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = parcels;
                return Ok(_response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access exception for task {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.Unauthorized;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.Result = new object[0];
                return Unauthorized(_response);
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                _logger.LogError(ex, "SQL Exception for task {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.Result = new object[0];
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching parcels for task {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.Result = new object[0];
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet("GetPickupDetailByTaskId/{taskId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetParcelsById(int taskId)
        {
            _logger.LogInformation("GetParcelsById called with taskId: {TaskId}", taskId);
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("Unauthorized access attempt.");
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authenticated.");
                    return Unauthorized(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int authenticatedUserId))
                {
                    _logger.LogWarning("Invalid or missing user claim.");
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid or missing user claim.");
                    return Unauthorized(_response);
                }
                _logger.LogInformation("Authenticated User ID: {AuthenticatedUserId}", authenticatedUserId);
                int userIdFromDb = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogInformation("User ID from DB: {UserIdFromDb}", userIdFromDb);
                if (authenticatedUserId != userIdFromDb)
                {
                    _logger.LogWarning("User {UserId} does not have access to task {TaskId}", authenticatedUserId, taskId);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User does not have access to this task.");
                    return Unauthorized(_response);
                }

                if (taskId <= 0)
                {
                    _logger.LogWarning("Invalid task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    return BadRequest(_response);
                }
                _logger.LogInformation("Fetching parcel data for taskId: {TaskId}", taskId);
                // Fetch parcels from the repository
                var parcelData = await _crewTaskDetailsRepository.GetParcelAsync(taskId, authenticatedUserId, userIdFromDb);

                // Group by PickupReceiptNumber and select the first one (if multiple exist)
                var groupedParcels = parcelData
                    .GroupBy(p => p.PickupReceiptNumber)
                    .Select(group => new
                    {
                        PickupReceiptNumber = group.Key,
                        ParcelQRs = group.Select(p => p.ParcelQR).ToArray()
                    })
                    .FirstOrDefault(); // Select the first group (or null if no data)

                if (groupedParcels == null)
                {
                    _logger.LogWarning("No parcel data found for taskId: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No data found for the provided task ID.");
                    return NotFound(_response);
                }
                _logger.LogInformation("Parcel data successfully retrieved for taskId: {TaskId}", taskId);
                // Set response
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = groupedParcels;
                return Ok(_response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access exception for taskId: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.Unauthorized;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return Unauthorized(_response);
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                _logger.LogError(ex, "SQL error while retrieving parcels for taskId: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving parcels for taskId: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }




    }
}
