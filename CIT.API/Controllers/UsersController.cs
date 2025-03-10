using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.Branch;
using CIT.API.Models.Dto.UserMasterApi;
using CIT.API.Models.Dto.Login;
using CIT.API.Models.Dto.UserMasterApi;
using CIT.API.Repository;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using CIT.API.Models.Dto.Customer;
using Microsoft.AspNetCore.Authorization;
using CIT.API.Models.Dto.User;

namespace CIT.API.Controllers
{
    [Route("api/UsersAuth")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly ILogger<UsersController> _logger;
        public UsersController(IUserRepository userRepo, IMapper mapper, ILogger<UsersController> logger)
        {
            _userRepo = userRepo;
            _mapper = mapper;
            _response = new();
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
        {
            _logger.LogInformation("Login attempt for user: {Username}", model.UserName);
            var loginResponse = await _userRepo.Login(model);

            if (loginResponse.User == null || string.IsNullOrEmpty(loginResponse.Token))
            {
                _logger.LogWarning("Invalid login attempt for user: {Username}", model.UserName);
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Username or password is incorrect");
                return BadRequest(_response);
            }
            _logger.LogInformation("User {Username} logged in successfully", model.UserName);
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Result = loginResponse;
            return Ok(_response);
        }


        [HttpPost("CreateUser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCrewUser([FromBody] UserCreateDTO crewUser)
        {
            _logger.LogInformation("Creating new user");
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid user creation request");
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Invalid data provided");
                return BadRequest(_response);
            }
            int Res = 0;
            try
            {
                if (crewUser == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid crewUser data.");
                    return BadRequest(_response);
                }

                // Call the repository method to create the user
                var createdUser = await _userRepo.CrewUserCreate(crewUser);
                _logger.LogInformation("User created successfully");
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = createdUser;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating user");
                // Populate the APIResponse with error details
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("GetAllUser")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetAllUserList()
        {
            _logger.LogInformation("Fetching all users");
            try
            {
                IEnumerable<UserMasterModel> userModels = await _userRepo.GetAllUsers();

                if (userModels == null || !userModels.Any())
                {
                    _logger.LogWarning("No users found");
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No User found.");
                    return NotFound(_response);
                }

                // Debugging: Log or inspect data
                //foreach (var user in userModels)
                //{
                //    Console.WriteLine($"UserId: {user.UserId}, UserName: {user.UserName}");
                //}

                if (userModels.Any(user => user == null))
                {
                    throw new Exception("One or more users in the list are null.");
                }

                // Map to DTO
                //_response.Result = _mapper.Map<List<UserListDTO>>(userModels);
                _response.Result = userModels;

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching users");
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("{id:int}", Name = "GetSingleUser")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetSingleUser(int id)
        {
            _logger.LogInformation("Fetching single users");
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var user = await _userRepo.GetUserById(id);

                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }

                //_response.Result = user;
                _response.Result = _mapper.Map<UserListDTO>(user);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching users");
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }



        [HttpPut("{userId:int}", Name = "UpdateUser")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateUser(int userId, [FromBody] UserUpdateDTO updateDTO)
        {
            _logger.LogInformation("UpdateUser called with userId: {UserId}", userId);
            try
            {
                if (updateDTO == null || userId != updateDTO.UserId)
                {
                    _logger.LogWarning("UpdateUser failed - Invalid userId: {UserId}", userId);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid  userId.");

                    return BadRequest(_response);
                }

                // Null check for _mapper
                if (_mapper == null)
                {
                    throw new NullReferenceException("_mapper is not initialized.");
                }

                UserMasterModel user = _mapper.Map<UserMasterModel>(updateDTO);
                var updatedUser = await _userRepo.UpdateUser(user);
                _logger.LogInformation("User with ID {UserId} updated successfully.", userId);

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", userId);

                //return Problem(ex.Message, ex.StackTrace);
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }

            return _response;
        }


        [HttpDelete("{userId:int}", Name = "DeleteUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> DeleteUserRecord(int userId)
        {
            _logger.LogInformation("DeleteUserRecord called with userId: {UserId}", userId);

            try
            {
                if (userId == 0)
                {
                    _logger.LogWarning("DeleteUserRecord failed - Invalid userId: {UserId}", userId);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid user Id.");

                    return BadRequest(_response);
                }

                var userData = await _userRepo.GetUserById(userId);

                if (userData == null)
                {
                    _logger.LogWarning("DeleteUserRecord failed - User not found: {UserId}", userId);

                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("user not found.");

                    return NotFound(_response);
                }

                var deletedUser = await _userRepo.DeleteUser( userId);
                _logger.LogInformation("User with ID {UserId} deleted successfully.", userId);

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", userId);

                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };

            }

            return _response;
        }
    }
}
