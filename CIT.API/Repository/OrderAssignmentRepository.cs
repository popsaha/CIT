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

        public async Task<int> AssignTeamsRandomly(DateTime assignDate, List<int> crewIds, List<int> leadVehicleIds, List<int> chaseVehicleIds)
        {
            if (!crewIds.Any() || !leadVehicleIds.Any() || !chaseVehicleIds.Any())
            {
                throw new InvalidOperationException("Crew IDs, Lead Vehicle IDs, and Chase Vehicle IDs cannot be empty");
            }
            using (var connection = _db.CreateConnection())
            {

                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var nextDay = assignDate.AddDays(1).ToString("yyyy-MM-dd");

                    // Query to get the orders with FullDayOccupancy info
                    var ordersQuery = @"SELECT OrderId, OrderRouteId, IsFullDayAssignment
                                FROM Orders
                                WHERE OrderDate = @NextDay;";
                    var orders = await connection.QueryAsync<OrderRoutes>(ordersQuery, new { NextDay = nextDay }, transaction);

                    // Get ATM team combinations
                    //var atmCombinationsQuery = @"SELECT CrewId, LeadVehicleId 
                    //                         FROM ATMTeamCombinations 
                    //                         WHERE @Date BETWEEN StartDate AND EndDate";
                    //var atmCombinations = await connection.QueryAsync<ATMTeamCombination>(atmCombinationsQuery, new { Date = nextDay }, transaction);

                    // Track which routes already have team assignments
                    var assignedRoutes = new Dictionary<int, int>(); // Dictionary to store OrderRouteId and TeamAssignmentId
                    var assignments = new List<TeamAssignment>();
                    int teamAssignmentId = 1; // Start assigning team IDs from 1

                    foreach (var order in orders)
                    {
                        int crewId, leadVehicleId, chaseVehicleId;

                        if (order.IsFullDayAssignment)
                        {
                            // Full day orders get unique teams
                            crewId = GetRandomId(crewIds);
                            leadVehicleId = GetRandomId(leadVehicleIds);
                            chaseVehicleId = GetRandomId(chaseVehicleIds);

                            assignments.Add(new TeamAssignment
                            {
                                OrderId = order.OrderId,
                                TeamAssignmentId = teamAssignmentId++,
                                CrewId = crewId,
                                LeadVehicleId = leadVehicleId,
                                ChaseVehicleId = chaseVehicleId
                            });
                        }
                        else
                        {
                            // For non-full-day orders, check if a team is already assigned to this route
                            if (!assignedRoutes.TryGetValue(order.OrderRouteId, out var existingTeamId))
                            {
                                // Assign a new team for this route
                                crewId = GetRandomId(crewIds);
                                leadVehicleId = GetRandomId(leadVehicleIds);
                                chaseVehicleId = GetRandomId(chaseVehicleIds);

                                existingTeamId = teamAssignmentId++;
                                assignedRoutes[order.OrderRouteId] = existingTeamId;

                                // Store the new assignment
                                assignments.Add(new TeamAssignment
                                {
                                    OrderId = order.OrderId,
                                    TeamAssignmentId = existingTeamId,
                                    CrewId = crewId,
                                    LeadVehicleId = leadVehicleId,
                                    ChaseVehicleId = chaseVehicleId
                                });
                            }
                            else
                            {
                                // Get the team already assigned for this route
                                var existingTeamAssignment = assignments.First(a => a.TeamAssignmentId == existingTeamId);

                                crewId = existingTeamAssignment.CrewId;
                                leadVehicleId = existingTeamAssignment.LeadVehicleId;
                                chaseVehicleId = existingTeamAssignment.ChaseVehicleId;

                                // Assign the existing team for this route
                                assignments.Add(new TeamAssignment
                                {
                                    OrderId = order.OrderId,
                                    TeamAssignmentId = existingTeamId,
                                    CrewId = crewId,
                                    LeadVehicleId = leadVehicleId,
                                    ChaseVehicleId = chaseVehicleId
                                });
                            }
                        }

                    }

                    // Insert assignments into database
                    var insertQuery = @"INSERT INTO TeamAssignments (OrderId, CrewId, LeadVehicleId, ChaseVehicleId) 
                                VALUES (@OrderId, @CrewId, @LeadVehicleId, @ChaseVehicleId)";
                    var rowsAffected = await connection.ExecuteAsync(insertQuery, assignments, transaction);

                    // Update Task table TaskStatusId to 2 for the related OrderIds
                    var updateTaskStatusQuery = @"UPDATE Task
                                          SET TaskStatusId = 2
                                          WHERE OrderId IN @OrderIds";
                    var orderIds = assignments.Select(a => a.OrderId).Distinct().ToList();
                    await connection.ExecuteAsync(updateTaskStatusQuery, new { OrderIds = orderIds }, transaction);

                    transaction.Commit();
                    return rowsAffected;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("An error occurred during team assignment.", ex);
                }

            }
        }

        private int GetRandomId(List<int> ids)
        {
            if (!ids.Any()) throw new InvalidOperationException("No IDs available for assignment");
            return ids[_random.Next(ids.Count)];
        }

        private ATMTeamCombination GetRandomATMCombination(IEnumerable<ATMTeamCombination> combinations)
        {
            var combinationList = combinations.ToList();
            if (!combinationList.Any())
                throw new InvalidOperationException("No valid ATM team combination found for the date");
            return combinationList[_random.Next(combinationList.Count)];
        }


    }
}
