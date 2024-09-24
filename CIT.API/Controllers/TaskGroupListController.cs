using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Models.Dto.TaskGroupList;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/taskGroupList")]
    [ApiController]
    public class TaskGroupListController : Controller
    {
        private readonly ITaskGroupListRepository _taskGroupList;
        protected APIResponse _response;
        private readonly IMapper _mapper;

        public TaskGroupListController(ITaskGroupListRepository taskGroupList, IMapper mapper)
        {
            _taskGroupList = taskGroupList;
            _mapper = mapper;
            _response = new();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetAllGroupList()
        {
            try
            {
                IEnumerable<TaskGroupList> groupList = await _taskGroupList.GetAllTaskGroupList();

                if (groupList == null || !groupList.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No Task List found.");
                    return NotFound(_response);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<TaskGroupListDTO>>(groupList);

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
