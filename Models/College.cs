using System;

namespace sis_app.Models
{
    /// <summary>
    /// Represents a college entity in the Student Information System
    /// </summary>
    public class College
    {
        // name of the college (e.g., "College of Engineering")
        public string Name { get; set; }

        // unique identifier code for the college (e.g., "COE")
        public string Code { get; set; }

        // timestamp of when the college record was last modified
        public DateTime DateTime { get; set; }

        // username of the person who last modified the record
        public string User { get; set; }

        /// <summary>
        /// converts college object to CSV format string
        /// format: Name,Code,DateTime,User
        /// </summary>
        /// <returns>comma-separated string of college properties</returns>
        public override string ToString()
        {
            return $"{Name},{Code},{DateTime},{User}";
        }

        /// <summary>
        /// creates a new College object from a CSV line
        /// </summary>
        /// <param name="csvLine">comma-separated string containing college data</param>
        /// <returns>new College object populated with CSV data</returns>
        public static College FromCsv(string csvLine)
        {
            // split the CSV line into individual values
            string[] values = csvLine.Split(',');

            // create and return new College object with parsed values
            return new College
            {
                Name = values[0],      // first value is college name
                Code = values[1],      // second value is college code
                DateTime = DateTime.Parse(values[2]),  // third value is datetime
                User = values[3]       // fourth value is username
            };
        }
    }
}