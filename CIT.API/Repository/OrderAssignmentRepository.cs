using AutoMapper;
using CIT.API.Context;
using CIT.API.Models.OrderAssignment;
using CIT.API.Repository.IRepository;
using Dapper;

namespace CIT.API.Repository
{
    public class OrderAssignmentRepository : IOrderAssignmentRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        private readonly Random _random = new Random();

        public OrderAssignmentRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }

        public async Task<List<TeamAssignment>> AssignTeamsToOrdersAsync(
            DateTime assignDate,
            List<int> crewIds,
            List<int> leadVehicleIds,
            List<int> chaseVehicleIds)
        {
            using var connection = _db.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var nextDay = assignDate.AddDays(1).ToString("yyyy-MM-dd");

                // Fetch orders not in TeamAssignment
                var ordersQuery = @"
                    SELECT o.OrderId, o.OrderRouteId, o.IsFullDayAssignment
                    FROM Orders o
                    LEFT JOIN TeamAssignments ta ON o.OrderId = ta.OrderId
                    WHERE o.OrderDate = @AssignDate AND ta.OrderId IS NULL;";
                var orders = (await connection.QueryAsync<OrderRoutes>(ordersQuery, new { AssignDate = nextDay }, transaction)).ToList();

                if (!orders.Any())
                    return new List<TeamAssignment>(); // No orders to assign

                var teamAssignments = new List<TeamAssignment>();
                var routeAssignments = new Dictionary<int, (int CrewId, int LeadVehicleId, int ChaseVehicleId)>();
                var random = new Random();

                foreach (var order in orders)
                {
                    if (order.IsFullDayAssignment == 1)
                    {
                        // Full-day assignment: Assign unique random IDs
                        if (!crewIds.Any() || !leadVehicleIds.Any() || !chaseVehicleIds.Any())
                            throw new Exception("Insufficient IDs available for full-day assignment.");

                        var crewId = crewIds[random.Next(crewIds.Count)];
                        var leadVehicleId = leadVehicleIds[random.Next(leadVehicleIds.Count)];
                        var chaseVehicleId = chaseVehicleIds[random.Next(chaseVehicleIds.Count)];

                        crewIds.Remove(crewId);
                        leadVehicleIds.Remove(leadVehicleId);
                        chaseVehicleIds.Remove(chaseVehicleId);

                        teamAssignments.Add(new TeamAssignment
                        {
                            OrderId = order.OrderId,
                            CrewId = crewId,
                            LeadVehicleId = leadVehicleId,
                            ChaseVehicleId = chaseVehicleId
                        });
                    }
                    else
                    {
                        // Non-full-day assignment: Check OrderRouteId
                        if (!routeAssignments.TryGetValue(order.OrderRouteId, out var existingAssignment))
                        {
                            // New route, assign unique random IDs
                            if (!crewIds.Any() || !leadVehicleIds.Any() || !chaseVehicleIds.Any())
                                throw new Exception("Insufficient IDs available for non-full-day assignment.");

                            var crewId = crewIds[random.Next(crewIds.Count)];
                            var leadVehicleId = leadVehicleIds[random.Next(leadVehicleIds.Count)];
                            var chaseVehicleId = chaseVehicleIds[random.Next(chaseVehicleIds.Count)];

                            crewIds.Remove(crewId);
                            leadVehicleIds.Remove(leadVehicleId);
                            chaseVehicleIds.Remove(chaseVehicleId);

                            routeAssignments[order.OrderRouteId] = (crewId, leadVehicleId, chaseVehicleId);

                            teamAssignments.Add(new TeamAssignment
                            {
                                OrderId = order.OrderId,
                                CrewId = crewId,
                                LeadVehicleId = leadVehicleId,
                                ChaseVehicleId = chaseVehicleId
                            });
                        }
                        else
                        {
                            // Reuse existing assignment for this OrderRouteId
                            teamAssignments.Add(new TeamAssignment
                            {
                                OrderId = order.OrderId,
                                CrewId = existingAssignment.CrewId,
                                LeadVehicleId = existingAssignment.LeadVehicleId,
                                ChaseVehicleId = existingAssignment.ChaseVehicleId
                            });
                        }
                    }
                }

                // Insert the team assignments
                var insertQuery = @"
                    INSERT INTO TeamAssignments (OrderId, CrewId, LeadVehicleId, ChaseVehicleId)
                    VALUES (@OrderId, @CrewId, @LeadVehicleId, @ChaseVehicleId);";
                await connection.ExecuteAsync(insertQuery, teamAssignments, transaction);

                // Update Task table TaskStatusId to 2 for related OrderIds
                var orderIds = teamAssignments.Select(a => a.OrderId).ToList();
                var updateQuery = @"
                    UPDATE Task
                    SET TaskStatusId = 2
                    WHERE OrderId IN @OrderIds AND TaskDate = @TaskDate;";
                await connection.ExecuteAsync(updateQuery, new { OrderIds = orderIds, TaskDate = nextDay }, transaction);

                transaction.Commit();
                return teamAssignments;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }




        private int GetRandomId(List<int> ids)
        {
            return ids[_random.Next(ids.Count)];
        }

        private TeamCombination GetRandomATMCombination(IEnumerable<TeamCombination> combinations)
        {
            var combinationList = combinations.ToList();
            if (!combinationList.Any())
                throw new InvalidOperationException("No valid ATM team combination found for the date");
            return combinationList[_random.Next(combinationList.Count)];
        }


    }
}
