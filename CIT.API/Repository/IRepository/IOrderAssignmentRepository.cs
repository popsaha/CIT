using CIT.API.Models.OrderAssignment;

namespace CIT.API.Repository.IRepository
{
    public interface IOrderAssignmentRepository
    {
        /// <summary>
        /// Assigns teams (Crew, Lead Vehicle, and Chase Vehicle) to orders for a specific date.
        /// </summary>
        /// <param name="assignDate">The date for which the teams need to be assigned.</param>
        /// <param name="crewIds">List of available Crew IDs.</param>
        /// <param name="leadVehicleIds">List of available Lead Vehicle IDs.</param>
        /// <param name="chaseVehicleIds">List of available Chase Vehicle IDs.</param>
        /// <returns>A list of team assignments after successful assignment.</returns>
        Task<List<TeamAssignment>> AssignTeamsToOrdersAsync(
            DateTime assignDate,
            List<int> crewIds,
            List<int> leadVehicleIds,
            List<int> chaseVehicleIds);
    }
}
