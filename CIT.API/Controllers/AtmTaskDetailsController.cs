using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.AtmCrewTaskDetails;
using CIT.API.Models.Dto.BSSCrewTaskDetails;
using CIT.API.Repository;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Security.Claims;

namespace CIT.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AtmTaskDetailsController : ControllerBase
    {
        private readonly IAtmCrewTaskDetailsRepository _atmCrewTaskDetailsRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly ILogger<AtmTaskDetailsController> _logger;

        public AtmTaskDetailsController(IAtmCrewTaskDetailsRepository atmCrewTaskDetailsRepository, IMapper mapper, ILogger<AtmTaskDetailsController> logger)
        {
            _atmCrewTaskDetailsRepository = atmCrewTaskDetailsRepository;
            _mapper = mapper;
            _logger = logger;
            _response = new APIResponse();
        }

        [HttpPost("{taskId}/Start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> StartBssTask(int taskId, [FromBody] AtmCrewTaskStatusUpdateDTO updateDTO)
        {
            _logger.LogInformation("Start Task in ATM  api called with taskId: {TaskId}, FieldData : {FieldData} ", taskId, updateDTO);

            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _atmCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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
                var currentScreenId = await _atmCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
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
                var expectedNextScreenId = await _atmCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

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
                bool updateResult = await _atmCrewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

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
        public async Task<ActionResult<APIResponse>> ArrivedBankTask(int taskId, [FromBody] AtmCrewTaskStatusUpdateDTO updateDTO)
        {
            _logger.LogInformation("Arrive Atm Task  api called with taskId: {TaskId}, FieldData : {FieldData} ", taskId, updateDTO);

            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _atmCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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
                var currentScreenId = await _atmCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
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
                var expectedNextScreenId = await _atmCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

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
                bool updateResult = await _atmCrewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

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

        [HttpPost("{taskId}/LoadedAtBank")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> LoadedAtBankTask(int taskId, [FromBody] AtmParcelLoadedDTO parcelDTO)
        {

            _logger.LogInformation("LoadedTask in ATM endpoint hit with taskId: {TaskId}, FieldData : {FieldData} ", taskId, parcelDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _atmCrewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogDebug("Retrieved userId: {userId}", userId);


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
                var currentScreenId = await _atmCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
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
                var expectedNextScreenId = await _atmCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

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

                string status = "LoadedAtBank";
                string activityType = "LoadedAtBank";
                bool updateResult = await _atmCrewTaskDetailsRepository.ParcelLoadStatusAsync(authenticatedUserId, taskId, status, parcelDTO, activityType, userId);

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


        [HttpPost("{taskId}/FailedAtm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> FailedBssTask(int taskId, [FromBody] AtmTaskFailedDTO failedDTO)
        {
            _logger.LogInformation("FailedTask endpoint hit with taskId: {TaskId}, FieldData : {FieldData} ", taskId, failedDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _atmCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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
                var currentScreenId = await _atmCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);


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
                var expectedNextScreenId = await _atmCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                //if (failedDTO.ScreenId != expectedNextScreenId)
                //{
                //    return BadRequest(new { message = "Invalid screen transition. The task has already passed this stage." });
                //}

                string status = "Failed";
                string activityType = "Failed";

                failedDTO.NextScreenId = "-1";
                bool updateResult = await _atmCrewTaskDetailsRepository.crewTaskFailedAsync(authenticatedUserId, taskId, status, failedDTO, activityType, userId);

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


        [HttpPost("{taskId}/ArrivedDelivery")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> ArrivedDeliveryTask(int taskId, [FromBody] AtmCrewTaskStatusUpdateDTO arrivedDTO)
        {
            _logger.LogInformation("ArrivedDeliveryTask started for taskId: {TaskId}, FieldData : {FieldData} ", taskId, arrivedDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _atmCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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
                var currentScreenId = await _atmCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
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
                var expectedNextScreenId = await _atmCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

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
                bool updateResult = await _atmCrewTaskDetailsRepository.arrivedDeliveryAsync(authenticatedUserId, taskId, status, arrivedDTO, activityType, userId);

                if (!updateResult)
                {
                    _logger.LogWarning("User {UserId} is not allowed to update taskId {TaskId}", authenticatedUserId, taskId);
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    _response.Result = new object[0]; // Set Result to an empty array.
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
               
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new
                {
                    status = status,
                    time = arrivedDTO.Time.ToString("MM/dd/yyyy HH:mm:ss"),
                    //parcels = parcelData
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

        [HttpPost("{taskId}/LoadedAtAtm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> LoadedAtAtmTask(int taskId, AtmParcelLoadedAtATMDTO parcelDTO)
        {
            _logger.LogInformation("loaded parcel at atm in in ATM api method started for taskId: {TaskId}, FieldData : {FieldData} ", taskId, parcelDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _atmCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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
                var currentScreenId = await _atmCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
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
                var expectedNextScreenId = await _atmCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

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
                var loadedParcels = await _atmCrewTaskDetailsRepository.GetParcelLoadedAtBankAsync(taskId, authenticatedUserId);
                var loadedParcelQRs = loadedParcels.Select(p => p.ParcelQR).ToHashSet();

                // Compare unloaded parcels with loaded parcels 
                // Compare unloaded parcels with loaded parcels
                var loadedParcelAtATMQRs = parcelDTO.Parcels.Select(p => p.ParcelQR).ToHashSet();
                var unmatchedParcels = loadedParcelAtATMQRs.Except(loadedParcelQRs).ToList();

                if (unmatchedParcels.Any())
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add($"The following Parcel are not loaded: {string.Join(", ", unmatchedParcels)}");
                    return BadRequest(_response);
                }

                var parcelCounts = await _atmCrewTaskDetailsRepository.GetParclesCountsByTaskId(taskId);
                string status;
                string activityType;
                            
                    status = "LoadedAtAtm";
                    activityType = "LoadedAtAtm";
                

                bool updateResult = await _atmCrewTaskDetailsRepository.ParcelLoadAtAtmStatusAsync(authenticatedUserId, taskId, status, parcelDTO, activityType, userId);

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


        [HttpPost("{taskId}/UnloadedAtAtm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> UnloadedAtAtmTask(int taskId, AtmParcelLoadedAtATMDTO parcelDTO)
        {
            _logger.LogInformation("UnloadParcel method started for taskId: {TaskId}, FieldData : {FieldData} ", taskId, parcelDTO);
            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _atmCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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
                var currentScreenId = await _atmCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
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
                var expectedNextScreenId = await _atmCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

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
          
                string status;
                string activityType;               
                status = "UnLoadedAtAtm";
                activityType = "UnLoadedAtAtm";

                bool updateResult = await _atmCrewTaskDetailsRepository.ParcelUnLoadAtAtmStatusAsync(authenticatedUserId, taskId, status, parcelDTO, activityType, userId);

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
        public async Task<ActionResult<APIResponse>> CompletedTask(int taskId, [FromBody] ParcelUnLoadedAtBankDTO updateDTO)
        {
            _logger.LogInformation("Completed Task  in ATM api called with taskId: {TaskId}, FieldData : {FieldData} ", taskId, updateDTO);

            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _atmCrewTaskDetailsRepository.GetUserIdByUuidAsync();
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
                var currentScreenId = await _atmCrewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
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
                var expectedNextScreenId = await _atmCrewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

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

                // Fetch loaded parcels for the task
                var loadedParcels = await _atmCrewTaskDetailsRepository.GetParcelUnLoadedAtAtmAsync(taskId, authenticatedUserId);
                var loadedParcelQRs = loadedParcels.Select(p => p.ParcelQR).ToHashSet();

                // Compare unloaded parcels with loaded parcels 
                // Compare unloaded parcels with loaded parcels
                var unloadedParcelAtATMQRs = updateDTO.Parcels.Select(p => p.ParcelQR).ToHashSet();
                var unmatchedParcels = unloadedParcelAtATMQRs.Except(loadedParcelQRs).ToList();

                if (unmatchedParcels.Any())
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add($"The following Parcel are not loaded: {string.Join(", ", unmatchedParcels)}");
                    return BadRequest(_response);
                }

                string status = "Completed";
                string activityType = "Completed";
                bool updateResult = await _atmCrewTaskDetailsRepository.ParcelUnLoadAtBankStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

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

        [HttpGet("GetParcelsLoadedAtBank/{taskId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetParcelsLoadedAtBankAsync(int taskId)
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
                int userIdFromDb = await _atmCrewTaskDetailsRepository.GetUserIdByUuidAsync();

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
                var parcels = await _atmCrewTaskDetailsRepository.GetParcelAsync(taskId, authenticatedUserId, userIdFromDb);
                //_logger.LogInformation("Successfully fetched {ParcelCount} parcels for task {TaskId}", parcels.Count(), taskId);

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

        [HttpGet("GetParcelsUnLoadedAtAtm/{taskId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetParcelsUnloadAtAtmAsync(int taskId)
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
                int userIdFromDb = await _atmCrewTaskDetailsRepository.GetUserIdByUuidAsync();

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
                var parcels = await _atmCrewTaskDetailsRepository.GetParcelUnloadedAsync(taskId, authenticatedUserId, userIdFromDb);
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
    }
}
