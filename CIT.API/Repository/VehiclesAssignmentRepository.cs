using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Repository.IRepository;
using CIT.API.Utility;
//using CIT.API.StaticMethods;

namespace CIT.API.Repository
{
    public class VehiclesAssignmentRepository : BaseRepository, IVehiclesAssignmentRepository
    {
        protected readonly APIResponse _response;
        public VehiclesAssignmentRepository(IConfiguration configuration, APIResponse response) : base(configuration)
        {
            _response = response;
        }

        public IEnumerable<VehicleAssignment> GetAllAssignOrder()
        {
            object paramObjects = new
            {
                Flag = BaseEnumType.GetAll
            };

            List<VehicleAssignment> result = ExecuteStoredProcedure<VehicleAssignment>("proc_VehicleAssignment", paramObjects);
            return result;
        }

        public List<VehicleAssignmentRequestDTO> AddAssignOrder(List<VehicleAssignmentRequestDTO> vehicleAssignRequestDTO)
        {
            var resultList = new List<VehicleAssignmentRequestDTO>();
            foreach (var vehicle in vehicleAssignRequestDTO)
            {
                try
                {
                    object paramObjects = new
                    {
                        Flag = BaseEnumType.Create,
                        @LeadID = vehicle.LeadID,
                        @ChaseID = vehicle.ChaseID,
                        @CrewCommanderID = vehicle.CrewCommanderID,
                        @VehicleAssignDate = vehicle.VehicleAssignDate
                    };

                    ExecuteStoredProcedure<object>("proc_VehicleAssignment", paramObjects);
                    resultList.Add(vehicle);
                }
                catch (Exception ex)
                {
                    return resultList;
                }
            }
            return resultList;
        }
    }
}
