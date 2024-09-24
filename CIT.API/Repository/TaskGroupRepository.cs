using CIT.API.Models.Dto;
using CIT.API.Models;
using CIT.API.Repository.IRepository;
using CIT.API.Utility;

namespace CIT.API.Repository
{
    public class TaskGroupRepository : BaseRepository, ITaskGroupRepository
    {
        protected readonly APIResponse _response;
        public TaskGroupRepository(IConfiguration configuration, APIResponse response) : base(configuration)
        {
            _response = response;
        }
        public IEnumerable<TaskGrouping> GetAllTaskGroup()
        {
            object paramObjects = new
            {
                Flag = BaseEnumType.GetAll
            };
            List<TaskGrouping> result = ExecuteStoredProcedure<TaskGrouping>("cit.spTaskGroup", paramObjects);
            return result;
        }

        public List<TaskGroupingRequestDTO> AddTaskGroups(List<TaskGroupingRequestDTO> taskGroupingRequestDTOs)
        {
            var resultList = new List<TaskGroupingRequestDTO>();

            foreach (var taskGroupingRequestDTO in taskGroupingRequestDTOs)
            {
                try
                {
                    object paramObjects = new
                    {
                        Flag = BaseEnumType.Create,
                        @GroupName = taskGroupingRequestDTO.GroupName,
                        @TaskId = taskGroupingRequestDTO.TaskID,
                        @TaskDate = taskGroupingRequestDTO.TaskDate,
                    };
                    ExecuteStoredProcedure<object>("cit.spTaskGroup", paramObjects);
                    resultList.Add(taskGroupingRequestDTO);
                }
                catch (Exception ex)
                {
                    return resultList;
                }
            }
            return resultList;
        }

        public bool DeleteTaskGroup(int id)
        {
            try
            {
                object paramObjects = new
                {
                    Flag = BaseEnumType.Delete,
                    @ID = id
                };

                ExecuteStoredProcedure<object>("cit.spTaskGroup", paramObjects);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
