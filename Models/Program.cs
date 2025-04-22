using System;

namespace sis_app.Models
{
    // represents an academic program in the student information system
    public class Program
    {
        // full name of the program (e.g., "bachelor of science in computer science")
        public string Name { get; set; }

        // unique identifier for the program (e.g., "bscs")
        public string Code { get; set; }

        // code of the college this program belongs to
        public string? CollegeCode { get; set; }

        // timestamp of last modification
        public DateTime DateTime { get; set; }

        // username of person who last modified the record
        public string User { get; set; }

        // converts program object to csv string format
        public override string ToString()
        {
            return $"{Name},{Code},{CollegeCode},{DateTime},{User}";
        }

        // creates a new program object from a csv line
        public static Program FromCsv(string csvLine)
        {
            // split the csv line into individual values
            string[] values = csvLine.Split(',');

            // create new program object with parsed values
            return new Program
            {
                Name = values[0],          // program name
                Code = values[1],          // program code
                CollegeCode = values[2],   // associated college code
                DateTime = DateTime.Parse(values[3]),  // last modified date
                User = values[4]           // last modified by
            };
        }
    }
}