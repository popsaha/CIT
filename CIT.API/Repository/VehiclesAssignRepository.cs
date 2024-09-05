using CIT.API.Models;
using CIT.API.Repository.IRepository;
using CIT.API.StaticMethods;

namespace CIT.API.Repository
{
    public class VehiclesAssignRepository : BaseRepository, IVehiclesAssignRepository
    {
        public VehiclesAssignRepository(IConfiguration configuration) : base(configuration)
        {
        }

        public IEnumerable<VehicleAssignModel> GetGetAllAssignOrder()
        {
            object paramObjects = new
            {
                Flag = strctCRUDAction.GetAll
            };

            List<VehicleAssignModel> result = ExecuteStoredProcedure<VehicleAssignModel>("proc_VehicleAssignment", paramObjects);
            return result;
        }

        public CommonResponseModel AddAssignOrder(VehicleAssignRequestModel vehicleAssignRequestDTO)
        {
            try
            {
                object paramObjects = new
                {
                    Flag = strctCRUDAction.Create,
                    @GroupId = vehicleAssignRequestDTO.GroupID,
                    @VehicleID = vehicleAssignRequestDTO.VehicleID,
                    @CrewCommanderID = vehicleAssignRequestDTO.CrewCommanderID,
                    @PoliceID = vehicleAssignRequestDTO.PoliceID,
                    @TaskID = vehicleAssignRequestDTO.TaskID
                };

                ExecuteStoredProcedure<object>("proc_VehicleAssignment", paramObjects);
                return CommonResponse.Success("Data added successfully");
            }
            catch (Exception ex)
            {
                return CommonResponse.Error($"{ex.Message}");
            }
        }

        public IEnumerable<TaskGroupModel> GetAllTaskGroup()
        {
            object paramObjects = new
            {
                Flag = strctCRUDAction.GetAll
            };
            List<TaskGroupModel> result = ExecuteStoredProcedure<TaskGroupModel>("cit.spTaskGroup", paramObjects);
            return result;
        }

        public CommonResponseModel AddTaskGroup(TaskGroupRequestModel taskGroupRequestModel)
        {
            try
            {
                object paramObjects = new
                {
                    Flag = strctCRUDAction.Create,
                    @GroupName = taskGroupRequestModel.GroupName,
                    @TaskId = taskGroupRequestModel.TaskID,
                    @TaskDate = DateTime.Now
                };
                ExecuteStoredProcedure<object>("cit.spTaskGroup", paramObjects);
                return CommonResponse.Success("Data added successfully");
            }
            catch (Exception ex)
            {
                return CommonResponse.Error($"{ex.Message}");
            }
        }

        public CommonResponseModel UpdateTaskGroup(TaskGroupModel taskGroupModel)
        {
            try
            {
                object paramObjects = new
                {
                    Flag = strctCRUDAction.Update,
                    @ID = taskGroupModel.Id,
                    @GroupName = taskGroupModel.GroupName,
                    @TaskId = taskGroupModel.TaskId,
                    @TaskDate = DateTime.Now
                };

                ExecuteStoredProcedure<object>("cit.spTaskGroup", paramObjects);
                return CommonResponse.Success($"Data Updated successfully for ID : {taskGroupModel.Id}");
            }
            catch (Exception ex)
            {
                return CommonResponse.Error($"{ex.Message}");
            }
        }

        public CommonResponseModel DeleteTaskGroup(int id)
        {
            try
            {
                object paramObjects = new
                {
                    Flag = strctCRUDAction.Delete,
                    @ID = id
                };

                ExecuteStoredProcedure<object>("cit.spTaskGroup", paramObjects);
                return CommonResponse.Success($"Data Deleted for ID : {id}");
            }
            catch (Exception ex)
            {
                return CommonResponse.Error($"{ex.Message}");
            }
        }
    }
}
