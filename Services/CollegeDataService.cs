using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using sis_app.Models;

namespace sis_app.Services
{
    public class CollegeDataService
    {
        public string CurrentUser { get; set; } = "Admin";

        public List<College> GetAllColleges()
        {
            List<College> colleges = new List<College>();
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT code, name, created_by, created_date FROM colleges";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                colleges.Add(new College
                {
                    Code = reader.GetString("code"),
                    Name = reader.GetString("name"),
                    User = reader.GetString("created_by"),
                    DateTime = reader.GetDateTime("created_date")
                });
            }

            return colleges;
        }

        public void AddCollege(College college)
        {
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO colleges (code, name, created_by, created_date) VALUES (@code, @name, @user, @date)";
            command.Parameters.AddWithValue("@code", college.Code);
            command.Parameters.AddWithValue("@name", college.Name);
            command.Parameters.AddWithValue("@user", CurrentUser);
            command.Parameters.AddWithValue("@date", DateTime.Now);

            command.ExecuteNonQuery();
        }

        public void UpdateCollege(College oldCollege, College newCollege)
        {
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = connection.CreateCommand();
                // Use a direct UPDATE without trying to handle the programs separately
                command.CommandText = @"
            UPDATE colleges 
            SET code = @newCode, 
                name = @newName, 
                created_by = @user, 
                created_date = @date 
            WHERE code = @oldCode";

                command.Parameters.AddWithValue("@newCode", newCollege.Code);
                command.Parameters.AddWithValue("@newName", newCollege.Name);
                command.Parameters.AddWithValue("@user", CurrentUser);
                command.Parameters.AddWithValue("@date", DateTime.Now);
                command.Parameters.AddWithValue("@oldCode", oldCollege.Code);

                command.ExecuteNonQuery();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void DeleteCollege(College collegeToDelete)
        {
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM colleges WHERE code = @code";
            command.Parameters.AddWithValue("@code", collegeToDelete.Code);

            command.ExecuteNonQuery();
        }
    }
}