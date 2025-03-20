using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.Customer;
using CIT.API.Models.Dto.User;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace CIT.API.Controllers
{
    [Route("api/LocalUser")]
    [ApiController]
    public class LocalUserController : ControllerBase
    {
        private readonly ILocalUserRepository _userRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        public LocalUserController(ILocalUserRepository userRepository, IMapper mapper)
        {
            _userRepository=userRepository;
            _mapper=mapper;
            _response = new();
        }

        [HttpGet]
        //[Authorize]
        //[HttpGet("GetCustomers")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetAllUser()
        {
            try
            {
                IEnumerable<User> userList = await _userRepository.GetAllUsers();

                if (userList == null || !userList.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No User found.");
                    return NotFound(_response);
                }

                _response.Result = _mapper.Map<List<LocalUserDTO>>(userList);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                

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

        [HttpGet("{id:int}", Name = "GetUser")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetUser(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var user = await _userRepository.GetUser(id);

                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }

                //_response.Result = user;
                _response.Result = _mapper.Map<LocalUserDTO>(user);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [HttpPost("CreateLocalUser")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CreateUser([FromBody] LocalUserCreateDTO createDTO)
        {
            int Res = 0;
            try
            {

                if (createDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid User data.");
                    return BadRequest(_response);
                }

                // Get the userId from the claims (JWT token)
                //var userId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var user = _mapper.Map<LocalUserCreateDTO>(createDTO);

                 Res = await _userRepository.AddUser(createDTO);

                // Call the repository method to create the user
                //var createdUser = await _userRepository.AddUser(createDTO);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = user;
                return Ok(_response);
                          
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }

            return _response;
        }

        [HttpPut("{userId:int}", Name = "UpdateLocalUser")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateLocalUser(int userId, [FromBody] LocalUserUpdateDTO updateDTO)
        {

            try
            {
                if (updateDTO == null || userId != updateDTO.UserId)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid User Id.");

                    return BadRequest(_response);
                }

                User user = _mapper.Map<User>(updateDTO);
                var updatedCustomer = await _userRepository.UpdateUser(user);

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                //return Problem(ex.Message, ex.StackTrace);
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }

            return _response;
        }

        [HttpDelete("{userId:int}", Name = "DeleteLocalUser")]
        //[Authorize(Roles = "CUSTOM")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> DeleteCustomer(int userId,int deletedBy)
        {
            try
            {
                if (userId == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid User Id.");

                    return BadRequest(_response);
                }

                var user = await _userRepository.GetUser(userId);

                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("user not found.");

                    return NotFound(_response);
                }

                var deletedUser = await _userRepository.DeleteUser(userId, deletedBy);

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };

            }

            return _response;
        }
    }
}
