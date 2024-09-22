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

        public List<VehicleAssignmentRequestDTO> AddAssignOrder(VehicleAssignmentRequestDTO vehicleAssignRequestDTO)
        {
            var resultList = new List<VehicleAssignmentRequestDTO>();

            try
            {
                if (vehicleAssignRequestDTO.LeadID != null && vehicleAssignRequestDTO.ChaseID != null && vehicleAssignRequestDTO.CrewCommanderID != null)
                {
                    for (int i = 0; i < vehicleAssignRequestDTO.LeadID.Count; i++)
                    {
                        object paramObjects = new
                        {
                            Flag = BaseEnumType.Create,
                            @LeadID = vehicleAssignRequestDTO.LeadID[i],
                            @ChaseID = vehicleAssignRequestDTO.ChaseID[i],
                            @CrewCommanderID = vehicleAssignRequestDTO.CrewCommanderID[i],
                        };

                        ExecuteStoredProcedure<object>("proc_VehicleAssignment", paramObjects);

                        resultList.Add(new VehicleAssignmentRequestDTO
                        {
                            LeadID = new List<int> { vehicleAssignRequestDTO.LeadID[i] },
                            ChaseID = new List<int> { vehicleAssignRequestDTO.ChaseID[i] },
                            CrewCommanderID = new List<int> { vehicleAssignRequestDTO.CrewCommanderID[i] },
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return resultList;
        }


        public List<string> ValidateVehicleAssignmentRequest(VehicleAssignmentRequestDTO vehicleAssignRequestDTO)
        {
            var errors = new List<string>();

            ValidateIdList(vehicleAssignRequestDTO.LeadID, "LeadID", errors);
            ValidateIdList(vehicleAssignRequestDTO.ChaseID, "ChaseID", errors);
            ValidateIdList(vehicleAssignRequestDTO.CrewCommanderID, "CrewCommanderID", errors);

            return errors;
        }

        private void ValidateIdList(List<int>? idList, string fieldName, List<string> errors)
        {
            if (idList == null || !idList.Any())
            {
                errors.Add($"{fieldName} is required and should contain at least one valid ID.");
            }
            else if (idList.Any(id => id <= 0))
            {
                errors.Add($"All {fieldName} values should be positive integers.");
            }
        }
    }
}
