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
        public async Task<ActionResult<APIResponse>> GetCrewTasks( DateTime? orderDate = null)
        {
            try
            {

                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();

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
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_response);
                }

                // Parse user ID from claim
                int authenticatedUserId;
                if (!int.TryParse(userIdClaim, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }

                // Retrieve assigned tasks from repository based on user ID
                var crewTasks = await _crewTaskDetailsRepository.GetCrewTasksByCommanderIdAsync(authenticatedUserId, userId, orderDate);

                if (crewTasks == null || !crewTasks.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No tasks found for this user.");
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
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();

                // Retrieve the claim for the crew commander ID from the token
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name); // or another claim type, based on your token


                int authenticatedUserId;
                if (!int.TryParse(userIdClaim.Value, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }
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
                var taskDetails = await _crewTaskDetailsRepository.GetTaskDetailsByTaskIdAsync(crewCommanderId, taskId, userId);

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

        [HttpPost("{taskId}/Start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> StartTask(int taskId, [FromBody] CrewTaskStatusUpdateDTO updateDTO)
        {

            try
            {

                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();

                if (taskId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int authenticatedUserId) || authenticatedUserId != userId)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }

                if (updateDTO.Location.Long == "string" || updateDTO.Location.Lat == "string")
                {
                    return BadRequest(new { message = "In location Lat and Log is Required." });
                }

                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);               
                if (currentScreenId == null)
                {
                    return BadRequest(new { message = "Task screen ID could not be retrieved." });
                }


                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "CIT-6")
                {
                    return BadRequest(new { message = "Task is already marked as completed and cannot be modified further." });
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "CIT-7")
                {
                    return BadRequest(new { message = "Task has already been marked as failed and cannot be modified further." });
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (updateDTO.ScreenId != expectedNextScreenId)
                {
                    return BadRequest(new { message = "Invalid screen transition. The task has already passed this stage." });
                }


                string status = "Started";
                string activityType = "Start";
                bool updateResult = await _crewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

                if (!updateResult)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

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
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
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



            try
            {

                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();

                if (taskId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int authenticatedUserId) || authenticatedUserId != userId)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }

                if (updateDTO.Location.Long == "string" || updateDTO.Location.Lat == "string")
                {
                    return BadRequest(new { message = "In location Lat and Log is Required." });
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
                    return BadRequest(new { message = "Task screen ID could not be retrieved." });
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "CIT-6")
                {
                    return BadRequest(new { message = "Task is already marked as completed and cannot be modified further." });
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "CIT-7")
                {
                    return BadRequest(new { message = "Task has already been marked as failed and cannot be modified further." });
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (updateDTO.ScreenId != expectedNextScreenId)
                {
                    return BadRequest(new { message = "Invalid screen transition. The task has already passed this stage." });
                }

                string status = "Arrived";
                string activityType = "Arrived";
                bool updateResult = await _crewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

                if (!updateResult)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

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
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                return BadRequest(_response);
            }

            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
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


            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();

                if (taskId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int authenticatedUserId) || authenticatedUserId != userId)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }

                if (failedDTO.Location.Long == "string" || failedDTO.Location.Lat == "string")
                {
                    return BadRequest(new { message = "In location Lat and Log is Required." });
                }
                else if (failedDTO.FailureReason == "string")
                {
                    return BadRequest(new { message = "FailureReason is Required." });
                }
                else if (failedDTO.ScreenId == "string")
                {
                    return BadRequest(new { message = "ScreenId is Required." });
                }
              

                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);


                // Prevent further modification if ScreenId is already "CIT-7"
                if (currentScreenId == null)
                {
                    return BadRequest(new { message = "Task screen ID could not be retrieved." });
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "CIT-6")
                {
                    return BadRequest(new { message = "Task is already marked as completed and cannot be modified further." });
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "CIT-7")
                {
                    return BadRequest(new { message = "Task has already been marked as failed and cannot be modified further." });
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
                failedDTO.ScreenId = "CIT-7";
                bool updateResult = await _crewTaskDetailsRepository.crewTaskFailedAsync(authenticatedUserId, taskId, status, failedDTO, activityType, userId);

                if (!updateResult)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

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
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                return BadRequest(_response);
            }

            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
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



            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();

                // Validate for duplicate ParcelQR values
                var parcelQRs = parcelDTO.Parcels.Select(p => p.ParcelQR).ToList();
                if (parcelQRs.Count != parcelQRs.Distinct().Count())
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Duplicate Parcel QR codes detected.");
                    return BadRequest(_response);
                }
                // Check if any ParcelQR has the value "string"
                if (parcelDTO.Parcels.Any(p => p.ParcelQR == "string"))
                {
                    return BadRequest(new { message = "ParcelQR cannot have the value 'string'." });
                }

                if (taskId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int authenticatedUserId) || authenticatedUserId != userId)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }

                if (parcelDTO.Location.Long == "string" || parcelDTO.Location.Lat == "string")
                {
                    return BadRequest(new { message = "In location Lat and Log is Required." });
                }


                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    return BadRequest(new { message = "Task screen ID could not be retrieved." });
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "CIT-6")
                {
                    return BadRequest(new { message = "Task is already marked as completed and cannot be modified further." });
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "CIT-7")
                {
                    return BadRequest(new { message = "Task has already been marked as failed and cannot be modified further." });
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (parcelDTO.ScreenId != expectedNextScreenId)
                {
                    return BadRequest(new { message = "Invalid screen transition. The task has already passed this stage." });
                }

                string status = "Loaded";
                string activityType = "Loaded";
                bool updateResult = await _crewTaskDetailsRepository.parcelLoadStatusAsync(authenticatedUserId, taskId, status, parcelDTO, activityType, userId);

                if (!updateResult)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

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
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                return BadRequest(_response);
            }

            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
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


            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();


                if (taskId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int authenticatedUserId) || authenticatedUserId != userId)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }

                if (arrivedDTO.Location.Long == "string" || arrivedDTO.Location.Lat == "string")
                {
                    return BadRequest(new { message = "In location Lat and Log is Required." });
                }

                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    return BadRequest(new { message = "Task screen ID could not be retrieved." });
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "CIT-6")
                {
                    return BadRequest(new { message = "Task is already marked as completed and cannot be modified further." });
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "CIT-7")
                {
                    return BadRequest(new { message = "Task has already been marked as failed and cannot be modified further." });
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (arrivedDTO.ScreenId != expectedNextScreenId)
                {
                    return BadRequest(new { message = "Invalid screen transition. The task has already passed this stage." });
                }


                string status = "ArrivedAtDelivery";
                string activityType = "ArrivedDelivery";
                bool updateResult = await _crewTaskDetailsRepository.arrivedDeliveryAsync(authenticatedUserId, taskId, status, arrivedDTO, activityType, userId);

                if (!updateResult)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                // Fetch parcel data from repository (stored as comma-separated values in CITTASKDETAIL)
                var parcelData = await _crewTaskDetailsRepository.GetParcelData(taskId);

                // Format parcel data for response
                List<object> parcels = parcelData != null
                    ? parcelData.Split(',').Select(qrCode => new { parcelQR = qrCode }).Cast<object>().ToList()
                    : new List<object>();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new
                {
                    status = status,
                    time = arrivedDTO.Time.ToString("MM/dd/yyyy HH:mm:ss"),
                    parcels = parcels
                };

                return Ok(_response);
            }

            catch (SqlException ex) when (ex.Number == 50000) // Check for the custom SQL error number
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                return BadRequest(_response);
            }

            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost("{taskId}/Unloaded")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> UnloadParcel(int taskId, [FromBody] CrewTaskParcelDTO parcelDTO)
        {


            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();

                // Validate for duplicate ParcelQR values
                var parcelQRs = parcelDTO.Parcels.Select(p => p.ParcelQR).ToList();
                if (parcelQRs.Count != parcelQRs.Distinct().Count())
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Duplicate Parcel QR codes detected.");
                    return BadRequest(_response);
                }
                // Check if any ParcelQR has the value "string"
                if (parcelDTO.Parcels.Any(p => p.ParcelQR == "string"))
                {
                    return BadRequest(new { message = "ParcelQR cannot have the value 'string'." });
                }

                if (taskId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int authenticatedUserId) || authenticatedUserId != userId)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }

                if (parcelDTO.Location.Long == "string" || parcelDTO.Location.Lat == "string")
                {
                    return BadRequest(new { message = "In location Lat and Log is Required." });
                }

                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    return BadRequest(new { message = "Task screen ID could not be retrieved." });
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "CIT-6")
                {
                    return BadRequest(new { message = "Task is already marked as completed and cannot be modified further." });
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "CIT-7")
                {
                    return BadRequest(new { message = "Task has already been marked as failed and cannot be modified further." });
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (parcelDTO.ScreenId != expectedNextScreenId)
                {
                    return BadRequest(new { message = "Invalid screen transition. The task has already passed this stage." });
                }

                string status = "Unloaded";
                string activityType = "Unloaded";
                bool updateResult = await _crewTaskDetailsRepository.parcelLoadStatusAsync(authenticatedUserId, taskId, status, parcelDTO, activityType, userId);

                if (!updateResult)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

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
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                return BadRequest(_response);
            }

            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
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

            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();

                if (taskId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid task ID.");
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name); //code tries to find the user's ID from their claims (data associated with their login session).

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int authenticatedUserId) || authenticatedUserId != userId) // If the claim with the user ID is missing, then the user isn’t authorized., If the user ID claim is there but can’t be converted to a valid integer (which the code expects), it’s also unauthorized.
                                                                                                                                                     //If the user’s ID from the claim doesn’t match the ID in the update request, the user isn’t authorized to update this                        particular task.
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_response);
                }

                if (updateDTO.Location.Long == "string" || updateDTO.Location.Lat == "string")
                {
                    return BadRequest(new { message = "In location Lat and Log is Required." });
                }

                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    return BadRequest(new { message = "Task screen ID could not be retrieved." });
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "CIT-6")
                {
                    return BadRequest(new { message = "Task is already marked as completed and cannot be modified further." });
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "CIT-7")
                {
                    return BadRequest(new { message = "Task has already been marked as failed and cannot be modified further." });
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (updateDTO.ScreenId != expectedNextScreenId)
                {
                    return BadRequest(new { message = "Invalid screen transition. The task has already passed this stage." });
                }


                string status = "Completed";
                string activityType = "Completed";
                bool updateResult = await _crewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

                if (!updateResult)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("You are not allowed to update this task.");
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

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
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                return BadRequest(_response);
            }

            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }



    }
}
