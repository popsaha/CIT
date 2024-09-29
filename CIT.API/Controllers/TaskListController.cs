using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/taskList")]
    [ApiController]
    public class TaskListController : Controller
    {
        private readonly ITaskListRepository _listRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;

        public TaskListController(ITaskListRepository listRepository, IMapper mapper)
        {
            _listRepository = listRepository;
            _mapper = mapper;
            _response = new();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetAllTasks([FromQuery] TaskDateDTO request)
        {
            try
            {
                if (!DateTime.TryParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime validDate))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid date format.");
                    return BadRequest(_response);
                }
                IEnumerable<TaskList> taskList = await _listRepository.GetAllTaskList(validDate);

                if (taskList == null || !taskList.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No Task List found.");
                    return NotFound(_response);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<TaskListDTO>>(taskList);

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
    }
}
