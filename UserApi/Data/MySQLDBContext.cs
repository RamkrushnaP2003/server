using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace UserApi.Data
{
    public class MySQLDBContext
    {
        private readonly string _connectionString;
        public MySQLDBContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySQLConnection");

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new ArgumentNullException(nameof(_connectionString), "MySQL connection string is not configured properly.");
            }
        }

        // Method to create and return a MySQL connection
        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        // Method to execute stored procedures
        public async Task<int> ExecuteStoredProcedureAsync(string storedProcedureName, MySqlParameter[] parameters)
        {
            using (var connection = CreateConnection())
            {
                using (var command = new MySqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddRange(parameters);

                    connection.Open();
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
