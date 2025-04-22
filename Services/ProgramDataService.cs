using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using sis_app.Models;

namespace sis_app.Services
{
    public class ProgramDataService
    {
        public string CurrentUser { get; set; } = "Admin";

        public List<Program> GetAllPrograms()
        {
            List<Program> programs = new List<Program>();
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT code, name, college_code, created_by, created_date FROM programs";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                programs.Add(new Program
                {
                    Code = reader.GetString("code"),
                    Name = reader.GetString("name"),
                    CollegeCode = reader.IsDBNull(reader.GetOrdinal("college_code"))
                        ? "DELETED"
                        : reader.GetString("college_code"),
                    User = reader.GetString("created_by"),
                    DateTime = reader.GetDateTime("created_date")
                });
            }

            return programs;
        }

        public void AddProgram(Program program)
        {
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO programs (code, name, college_code, created_by, created_date) VALUES (@code, @name, @collegeCode, @user, @date)";
            command.Parameters.AddWithValue("@code", program.Code);
            command.Parameters.AddWithValue("@name", program.Name);
            command.Parameters.AddWithValue("@collegeCode", program.CollegeCode);
            command.Parameters.AddWithValue("@user", CurrentUser);
            command.Parameters.AddWithValue("@date", DateTime.Now);

            command.ExecuteNonQuery();
        }

        public void UpdateProgram(Program oldProgram, Program newProgram)
        {
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
            UPDATE programs 
            SET code = @newCode, 
                name = @newName,
                college_code = @collegeCode,
                created_by = @user, 
                created_date = @date 
            WHERE code = @oldCode";

                command.Parameters.AddWithValue("@newCode", newProgram.Code);
                command.Parameters.AddWithValue("@newName", newProgram.Name);
                command.Parameters.AddWithValue("@collegeCode", newProgram.CollegeCode);
                command.Parameters.AddWithValue("@user", CurrentUser);
                command.Parameters.AddWithValue("@date", DateTime.Now);
                command.Parameters.AddWithValue("@oldCode", oldProgram.Code);

                command.ExecuteNonQuery();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void DeleteProgram(Program programToDelete)
        {
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM programs WHERE code = @code";
                command.Parameters.AddWithValue("@code", programToDelete.Code);
                command.ExecuteNonQuery();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}