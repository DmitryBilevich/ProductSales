using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace ProductSalesApi.Services
{
    public class JsonStoredProcedureService
    {
        private readonly string _connectionString;

        public JsonStoredProcedureService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        /// <summary>
        /// Executes a stored procedure with JSON input and returns JSON output
        /// </summary>
        /// <param name="procedureName">Name of the stored procedure</param>
        /// <param name="jsonInput">JSON string input parameter</param>
        /// <returns>JSON string result</returns>
        public async Task<string> ExecuteJsonProcedureAsync(string procedureName, string jsonInput)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add JSON input parameter
            command.Parameters.Add(new SqlParameter("@JsonInput", SqlDbType.NVarChar, -1)
            {
                Value = jsonInput ?? "{}"
            });

            await connection.OpenAsync();

            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? "{}";
        }

        /// <summary>
        /// Executes a stored procedure with JSON input and returns JSON output with additional output parameter
        /// </summary>
        /// <param name="procedureName">Name of the stored procedure</param>
        /// <param name="jsonInput">JSON string input parameter</param>
        /// <returns>Tuple of JSON result and output parameter value</returns>
        public async Task<(string JsonResult, object OutputParam)> ExecuteJsonProcedureWithOutputAsync(
            string procedureName,
            string jsonInput,
            string outputParamName = "@OutputJson")
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add JSON input parameter
            command.Parameters.Add(new SqlParameter("@JsonInput", SqlDbType.NVarChar, -1)
            {
                Value = jsonInput ?? "{}"
            });

            // Add output parameter
            var outputParam = new SqlParameter(outputParamName, SqlDbType.NVarChar, -1)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(outputParam);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            return (outputParam.Value?.ToString() ?? "{}", outputParam.Value);
        }

        /// <summary>
        /// Executes a stored procedure that returns a dataset as JSON
        /// </summary>
        /// <param name="procedureName">Name of the stored procedure</param>
        /// <param name="jsonInput">JSON string input parameter</param>
        /// <returns>JSON string result</returns>
        public async Task<string> ExecuteJsonQueryAsync(string procedureName, string jsonInput)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@JsonInput", SqlDbType.NVarChar, -1)
            {
                Value = jsonInput ?? "{}"
            });

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetString(0); // First column should contain JSON result
            }

            return "{}";
        }
    }
}