using CIT.API.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CIT.API.Utility
{
    public class BaseRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _dbContext;

        public BaseRepository(IConfiguration configuration)
        {
            _dbContext = configuration.GetConnectionString("SqlConnection");
        }

        protected virtual List<T> ExecuteStoredProcedure<T>(string spName, object parameters) where T : new()
        {
            List<T> results = new List<T>();

            using (SqlConnection connection = new SqlConnection(_dbContext))
            {
                using (SqlCommand command = new SqlCommand(spName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    if (parameters != null)
                    {
                        foreach (var param in parameters.GetType().GetProperties())
                        {
                            command.Parameters.AddWithValue(param.Name, param.GetValue(parameters));
                        }
                    }
                    connection.Open();
                    if (typeof(T) == typeof(object))
                    {
                        command.ExecuteNonQuery();
                        return results;
                    }
                    else
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                T result = new T();

                                foreach (var prop in typeof(T).GetProperties())
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal(prop.Name)))
                                    {
                                        object value = reader[prop.Name];
                                        prop.SetValue(result, Convert.ChangeType(value, prop.PropertyType));
                                    }
                                }

                                results.Add(result);
                            }
                        }
                    }
                }
            }
            return results;
        }
    }
}
