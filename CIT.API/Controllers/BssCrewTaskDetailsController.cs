using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.BSSCrewTaskDetails;
using CIT.API.Models.Dto.CrewTaskDetails;
using CIT.API.Repository;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Security.Claims;

namespace CIT.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BssCrewTaskDetailsController : ControllerBase
    {
        private readonly IBSSCrewTaskDetailsRepository _bssCrewTaskDetailsRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly ILogger<BssCrewTaskDetailsController> _logger;

        public BssCrewTaskDetailsController(IBSSCrewTaskDetailsRepository bssCrewTaskDetailsRepository, IMapper mapper, ILogger<BssCrewTaskDetailsController> logger)
        {
            _bssCrewTaskDetailsRepository = bssCrewTaskDetailsRepository;
            _mapper = mapper;
            _response = new APIResponse();
            _logger = logger;
        }

        [HttpPost("{taskId}/StartBss")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> StartBssTask(int taskId, [FromBody] BSSCrewTaskStatusUpdateDTO updateDTO)
        {
            _logger.LogInformation("Start Task  api called with taskId: {TaskId}, FieldData : {FieldData} ", taskId, updateDTO);

            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _bssCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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
                var currentScreenId = await _bssCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    _logger.LogWarning("Task screen ID could not be retrieved for TaskId: {TaskId}", taskId);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);

                }


                // Prevent further modification if ScreenId is already "BSS-Completed"
                if (currentScreenId == "1")
                {
                    _logger.LogWarning("TaskId: {TaskId} is already marked as completed.", taskId);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "BSS-Failed"
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
                var expectedNextScreenId = await _bssCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

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
                bool updateResult = await _bssCrewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

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

        [HttpPost("{taskId}/ArrivedBss")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> ArrivedBssTask(int taskId, [FromBody] BSSCrewTaskStatusUpdateDTO updateDTO)
        {
            _logger.LogInformation("Arrive Bss Task  api called with taskId: {TaskId}, FieldData : {FieldData} ", taskId, updateDTO);

            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _bssCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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
                var currentScreenId = await _bssCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    _logger.LogWarning("Task screen ID could not be retrieved for TaskId: {TaskId}", taskId);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);

                }


                // Prevent further modification if ScreenId is already "Bss-Completed"
                if (currentScreenId == "1")
                {
                    _logger.LogWarning("TaskId: {TaskId} is already marked as completed.", taskId);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "Bss-Failed"
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
                var expectedNextScreenId = await _bssCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

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


                string status = "Arrived";
                string activityType = "Arrived";
                bool updateResult = await _bssCrewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

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


        [HttpPost("{taskId}/FailedBss")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> FailedBssTask(int taskId, [FromBody] BssTaskFailedDTO failedDTO)
        {
            _logger.LogInformation("FailedTask endpoint hit with taskId: {TaskId}, FieldData : {FieldData} ", taskId, failedDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _bssCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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
                    _logger.LogWarning("Invalid location data provided: {Location}", failedDTO.Location);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (failedDTO.Location.Long == "string" || failedDTO.Location.Lat == "string")
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
                var currentScreenId = await _bssCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);


                // Prevent further modification if ScreenId is already "Bss-Failed"
                if (currentScreenId == null)
                {
                    _logger.LogWarning("Task screen ID could not be retrieved for TaskId: {TaskId}", taskId);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if ScreenId is already "Bss-Completed"
                if (currentScreenId == "1")
                {
                    _logger.LogWarning("Task {TaskId} is already completed and cannot be modified.", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "Bss-Failed"
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
                var expectedNextScreenId = await _bssCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                //if (failedDTO.ScreenId != expectedNextScreenId)
                //{
                //    return BadRequest(new { message = "Invalid screen transition. The task has already passed this stage." });
                //}

                string status = "Failed";
                string activityType = "Failed";

                failedDTO.NextScreenId = "-1";
                bool updateResult = await _bssCrewTaskDetailsRepository.crewTaskFailedAsync(authenticatedUserId, taskId, status, failedDTO, activityType, userId);

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

        [HttpPost("{taskId}/SaveAmountBss")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> SaveAmountBssTask(int taskId, [FromBody] BssSaveAmountDTO bssSaveAmount)
        {
            _logger.LogInformation("SaveAmount Bss Task  api called with taskId: {TaskId}, FieldData : {FieldData} ", taskId, bssSaveAmount);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _bssCrewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogInformation("Retrieved userId: {UserId}", userId);
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

                //if (bssSaveAmount.SaveAmount == 0 || bssSaveAmount.SaveAmount <= 0)
                //{
                //    _logger.LogWarning("Invalid Save Amount: {SaveAmount}", bssSaveAmount.SaveAmount);
                //    _response.StatusCode = HttpStatusCode.BadRequest;
                //    _response.IsSuccess = false;
                //    _response.ErrorMessages.Add("Please fill the Save Amount field");
                //    _response.Result = new object[0]; // Set Result to an empty array.
                //    return BadRequest(_response);
                //}

                if (bssSaveAmount.Location.Long == "" || bssSaveAmount.Location.Lat == "")
                {
                    _logger.LogWarning("Invalid location data: {Location}", bssSaveAmount.Location);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                if (bssSaveAmount.Location.Long == "string" || bssSaveAmount.Location.Lat == "string")
                {
                    _logger.LogWarning("Invalid location data: {Location}", bssSaveAmount.Location);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("In location Lat and Log is Required.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _bssCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                _logger.LogInformation("Retrieved currentScreenId: {ScreenId}", currentScreenId);
                if (currentScreenId == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }


                if (currentScreenId == "1")
                {
                    _logger.LogWarning("Invalid task state: {ScreenId}", currentScreenId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }


                if (currentScreenId == "-1")
                {
                    _logger.LogWarning("Invalid task state: {ScreenId}", currentScreenId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task has already been marked as failed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _bssCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);
                _logger.LogInformation("Expected next screenId: {ScreenId}", expectedNextScreenId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (bssSaveAmount.NextScreenId != expectedNextScreenId)
                {
                    _logger.LogWarning("Invalid screen transition for taskId: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid screen transition. The task has already passed this stage.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }
                //var TotalAmount = bssSaveAmount.LocalAmount + bssSaveAmount.Currency.USD + bssSaveAmount.Currency.GBP + bssSaveAmount.Currency.EURO + bssSaveAmount.Currency.ZAR + bssSaveAmount.Currency.Others;

                string status = "SaveAmount";
                string activityType = "SaveAmount";
                bool updateResult = await _bssCrewTaskDetailsRepository.SaveAmountAsync(authenticatedUserId, taskId, status, bssSaveAmount, activityType, userId);

                if (!updateResult)
                {
                    _logger.LogWarning("Failed to update task: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                _logger.LogInformation("Successfully updated task: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new
                {
                    status = status,
                    time = bssSaveAmount.Time.ToString("MM/dd/yyyy HH:mm:ss")

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

        [HttpPost("{taskId}/LoadedBss")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> LoadedBssTask(int taskId, [FromBody] BssParcelLoadDTO parcelDTO)
        {

            _logger.LogInformation("LoadedTask endpoint hit with taskId: {TaskId}, FieldData : {FieldData} ", taskId, parcelDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _bssCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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

                // Check if any ParcelQR has the value "string"
                if (parcelDTO.Parcels.Any(p => p.ParcelQR == ""))
                {
                    _logger.LogWarning("Parcel Number cannot be empty.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Parcel Number cannot be empty. Please enter Parcel Number");
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

                if (parcelDTO.PickupReceiptNumber == "string" || parcelDTO.PickupReceiptNumber == "")
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
                var currentScreenId = await _bssCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    _logger.LogWarning("Task screen ID could not be retrieved for taskId: {taskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if ScreenId is already "Bss-Completed"
                if (currentScreenId == "1")
                {
                    _logger.LogWarning("Task {taskId} is already completed.", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "Bss-Failed"
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
                var expectedNextScreenId = await _bssCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

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
                bool updateResult = await _bssCrewTaskDetailsRepository.parcelLoadStatusAsync(authenticatedUserId, taskId, status, parcelDTO, activityType, userId);

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

        [HttpPost("{taskId}/ArrivedDeliveryBss")]
        [ProducesResponseType(StatusCodes.Status200OK)]        
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> ArrivedDeliveryBssTask(int taskId, [FromBody] BSSCrewTaskStatusUpdateDTO arrivedDTO)
        {
            _logger.LogInformation("ArrivedDeliveryTask started for taskId: {TaskId}, FieldData : {FieldData} ", taskId, arrivedDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _bssCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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
                    _logger.LogWarning("Invalid location data received for taskId: {TaskId}", taskId);
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
                var currentScreenId = await _bssCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
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

                // Prevent further modification if ScreenId is already "Bss-Completed"
                if (currentScreenId == "1")
                {
                    _logger.LogWarning("TaskId: {TaskId} is already completed (ScreenId: {ScreenId})", taskId, currentScreenId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId ""
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
                var expectedNextScreenId = await _bssCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

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


                string status = "ArrivedDelivery";
                string activityType = "ArrivedDelivery";
                bool updateResult = await _bssCrewTaskDetailsRepository.arrivedDeliveryAsync(authenticatedUserId, taskId, status, arrivedDTO, activityType, userId);

                if (!updateResult)
                {
                    _logger.LogWarning("User {UserId} is not allowed to update taskId {TaskId}", authenticatedUserId, taskId);
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                // Fetch parcel data from repository (stored as comma-separated values in BSSTASKDETAIL)
                var parcelData = await _bssCrewTaskDetailsRepository.GetParcelAsync(taskId, authenticatedUserId, userId);
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

        [HttpPost("{taskId}/UnloadBss")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> UnloadBssParcel(int taskId, BssParcelUnloadDTO parcelDTO)
        {
            _logger.LogInformation("UnloadParcel method started for taskId: {TaskId}, FieldData : {FieldData} ", taskId, parcelDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _bssCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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

                // Check if any ParcelQR has the value "empty"
                if (parcelDTO.Parcels.Any(p => p.ParcelQR == ""))
                {
                    _logger.LogWarning("Invalid Parcel Number value for Task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Parcel Number cannot be empty");
                    _response.Result = new object[0]; // Set Result to an empty array.
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
                var currentScreenId = await _bssCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    _logger.LogWarning("Could not retrieve screen ID for Task ID: {TaskId}", taskId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if ScreenId is already "Bss-Completed"
                if (currentScreenId == "1")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "Bss-Failed"
                if (currentScreenId == "-1")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task has already been marked as failed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _bssCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

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
                var loadedParcels = await _bssCrewTaskDetailsRepository.GetParcelAsync(taskId, authenticatedUserId, userId);
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

                var parcelCounts = await _bssCrewTaskDetailsRepository.GetParclesCountsByTaskId(taskId);
                string status;
                string activityType;
                if (parcelCounts.ParcelLoaded != parcelCounts.ParcelUnloaded)
                {
                    parcelDTO.NextScreenId = "BSS-Unloaded";
                    status = "Unloaded";
                    activityType = "Unloaded";
                }
                else
                {
                    status = "Unloaded";
                    activityType = "Unloaded";
                }


                bool updateResult = await _bssCrewTaskDetailsRepository.parcelUnLoadStatusAsync(authenticatedUserId, taskId, status, parcelDTO, activityType, userId);

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


        [HttpPost("{taskId}/CompletedBss")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CompletedBssTask(int taskId, [FromBody] BSSCrewTaskStatusUpdateDTO updateDTO)
        {
            _logger.LogInformation("Completed method started for taskId: {TaskId}, FieldData : {FieldData} ", taskId, updateDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _bssCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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
                var currentScreenId = await _bssCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if ScreenId is already "Bss-Completed"
                if (currentScreenId == "1")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "Bss-Failed"
                if (currentScreenId == "-1")
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Task has already been marked as failed and cannot be modified further.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_response);
                }

                // Step 2: Calculate the next expected screen ID
                
                _logger.LogInformation("User {UserId} attempting to complete task {TaskId}.", authenticatedUserId, taskId);
                var parcelCounts = await _bssCrewTaskDetailsRepository.GetParclesCountsByTaskId(taskId);
                string status;
                string activityType;
                if (parcelCounts.ParcelLoaded != parcelCounts.ParcelUnloaded)
                {
                    updateDTO.NextScreenId = "BSS-Unloaded";
                    status = "PartialCompleted";
                    activityType = "PartialCompleted";

                    bool updateResultData = await _bssCrewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

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


                bool updateResult = await _bssCrewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

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
       
        [HttpGet("{taskId:int}/GetAmount")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetTotalAmount(int taskId)
        {
            _logger.LogInformation("Fetching Total Amount for TaskId: {TaskId}", taskId);

            try
            {
                int totalAmount = await _bssCrewTaskDetailsRepository.GetTotalAmountByTaskId(taskId);

                if (totalAmount == 0)
                {
                    _logger.LogWarning("No Total Amount found for TaskId: {TaskId}", taskId);

                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No Total Amount found for the given TaskId.");
                    return NotFound(_response);
                }

                _logger.LogInformation("Successfully fetched Total Amount for TaskId: {TaskId}", taskId);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = totalAmount;

                return Ok(_response);
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                _logger.LogError(ex, "SQL Exception occurred while fetching Total Amount for TaskId: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching Total Amount for TaskId: {TaskId}", taskId);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

    }
}
