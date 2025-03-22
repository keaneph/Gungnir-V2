using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using sis_app.Models;

namespace sis_app.Services
{
    // service class to handle all college data operations (crud operations)
    public class CollegeDataService
    {
        // path to the csv file storing college data
        internal readonly string _filePath;

        // current user performing operations (defaults to "Admin")
        public string CurrentUser { get; set; } = "Admin";

        // constructor initializes service with file path and ensures data directory exists
        public CollegeDataService(string fileName)
        {
            // create path to Data folder in project directory, not in the bin folder
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

        // retrieves all colleges from the csv file
        public List<College> GetAllColleges()
        {
            List<College> colleges = new List<College>();

            try
            {
                // read each line and convert to college object
                var lines = File.ReadAllLines(_filePath);
                foreach (string line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    colleges.Add(College.FromCsv(line));
                }
            }
            catch (Exception ex)
            {
                // log error but don't crash the application
                Console.WriteLine($"Error reading college data: {ex.Message}");
            }

            return colleges;
        }

        // adds a new college to the csv file
        public void AddCollege(College college)
        {
            try
            {
                // using statement ensures proper resource disposal
                using (StreamWriter sw = File.AppendText(_filePath))
                {
                    sw.WriteLine(college.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding college: {ex.Message}");
                throw; 
            }
        }

        // updates existing college information
        public void UpdateCollege(College oldCollege, College newCollege)
        {
            // get current list of colleges
            List<College> colleges = GetAllColleges();
            bool replaced = false;

            try
            {
                // rewrite entire file with updated college data
                using (StreamWriter sw = new StreamWriter(_filePath))
                {
                    foreach (College college in colleges)
                    {
                        // if match found, write new college data
                        if (college.Name == oldCollege.Name && college.Code == oldCollege.Code)
                        {
                            // update audit fields
                            newCollege.DateTime = DateTime.Now;
                            newCollege.User = CurrentUser;
                            sw.WriteLine(newCollege.ToString());
                            replaced = true;
                        }
                        else
                        {
                            // keep existing college data
                            sw.WriteLine(college.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating college: {ex.Message}");
                throw;
            }

            // log if college wasn't found
            if (!replaced)
            {
                Console.WriteLine($"College to update not found: {oldCollege.Name} - {oldCollege.Code}");
            }
        }

        // removes a college from the csv file
        public void DeleteCollege(College collegeToDelete)
        {
            // get current list of colleges
            List<College> colleges = GetAllColleges();
            bool removed = false;

            try
            {
                // rewrite file excluding deleted college
                using (StreamWriter sw = new StreamWriter(_filePath))
                {
                    foreach (College college in colleges)
                    {
                        // skip the college to be deleted
                        if (college.Name == collegeToDelete.Name && college.Code == collegeToDelete.Code)
                        {
                            removed = true;
                        }
                        else
                        {
                            // keep other colleges
                            sw.WriteLine(college.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting college: {ex.Message}");
                throw;
            }

            // log if college wasn't found
            if (!removed)
            {
                Console.WriteLine($"College to delete not found: {collegeToDelete.Name} - {collegeToDelete.Code}");
            }
        }
    }
}