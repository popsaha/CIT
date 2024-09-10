using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace CIT.API.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]
    public class VehicleAssignmentController : Controller
    {
        private readonly IServiceProvider _serviceProvider;

        public VehicleAssignmentController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        [Route("GetAllAssignOrder")]
        public IActionResult GetAllAssignOrder()
        {
            try
            {
                var orderAssignService = _serviceProvider.GetRequiredService<IVehiclesAssignmentRepository>();
                return Ok(orderAssignService.GetAllAssignOrder());
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }

        //[HttpPost]
        //[Route("AddAssignOrder")]
        //public IActionResult AddOrderDistribute(VehicleAssignmentRequestDTO vehicleAssignRequestDTO)
        //{
        //    try
        //    {
        //        var orderAssignService = _serviceProvider.GetRequiredService<IVehiclesAssignmentRepository>();
        //        return Ok(orderAssignService.AddAssignOrder(vehicleAssignRequestDTO));
        //    }
        //    catch (Exception ex)
        //    {
        //        return Problem(ex.Message, ex.StackTrace);
        //    }

        //}

        //[HttpGet]
        //[Route("GetAllTaskGroup")]
        //public IActionResult GetAllTaskGroup()
        //{
        //    try
        //    {
        //        var orderAssignService = _serviceProvider.GetRequiredService<IVehiclesAssignmentRepository>();
        //        return Ok(orderAssignService.GetAllTaskGroup());
        //    }
        //    catch (Exception ex)
        //    {
        //        return Problem(ex.Message, ex.StackTrace);
        //    }
        //}

        //[HttpPost]
        //[Route("AddTaskGroup")]
        //public IActionResult AddTaskGroup(TaskGroupRequestModel taskGroupRequestModel)
        //{
        //    try
        //    {
        //        var orderAssignService = _serviceProvider.GetRequiredService<IVehiclesAssignmentRepository>();
        //        return Ok(orderAssignService.AddTaskGroup(taskGroupRequestModel));
        //    }
        //    catch (Exception ex)
        //    {
        //        return Problem(ex.Message, ex.StackTrace);
        //    }
        //}

        //[HttpPut]
        //[Route("UpdateTaskGroup")]
        //public IActionResult UpdateTaskGroup(TaskGrouping taskGroupModel)
        //{
        //    try
        //    {
        //        var orderAssignService = _serviceProvider.GetRequiredService<IVehiclesAssignmentRepository>();
        //        return Ok(orderAssignService.UpdateTaskGroup(taskGroupModel));
        //    }
        //    catch (Exception ex)
        //    {
        //        return Problem(ex.Message, ex.StackTrace);
        //    }
        //}

        //[HttpDelete]
        //[Route("DeleteTaskGroup")]
        //public IActionResult DeleteTaskGroup(int id)
        //{
        //    try
        //    {
        //        var orderAssignService = _serviceProvider.GetRequiredService<IVehiclesAssignmentRepository>();
        //        return Ok(orderAssignService.DeleteTaskGroup(id));
        //    }
        //    catch (Exception ex)
        //    {
        //        return Problem(ex.Message, ex.StackTrace);
        //    }
        //}
    }
}
