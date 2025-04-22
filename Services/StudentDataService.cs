using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using sis_app.Models;

namespace sis_app.Services
{
    public class StudentDataService
    {
        public string CurrentUser { get; set; } = "Admin";

        public List<Student> GetAllStudents()
        {
            List<Student> students = new List<Student>();
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT s.id_number, s.first_name, s.last_name, s.year_level, s.gender, 
                       s.program_code, p.college_code, s.created_by, s.created_date 
                FROM students s
                LEFT JOIN programs p ON s.program_code = p.code";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                students.Add(new Student
                {
                    IDNumber = reader.GetString("id_number"),
                    FirstName = reader.GetString("first_name"),
                    LastName = reader.GetString("last_name"),
                    YearLevel = reader.GetInt32("year_level"),
                    Gender = reader.GetString("gender"),
                    ProgramCode = reader.GetString("program_code"),
                    CollegeCode = !reader.IsDBNull(reader.GetOrdinal("college_code")) ? reader.GetString("college_code") : "DELETED",
                    User = reader.GetString("created_by"),
                    DateTime = reader.GetDateTime("created_date")
                });
            }

            return students;
        }

        public void AddStudent(Student student)
        {
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO students 
                (id_number, first_name, last_name, program_code, year_level, gender, created_by, created_date)
                VALUES 
                (@id, @firstName, @lastName, @programCode, @yearLevel, @gender, @user, @date)";

            command.Parameters.AddWithValue("@id", student.IDNumber);
            command.Parameters.AddWithValue("@firstName", student.FirstName);
            command.Parameters.AddWithValue("@lastName", student.LastName);
            command.Parameters.AddWithValue("@programCode", student.ProgramCode);
            command.Parameters.AddWithValue("@yearLevel", student.YearLevel);
            command.Parameters.AddWithValue("@gender", student.Gender);
            command.Parameters.AddWithValue("@user", student.User);
            command.Parameters.AddWithValue("@date", student.DateTime);

            command.ExecuteNonQuery();
        }

        public void UpdateStudent(Student oldStudent, Student newStudent)
        {
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE students 
                SET first_name = @firstName,
                    last_name = @lastName,
                    program_code = @programCode,
                    year_level = @yearLevel,
                    gender = @gender,
                    created_by = @user,
                    created_date = @date
                WHERE id_number = @oldId";

            command.Parameters.AddWithValue("@firstName", newStudent.FirstName);
            command.Parameters.AddWithValue("@lastName", newStudent.LastName);
            command.Parameters.AddWithValue("@programCode", newStudent.ProgramCode);
            command.Parameters.AddWithValue("@yearLevel", newStudent.YearLevel);
            command.Parameters.AddWithValue("@gender", newStudent.Gender);
            command.Parameters.AddWithValue("@user", newStudent.User);
            command.Parameters.AddWithValue("@date", newStudent.DateTime);
            command.Parameters.AddWithValue("@oldId", oldStudent.IDNumber);

            command.ExecuteNonQuery();
        }

        public void DeleteStudent(Student studentToDelete)
        {
            using var connection = new MySqlConnection(App.DatabaseService._connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM students WHERE id_number = @id";
            command.Parameters.AddWithValue("@id", studentToDelete.IDNumber);

            command.ExecuteNonQuery();
        }
    }
}