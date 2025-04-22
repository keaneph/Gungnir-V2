
using MySql.Data.MySqlClient;
using System;
using System.Data;
using sis_app.Models;

namespace sis_app.Services
{
    public class DatabaseService
    {
        public readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
            InitializeDatabase();
        }

        public void InitializeDatabase()
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            // Create colleges table
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS colleges (
                        code VARCHAR(20) PRIMARY KEY,
                        name VARCHAR(100) NOT NULL,
                        created_by VARCHAR(50),
                        created_date DATETIME DEFAULT CURRENT_TIMESTAMP
                    );";
                command.ExecuteNonQuery();
            }

            // Create programs table with foreign key
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS programs (
                        code VARCHAR(20) PRIMARY KEY,
                        name VARCHAR(100) NOT NULL,
                        college_code VARCHAR(20),
                        created_by VARCHAR(50),
                        created_date DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (college_code) REFERENCES colleges(code)
                            ON UPDATE CASCADE
                            ON DELETE SET NULL
                    );";
                command.ExecuteNonQuery();
            }

            // Create students table with foreign key
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS students (
                        id_number VARCHAR(20) PRIMARY KEY,
                        first_name VARCHAR(50) NOT NULL,
                        last_name VARCHAR(50) NOT NULL,
                        program_code VARCHAR(20),
                        year_level INT,
                        gender VARCHAR(10),
                        created_by VARCHAR(50),
                        created_date DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (program_code) REFERENCES programs(code)
                            ON UPDATE CASCADE
                            ON DELETE SET NULL
                    );";
                command.ExecuteNonQuery();
            }

            // Create users table
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS users (
                        username VARCHAR(50) PRIMARY KEY,
                        password VARCHAR(100) NOT NULL,
                        created_date DATETIME DEFAULT CURRENT_TIMESTAMP
                    );";
                command.ExecuteNonQuery();
            }
        }

        public void MigrateExistingData(
            CollegeDataService collegeService,
            ProgramDataService programService,
            StudentDataService studentService,
            UserDataService userService)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Migrate colleges
                foreach (var college in collegeService.GetAllColleges())
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "INSERT INTO colleges (code, name, created_by, created_date) VALUES (@code, @name, @user, @date)";
                    cmd.Parameters.AddWithValue("@code", college.Code);
                    cmd.Parameters.AddWithValue("@name", college.Name);
                    cmd.Parameters.AddWithValue("@user", college.User);
                    cmd.Parameters.AddWithValue("@date", college.DateTime);
                    cmd.ExecuteNonQuery();
                }

                // Migrate programs
                foreach (var program in programService.GetAllPrograms())
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "INSERT INTO programs (code, name, college_code, created_by, created_date) VALUES (@code, @name, @collegeCode, @user, @date)";
                    cmd.Parameters.AddWithValue("@code", program.Code);
                    cmd.Parameters.AddWithValue("@name", program.Name);
                    cmd.Parameters.AddWithValue("@collegeCode", program.CollegeCode);
                    cmd.Parameters.AddWithValue("@user", program.User);
                    cmd.Parameters.AddWithValue("@date", program.DateTime);
                    cmd.ExecuteNonQuery();
                }

                // Migrate students
                foreach (var student in studentService.GetAllStudents())
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO students 
                        (id_number, first_name, last_name, program_code, year_level, gender, created_by, created_date) 
                        VALUES 
                        (@id, @firstName, @lastName, @programCode, @yearLevel, @gender, @user, @date)";
                    cmd.Parameters.AddWithValue("@id", student.IDNumber);
                    cmd.Parameters.AddWithValue("@firstName", student.FirstName);
                    cmd.Parameters.AddWithValue("@lastName", student.LastName);
                    cmd.Parameters.AddWithValue("@programCode", student.ProgramCode);
                    cmd.Parameters.AddWithValue("@yearLevel", student.YearLevel);
                    cmd.Parameters.AddWithValue("@gender", student.Gender);
                    cmd.Parameters.AddWithValue("@user", student.User);
                    cmd.Parameters.AddWithValue("@date", student.DateTime);
                    cmd.ExecuteNonQuery();
                }

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
