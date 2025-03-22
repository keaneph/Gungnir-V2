using System;
using System.IO;
using System.Linq;

namespace sis_app.Services
{
    public class UserDataService
    {
        // path to csv file storing user credentials
        internal readonly string _filePath;

        public UserDataService(string fileName)
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

            // ensure credentials file exists with header
            EnsureFileExists();
        }

        private void EnsureFileExists()
        {
            if (!File.Exists(_filePath))
            {
                // create new file with header row
                File.WriteAllText(_filePath, "Username,Password\n");
            }
        }

        public bool ValidateUser(string username, string password)
        {
            try
            {
                // skip header row and check credentials
                var lines = File.ReadAllLines(_filePath)
                    .Skip(1)
                    .Where(l => !string.IsNullOrWhiteSpace(l));

                return lines.Any(line =>
                {
                    var parts = line.Split(',');
                    return parts.Length == 2 &&
                           parts[0] == username &&
                           parts[1] == password;
                });
            }
            catch (Exception ex)
            {
                // log error and return false on any file access errors
                Console.WriteLine($"Error validating user: {ex.Message}");
                return false;
            }
        }

        public bool RegisterUser(string username, string password)
        {
            try
            {
                // append new user credentials to file
                using (StreamWriter sw = File.AppendText(_filePath))
                {
                    sw.WriteLine($"{username},{password}");
                }
                return true;
            }
            catch (Exception ex)
            {
                // log error and return false if registration fails
                Console.WriteLine($"Error registering user: {ex.Message}");
                return false;
            }
        }

        public bool UsernameExists(string username)
        {
            try
            {
                // skip header row and check for username
                var lines = File.ReadAllLines(_filePath)
                    .Skip(1)
                    .Where(l => !string.IsNullOrWhiteSpace(l));

                return lines.Any(line =>
                {
                    var parts = line.Split(',');
                    return parts.Length > 0 && parts[0] == username;
                });
            }
            catch (Exception ex)
            {
                // log error and return false on any file access errors
                Console.WriteLine($"Error checking username: {ex.Message}");
                return false;
            }
        }
    }
}