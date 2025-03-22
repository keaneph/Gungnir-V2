using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using sis_app.Models;

namespace sis_app.Services
{
    // service class to handle all program data operations (crud operations)
    public class ProgramDataService
    {
        // path to the csv file storing program data
        internal readonly string _filePath;

        // current user performing operations (defaults to "Admin")
        public string CurrentUser { get; set; } = "Admin";

        // constructor initializes service with file path and ensures data directory exists
        public ProgramDataService(string fileName)
        {
            // create path to Data folder in project directory
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = Path.Combine(baseDirectory, "..\\..\\..\\");
            string dataDirectory = Path.Combine(projectDirectory, "Data");

            // create Data directory if it doesn't exist
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            // set full path to csv file
            _filePath = Path.Combine(dataDirectory, fileName);

            // create empty file if it doesn't exist
            if (!File.Exists(_filePath))
            {
                File.Create(_filePath).Close();
            }
        }

        // retrieves all academic programs from the csv file
        public List<Program> GetAllPrograms()
        {
            List<Program> programs = new List<Program>();

            try
            {
                // read each line and convert to program object
                var lines = File.ReadAllLines(_filePath);
                foreach (string line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    programs.Add(Program.FromCsv(line));
                }
            }
            catch (Exception ex)
            {
                // log error but don't crash the application
                Console.WriteLine($"Error reading programs data: {ex.Message}");
            }

            return programs;
        }

        // adds a new academic program to the csv file
        public void AddProgram(Program program)
        {
            try
            {
                // using statement ensures proper resource disposal
                using (StreamWriter sw = File.AppendText(_filePath))
                {
                    sw.WriteLine(program.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding program: {ex.Message}");
                throw; // rethrow to notify caller of failure
            }
        }

        // updates existing program information
        public void UpdateProgram(Program oldProgram, Program newProgram)
        {
            // get current list of programs
            List<Program> programs = GetAllPrograms();
            bool replaced = false;

            try
            {
                // rewrite entire file with updated program data
                using (StreamWriter sw = new StreamWriter(_filePath))
                {
                    foreach (Program program in programs)
                    {
                        // if match found, write new program data
                        if (program.Name == oldProgram.Name && program.Code == oldProgram.Code)
                        {
                            // update audit fields
                            newProgram.DateTime = DateTime.Now;
                            newProgram.User = CurrentUser;
                            sw.WriteLine(newProgram.ToString());
                            replaced = true;
                        }
                        else
                        {
                            // keep existing program data
                            sw.WriteLine(program.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating program: {ex.Message}");
                throw;
            }

            // log if program wasn't found
            if (!replaced)
            {
                Console.WriteLine($"Program to update not found: {oldProgram.Name} - {oldProgram.Code}");
            }
        }

        // removes a program from the csv file
        public void DeleteProgram(Program programToDelete)
        {
            // get current list of programs
            List<Program> programs = GetAllPrograms();
            bool removed = false;

            try
            {
                // rewrite file excluding deleted program
                using (StreamWriter sw = new StreamWriter(_filePath))
                {
                    foreach (Program program in programs)
                    {
                        // skip the program to be deleted
                        if (program.Name == programToDelete.Name && program.Code == programToDelete.Code)
                        {
                            removed = true;
                        }
                        else
                        {
                            // keep other programs
                            sw.WriteLine(program.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting program: {ex.Message}");
                throw;
            }

            // log if program wasn't found
            if (!removed)
            {
                Console.WriteLine($"Program to delete not found: {programToDelete.Name} - {programToDelete.Code}");
            }
        }
    }
}