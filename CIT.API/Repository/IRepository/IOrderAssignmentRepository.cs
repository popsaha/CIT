namespace CIT.API.Repository.IRepository
{
    public interface IOrderAssignmentRepository
    {
        Task<int> AssignTeamsRandomly(DateTime assignDate, List<int> crewIds, List<int> leadVehicleIds, List<int> chaseVehicleIds);
    }
}
