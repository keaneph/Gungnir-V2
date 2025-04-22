using MySql.Data.MySqlClient;
using System;

namespace sis_app.Services
{
    public class UserDataService
    {
        public bool ValidateUser(string username, string password)
        {
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM users WHERE username = @username AND password = @password";
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", password);

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        public bool RegisterUser(string username, string password)
        {
            try
            {
                using var connection = new MySqlConnection(App.DatabaseService._connectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO users (username, password) VALUES (@username, @password)";
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", password);

                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UsernameExists(string username)
        {
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM users WHERE username = @username";
            command.Parameters.AddWithValue("@username", username);

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
    }
}