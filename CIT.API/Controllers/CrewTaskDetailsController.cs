using AutoMapper;
using Azure.Core;
using CIT.API.Data;
using CIT.API.Models;
using CIT.API.Models.Dto.CrewTaskDetails;
using CIT.API.Repository;
using CIT.API.Repository.IRepository;
using CIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
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
        protected APIOtpResponse _OtpResponse;
        protected APIOtpValidateResponse _OtpValidateResponse;
        private EmailService _emailService;
        private readonly IWebApiExecutor _webApiExecutor;
        private readonly ILogger<CrewTaskDetailsController> _logger;

        public CrewTaskDetailsController(ICrewTaskDetailsRepository crewTaskDetailsRepository, IMapper mapper, EmailService emailService,ILogger<CrewTaskDetailsController> logger, IWebApiExecutor webApiExecutor)
        {
            _crewTaskDetailsRepository = crewTaskDetailsRepository;
            _mapper = mapper;
            _response = new APIResponse();
            _OtpResponse = new APIOtpResponse();
            _emailService = emailService;
            _logger = logger;
            _webApiExecutor = webApiExecutor;
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
        public async Task<ActionResult<APIOtpResponse>> ArriveTask(int taskId, [FromBody] CrewTaskStatusUpdateDTO updateDTO)
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
                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("Invalid task ID.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                int authenticatedUserId;
                if (!int.TryParse(userIdClaim.Value, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _logger.LogWarning("Unauthorized access attempt. AuthenticatedUserId: {AuthenticatedUserId}, ExpectedUserId: {ExpectedUserId}", authenticatedUserId, userId);

                    _OtpResponse.StatusCode = HttpStatusCode.Unauthorized;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_OtpResponse);
                }
                // Check if the user is authenticated and has the correct claim
                if (userIdClaim == null)
                {
                    _logger.LogWarning("Unauthorized access attempt. AuthenticatedUserId: {AuthenticatedUserId}, ExpectedUserId: {ExpectedUserId}", authenticatedUserId, userId);

                    _OtpResponse.StatusCode = HttpStatusCode.Unauthorized;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_OtpResponse);
                }

                if (updateDTO.Location.Long == "" || updateDTO.Location.Lat == "")
                {
                    _logger.LogWarning("Invalid location data received: Lat={Lat}, Long={Long}", updateDTO.Location.Lat, updateDTO.Location.Long);

                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("In location Lat and Log is Required.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }

                if (updateDTO.Location.Long == "string" || updateDTO.Location.Lat == "string")
                {
                    _logger.LogWarning("Invalid location data provided: {Location}", updateDTO.Location);
                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("In location Lat and Log is Required.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
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
                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "1")
                {
                    _logger.LogWarning("Task {TaskId} is already completed and cannot be modified.", taskId);
                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "-1")
                {
                    _logger.LogWarning("Task {TaskId} has already been marked as failed.", taskId);
                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("Task has already been marked as failed and cannot be modified further.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (updateDTO.NextScreenId != expectedNextScreenId)
                {
                    _logger.LogWarning("Invalid screen transition for TaskId: {TaskId}. Expected: {ExpectedNextScreenId}, Provided: {ProvidedScreenId}",
                     taskId, expectedNextScreenId, updateDTO.NextScreenId);
                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("Invalid screen transition or The task has already passed this stage.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }


                bool isOtpRequired = await _crewTaskDetailsRepository.CheckOtpRequiredAsync(taskId);
                //_OtpResponse.OTPcheck = isOtpRequired;
                if (isOtpRequired)
                {

                    // 2️⃣ Generate OTP
                    var otpResult = await _crewTaskDetailsRepository.CreateOtpAsync(
                        //mobile: customerMobile,
                        taskId: taskId,
                        purpose: "ARRIVED",
                        createdByUserId: authenticatedUserId
                    );

                    var contactDetails = await _crewTaskDetailsRepository.GetContactDetailsByTaskId(taskId);
                    //string customerMobile = "0745972721";
                    string pikupEmail = contactDetails.PickupEmail;
                    string pickupContact = contactDetails.PickupContact;
                    string smsMessage = $"Your One-Time Password (OTP) for verifying the 'Arrived at Pickup' " +
                        $"action is:\n\n{otpResult.otp}\n\nThis OTP is valid for 5 minutes.";

                    // CALL SMS API HERE
                    var smsResponse = await _webApiExecutor.SendSmsOtpAsync(pickupContact, smsMessage);

                    string emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; background-color:#f5f5f5; padding:20px;'>
                    
                        <div style='max-width:500px; margin:auto; background:white; padding:25px; border-radius:8px; box-shadow:0 0 10px rgba(0,0,0,0.1);'>
                            
                            <h2 style='color:#333; text-align:center;'>CIT - OTP Verification</h2>
                    
                            <p style='font-size:16px; color:#555;'>
                                Dear User,
                                <br/><br/>
                                Your One-Time Password (OTP) for verifying the <strong>Arrived at pickup</strong> action is:
                            </p>
                    
                            <div style='text-align:center; margin:25px 0;'>
                                <div style='display:inline-block; padding:15px 25px; background:#007bff; color:white; font-size:28px; letter-spacing:5px; border-radius:6px;'>
                                    <strong>{otpResult.otp}</strong>
                                </div>
                            </div>
                    
                            <p style='font-size:14px; color:#666;'>
                                This OTP is valid for <strong>5 minutes</strong>.  
                                Please do not share this OTP with anyone for security reasons.
                            </p>
                    
                            <hr style='margin:25px 0;' />
                    
                            <p style='font-size:12px; color:#999; text-align:center;'>
                                If you did not request this OTP, please ignore this email.<br/>
                                © RMS Security System
                            </p>
                        </div>

                    </body>
                    </html>";
                    await _emailService.SendEmailAsync(
                         toEmail: pikupEmail,
                         subject: "CIT OTP Verification",
                         body: emailBody
                            );
                    // 2️⃣ Generate OTP
                    //var (otpTxnId, otp) = await _crewTaskDetailsRepository.CreateOtpAsync(
                    //    taskId: taskId,
                    //    purpose: "ARRIVED",
                    //    createdByUserId: authenticatedUserId
                    //);

                    string status = "Arrived";
                    string activityType = "Arrived";

                    //bool otpCheck;
                    _OtpResponse.StatusCode = HttpStatusCode.OK;
                    _OtpResponse.IsSuccess = true;
                    _OtpResponse.otpRequired = isOtpRequired;
                    _OtpResponse.otpTransactionId = otpResult.otpTxnId;
                    //_OtpResponse.otp = otp;
                    _OtpResponse.Result = new
                    {
                        status = status,
                        time = updateDTO.Time.ToString("MM/dd/yyyy HH:mm:ss")
                    };
                    return Ok(_OtpResponse);
                }
                else
                {
                    string status = "Arrived";
                    string activityType = "Arrived";
                    bool updateResult = await _crewTaskDetailsRepository.UpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

                    if (!updateResult)
                    {
                        _logger.LogWarning("User {UserId} is not allowed to update task {TaskId}.", authenticatedUserId, taskId);
                        _OtpResponse.StatusCode = HttpStatusCode.Forbidden;
                        _OtpResponse.IsSuccess = false;
                        _OtpResponse.ErrorMessages.Add("You are not allowed to update this task.");
                        _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                        return StatusCode((int)HttpStatusCode.Forbidden, _OtpResponse);
                    }
                    _logger.LogInformation("Task {TaskId} successfully marked as {Status} by User {UserId}.", taskId, status, authenticatedUserId);
                    //bool otpCheck;
                    _OtpResponse.StatusCode = HttpStatusCode.OK;
                    _OtpResponse.IsSuccess = true;
                    _OtpResponse.otpRequired = isOtpRequired;
                    _OtpResponse.Result = new
                    {
                        status = status,
                        time = updateDTO.Time.ToString("MM/dd/yyyy HH:mm:ss")
                    };
                    return Ok(_OtpResponse);
                }


            }
            catch (SqlException ex) when (ex.Number == 50000) // Check for the custom SQL error number
            {
                _logger.LogError(ex, "SQL error occurred while processing ArriveTask for TaskId: {TaskId}", taskId);

                _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                _OtpResponse.IsSuccess = false;
                _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                _OtpResponse.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                return BadRequest(_OtpResponse);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing ArriveTask for TaskId: {TaskId}", taskId);

                _OtpResponse.StatusCode = HttpStatusCode.InternalServerError;
                _OtpResponse.IsSuccess = false;
                _OtpResponse.ErrorMessages.Add(ex.Message);
                _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                return StatusCode((int)HttpStatusCode.InternalServerError, _OtpResponse);
            }
        }




        [HttpPost("{taskId}/OtpValidation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIOtpValidateResponse>> OtpValidation(int taskId, [FromBody] OptValidationStatusUpdateDTO updateDTO)
        {
            // ✅ MUST BE THE FIRST LINE IN THIS METHOD
            _OtpValidateResponse = new APIOtpValidateResponse();
           var jsonRequestBody = JsonSerializer.Serialize(updateDTO);
            _logger.LogInformation("Request body for otp validation:{jsonRequestBody} ", jsonRequestBody);

            try
            {
                // Retrieve the userId associated with the provided uuid using the repository method
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();
                _logger.LogDebug("Retrieved userId: {UserId} for UUID", userId);

                if (taskId <= 0)
                {
                    _logger.LogWarning("Invalid task ID: {TaskId}", taskId);
                    _OtpValidateResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpValidateResponse.IsSuccess = false;
                    _OtpValidateResponse.ErrorMessages.Add("Invalid task ID.");
                    _OtpValidateResponse.Result = new object[0];
                    return BadRequest(_OtpValidateResponse);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                int authenticatedUserId;
                if (!int.TryParse(userIdClaim.Value, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _logger.LogWarning("Unauthorized access attempt. AuthenticatedUserId: {AuthenticatedUserId}, ExpectedUserId: {ExpectedUserId}", authenticatedUserId, userId);

                    _OtpValidateResponse.StatusCode = HttpStatusCode.Unauthorized;
                    _OtpValidateResponse.IsSuccess = false;
                    _OtpValidateResponse.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_OtpValidateResponse);
                }

                if (userIdClaim == null)
                {
                    _logger.LogWarning("Unauthorized access.");
                    _OtpValidateResponse.StatusCode = HttpStatusCode.Unauthorized;
                    _OtpValidateResponse.IsSuccess = false;
                    _OtpValidateResponse.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_OtpValidateResponse);
                }

                if (updateDTO.location.Long == "" || updateDTO.location.Lat == "")
                {
                    _logger.LogWarning("Invalid location data received.");
                    _OtpValidateResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpValidateResponse.IsSuccess = false;
                    _OtpValidateResponse.ErrorMessages.Add("Location Lat and Long are required.");
                    _OtpValidateResponse.Result = new object[0];
                    return BadRequest(_OtpValidateResponse);
                }

                if (updateDTO.location.Long == "string" || updateDTO.location.Lat == "string")
                {
                    _logger.LogWarning("Invalid location data provided.");
                    _OtpValidateResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpValidateResponse.IsSuccess = false;
                    _OtpValidateResponse.ErrorMessages.Add("Location Lat and Long are required.");
                    _OtpValidateResponse.Result = new object[0];
                    return BadRequest(_OtpValidateResponse);
                }

                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == null)
                {
                    _OtpValidateResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpValidateResponse.IsSuccess = false;
                    _OtpValidateResponse.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _OtpValidateResponse.Result = new object[0];
                    return BadRequest(_OtpValidateResponse);
                }

                if (currentScreenId == "1")
                {
                    _OtpValidateResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpValidateResponse.IsSuccess = false;
                    _OtpValidateResponse.ErrorMessages.Add("Task already completed.");
                    _OtpValidateResponse.Result = new object[0];
                    return BadRequest(_OtpValidateResponse);
                }

                if (currentScreenId == "-1")
                {
                    _OtpValidateResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpValidateResponse.IsSuccess = false;
                    _OtpValidateResponse.ErrorMessages.Add("Task already failed.");
                    _OtpValidateResponse.Result = new object[0];
                    return BadRequest(_OtpValidateResponse);
                }

                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                if (updateDTO.nextScreenId != expectedNextScreenId)
                {
                    _OtpValidateResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpValidateResponse.IsSuccess = false;
                    _OtpValidateResponse.ErrorMessages.Add("Invalid screen transition.");
                    _OtpValidateResponse.Result = new object[0];
                    return BadRequest(_OtpValidateResponse);
                }

                //bool isOtpRequired = await _crewTaskDetailsRepository.CheckOtpRequiredAsync(taskId);

                string status = "Arrived";
                string activityType = "Arrived";
                bool updateResult = await _crewTaskDetailsRepository.OtpStutasValidation(authenticatedUserId, taskId, status, updateDTO, activityType, userId);
                if (updateResult)
                {
                    bool updateResult1 = await _crewTaskDetailsRepository.ArrivedUpdateTaskStatusAsync(authenticatedUserId, taskId, status, updateDTO, activityType, userId);

                }

                if (!updateResult)
                {
                    _OtpValidateResponse.StatusCode = HttpStatusCode.Forbidden;
                    _OtpValidateResponse.IsSuccess = false;
                    _OtpValidateResponse.ErrorMessages.Add("Invalid OTP.");
                    _OtpValidateResponse.otpValidated = false;
                    return StatusCode((int)HttpStatusCode.Forbidden, _OtpValidateResponse);
                }

                _OtpValidateResponse.StatusCode = HttpStatusCode.OK;
                _OtpValidateResponse.IsSuccess = true;
                _OtpValidateResponse.otpValidated = true;
                _OtpValidateResponse.Result = new
                {
                    status = "Arrived",
                    time = updateDTO.time.ToString("MM/dd/yyyy HH:mm:ss")
                };

                return Ok(_OtpValidateResponse);
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                _OtpValidateResponse.StatusCode = HttpStatusCode.BadRequest;
                _OtpValidateResponse.IsSuccess = false;
                _OtpValidateResponse.ErrorMessages.Add(ex.Message);
                _OtpValidateResponse.Result = new object[0];
                return BadRequest(_OtpValidateResponse);
            }
            catch (Exception ex)
            {
                _OtpValidateResponse.StatusCode = HttpStatusCode.InternalServerError;
                _OtpValidateResponse.IsSuccess = false;
                _OtpValidateResponse.ErrorMessages.Add(ex.Message);
                _OtpValidateResponse.Result = new object[0];
                return StatusCode((int)HttpStatusCode.InternalServerError, _OtpValidateResponse);
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
        public async Task<ActionResult<APIOtpResponse>> ArrivedDeliveryTask(int taskId, [FromBody] CrewTaskStatusUpdateDTO arrivedDTO)
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
                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("Invalid task ID.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                int authenticatedUserId;
                if (!int.TryParse(userIdClaim.Value, out authenticatedUserId) || authenticatedUserId != userId)
                {
                    _logger.LogWarning("Unauthorized access attempt by userId: {UserId}", authenticatedUserId);
                    _OtpResponse.StatusCode = HttpStatusCode.Unauthorized;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("Unauthorized access to tasks.");
                    return Unauthorized(_OtpResponse);
                }
                // Check if the user is authenticated and has the correct claim
                if (userIdClaim == null)
                {
                    _logger.LogWarning("Unauthorized access attempt by userId: {UserId}", authenticatedUserId);
                    _OtpResponse.StatusCode = HttpStatusCode.Unauthorized;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(_OtpResponse);
                }

                if (arrivedDTO.Location.Long == "" || arrivedDTO.Location.Lat == "")
                {
                    _logger.LogWarning("Invalid location data received: Lat={Lat}, Long={Long}", arrivedDTO.Location.Lat, arrivedDTO.Location.Long);

                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("In location Lat and Log is Required.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }

                if (arrivedDTO.Location.Long == "string" || arrivedDTO.Location.Lat == "string")
                {
                    _logger.LogWarning("Invalid location data received for taskId: {TaskId}", taskId);
                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("In location Lat and Log is Required.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }

                // Step 1: Retrieve the current screen ID for this task
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);
                _logger.LogDebug("Current screen ID for taskId {TaskId}: {ScreenId}", taskId, currentScreenId);
                if (currentScreenId == null)
                {
                    _logger.LogWarning("TaskId: {TaskId} is already completed or failed (ScreenId: {ScreenId})", taskId, currentScreenId);
                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }

                // Prevent further modification if ScreenId is already "CIT-6"
                if (currentScreenId == "1")
                {
                    _logger.LogWarning("TaskId: {TaskId} is already completed (ScreenId: {ScreenId})", taskId, currentScreenId);
                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("Task is already marked as completed and cannot be modified further.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }

                // Prevent further modification if the task is already marked as failed with ScreenId "CIT-7"
                if (currentScreenId == "-1")
                {
                    _logger.LogWarning("TaskId: {TaskId} is already failed (ScreenId: {ScreenId})", taskId, currentScreenId);
                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("Task has already been marked as failed and cannot be modified further.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }

                // Step 2: Calculate the next expected screen ID
                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                // Step 3: Check if the request ScreenId matches the expected next ScreenId
                if (arrivedDTO.NextScreenId != expectedNextScreenId)
                {
                    _logger.LogWarning("Invalid screen transition for taskId {TaskId}: Expected {Expected}, Received {Received}", taskId, expectedNextScreenId, arrivedDTO.NextScreenId);
                    _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                    _OtpResponse.IsSuccess = false;
                    _OtpResponse.ErrorMessages.Add("Invalid screen transition. The task has already passed this stage.");
                    _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                    return BadRequest(_OtpResponse);
                }

                // 6️⃣ Check if OTP is required
                bool otpRequired = await _crewTaskDetailsRepository.CheckOtpRequiredAsync(taskId);
                if (otpRequired)
                {
                    //var (otpTxnId, otp) = await _crewTaskDetailsRepository.CreateOtpAsync(
                    //    taskId: taskId,
                    //    purpose: "ARRIVED",
                    //    createdByUserId: authenticatedUserId
                    //);

                    var otpResult = await _crewTaskDetailsRepository.CreateOtpAsync(
                        //mobile: customerMobile,
                        taskId: taskId,
                        purpose: "ArrivedAtDelivery",
                        createdByUserId: authenticatedUserId
                    );

                    var contactDetails = await _crewTaskDetailsRepository.GetContactDetailsByTaskId(taskId);
                    //string customerMobile = "0745972721";

                    string deliveryEmail = contactDetails?.DeliveryEmail;
                    string customerMobile = contactDetails?.DeliveryContact;

                    string smsMessage = $"Your One-Time Password (OTP) for verifying the 'Arrived at Delivery' " +
                        $"action is:\n\n{otpResult.otp}\n\nThis OTP is valid for 5 minutes.";

                    // CALL SMS API HERE
                    var smsResponse = await _webApiExecutor.SendSmsOtpAsync(customerMobile, smsMessage);

                    string emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; background-color:#f5f5f5; padding:20px;'>
                    
                        <div style='max-width:500px; margin:auto; background:white; padding:25px; border-radius:8px; box-shadow:0 0 10px rgba(0,0,0,0.1);'>
                            
                            <h2 style='color:#333; text-align:center;'>RMS - OTP Verification</h2>
                    
                            <p style='font-size:16px; color:#555;'>
                                Dear User,
                                <br/><br/>
                                Your One-Time Password (OTP) for verifying the <strong>Arrived at pickup</strong> action is:
                            </p>
                    
                            <div style='text-align:center; margin:25px 0;'>
                                <div style='display:inline-block; padding:15px 25px; background:#007bff; color:white; font-size:28px; letter-spacing:5px; border-radius:6px;'>
                                    <strong>{otpResult.otp}</strong>
                                </div>
                            </div>
                    
                            <p style='font-size:14px; color:#666;'>
                                This OTP is valid for <strong>5 minutes</strong>.  
                                Please do not share this OTP with anyone for security reasons.
                            </p>
                    
                            <hr style='margin:25px 0;' />
                    
                            <p style='font-size:12px; color:#999; text-align:center;'>
                                If you did not request this OTP, please ignore this email.<br/>
                                © RMS Security System
                            </p>
                        </div>

                    </body>
                    </html>";
                    await _emailService.SendEmailAsync(
                         toEmail: deliveryEmail,
                         subject: "CIT OTP Verification",
                         body: emailBody
                            );

                    string status = "ArrivedAtDelivery";
                    string activityType = "ArrivedDelivery";
                    // Fetch parcel data from repository (stored as comma-separated values in CITTASKDETAIL)
                    var parcelData = await _crewTaskDetailsRepository.GetParcelAsync(taskId, authenticatedUserId, userId);
                    _logger.LogInformation("Task {TaskId} successfully updated to ArrivedAtDelivery", taskId);
                    // Format parcel data for response
                    //List<object> parcels = parcelData != null
                    //    ? parcelData.Split(',').Select(qrCode => new { parcelQR = qrCode }).Cast<object>().ToList()
                    //    : new List<object>();

                    _OtpResponse.StatusCode = HttpStatusCode.OK;
                    _OtpResponse.IsSuccess = true;
                    _OtpResponse.otpRequired = otpRequired;
                    _OtpResponse.otpTransactionId = otpResult.otpTxnId;
                    //_OtpResponse.otp = otp;
                    _OtpResponse.Result = new
                    {
                        status = status,
                        time = arrivedDTO.Time.ToString("MM/dd/yyyy HH:mm:ss"),
                        parcels = parcelData
                    };

                    return Ok(_OtpResponse);
                }
                else
                {

                    string status = "ArrivedAtDelivery";
                    string activityType = "ArrivedDelivery";
                    bool updateResult = await _crewTaskDetailsRepository.arrivedDeliveryAsync(authenticatedUserId, taskId, status, arrivedDTO, activityType, userId);

                    if (!updateResult)
                    {
                        _logger.LogWarning("User {UserId} is not allowed to update taskId {TaskId}", authenticatedUserId, taskId);
                        _OtpResponse.StatusCode = HttpStatusCode.Forbidden;
                        _OtpResponse.IsSuccess = false;
                        _OtpResponse.ErrorMessages.Add("You are not allowed to update this task.");
                        _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                        return StatusCode((int)HttpStatusCode.Forbidden, _OtpResponse);
                    }
                    var parcelData = await _crewTaskDetailsRepository.GetParcelAsync(taskId, authenticatedUserId, userId);

                    _logger.LogInformation("Task {TaskId} successfully updated to ArrivedAtDelivery", taskId);
                    // Format parcel data for response
                    //List<object> parcels = parcelData != null
                    //    ? parcelData.Split(',').Select(qrCode => new { parcelQR = qrCode }).Cast<object>().ToList()
                    //    : new List<object>();

                    _OtpResponse.StatusCode = HttpStatusCode.OK;
                    _OtpResponse.IsSuccess = true;
                    _OtpResponse.otpRequired = otpRequired;
                    _OtpResponse.Result = new
                    {
                        status = status,
                        time = arrivedDTO.Time.ToString("MM/dd/yyyy HH:mm:ss"),
                        parcels = parcelData
                    };
                    return Ok(_OtpResponse);
                }

            }

            catch (SqlException ex) when (ex.Number == 50000) // Check for the custom SQL error number
            {
                _logger.LogError(ex, "SQL Exception occurred for TaskId: {TaskId}", taskId);
                _OtpResponse.StatusCode = HttpStatusCode.BadRequest;
                _OtpResponse.IsSuccess = false;
                _OtpResponse.ErrorMessages.Add(ex.Message); // Display the custom message from the procedure
                _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                return BadRequest(_OtpResponse);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred for TaskId: {TaskId}", taskId);
                _OtpResponse.StatusCode = HttpStatusCode.InternalServerError;
                _OtpResponse.IsSuccess = false;
                _OtpResponse.ErrorMessages.Add(ex.Message);
                _OtpResponse.Result = new object[0]; // Set Result to an empty array.
                return StatusCode((int)HttpStatusCode.InternalServerError, _OtpResponse);
            }
        }


        [HttpPost("{taskId}/OtpValidationDelivary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIOtpValidateResponse>> ArrivedDeliveryTask(
    int taskId,
    [FromBody] OptValidationStatusUpdateDTO arrivedDTO)
        {
            // Initialize response object
            var response = new APIOtpValidateResponse();

            _logger.LogInformation("ArrivedDeliveryTask started for taskId: {TaskId}, FieldData : {FieldData}", taskId, arrivedDTO);

            try
            {
                // 1️⃣ USER VALIDATION
                int userId = await _crewTaskDetailsRepository.GetUserIdByUuidAsync();
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int authenticatedUserId) ||
                    authenticatedUserId != userId)
                {
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("User is not authorized.");
                    return Unauthorized(response);
                }

                if (taskId <= 0)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Invalid Task ID.");
                    return BadRequest(response);
                }

                // 2️⃣ LOCATION VALIDATION
                if (string.IsNullOrWhiteSpace(arrivedDTO.location.Lat) ||
                    string.IsNullOrWhiteSpace(arrivedDTO.location.Long) ||
                    arrivedDTO.location.Lat == "string" ||
                    arrivedDTO.location.Long == "string")
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Location Lat and Long are required.");
                    return BadRequest(response);
                }

                // 3️⃣ SCREEN VALIDATION
                var currentScreenId = await _crewTaskDetailsRepository.GetCurrentScreenIdByTaskId(taskId);

                if (currentScreenId == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Task screen ID could not be retrieved.");
                    return BadRequest(response);
                }

                if (currentScreenId == "1")
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Task already completed.");
                    return BadRequest(response);
                }

                if (currentScreenId == "-1")
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Task already failed.");
                    return BadRequest(response);
                }

                var expectedNextScreenId = await _crewTaskDetailsRepository.GetNextScreenIdByTaskId(taskId);

                if (arrivedDTO.nextScreenId != expectedNextScreenId)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Invalid screen transition.");
                    return BadRequest(response);
                }

                // 4️⃣ OTP VALIDATION
                string status = "ArrivedAtDelivery";
                string activityType = "ArrivedDelivery";

                bool otpValid = await _crewTaskDetailsRepository.OtpStutasValidationDelivary(
                    authenticatedUserId,
                    taskId,
                    status,
                    arrivedDTO,
                    activityType,
                    userId
                );

                if (!otpValid)
                {
                    response.StatusCode = HttpStatusCode.Forbidden;
                    response.IsSuccess = false;
                    response.otpValidated = false;
                    response.ErrorMessages.Add("Invalid OTP.");
                    return StatusCode(403, response);
                }

                // 5️⃣ UPDATE TASK STATUS
                await _crewTaskDetailsRepository.arrivedDeliveryOtpVarification(
                    authenticatedUserId, taskId, status, arrivedDTO, activityType, userId);

                var parcelData = await _crewTaskDetailsRepository.GetParcelAsync(taskId, authenticatedUserId, userId);

                // 6️⃣ SUCCESS RESPONSE
                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccess = true;
                response.otpValidated = true;
                response.Result = new
                {
                    status = status,
                    time = arrivedDTO.time.ToString("MM/dd/yyyy HH:mm:ss"),
                    parcels = parcelData
                };

                return Ok(response);
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
                return StatusCode(500, response);
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
