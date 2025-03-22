using System;

namespace sis_app.Models
{
    // represents a student entity in the student information system
    public class Student
    {
        // unique student identification number (e.g., "2023-1234")
        public string IDNumber { get; set; }

        // student's first name
        public string FirstName { get; set; }

        // student's last name
        public string LastName { get; set; }

        // current year level of the student (1-4)
        public int YearLevel { get; set; }

        // student's gender (male/female)
        public string Gender { get; set; }

        // code of the program the student is enrolled in
        public string ProgramCode { get; set; }

        // code of the college the student belongs to
        public string CollegeCode { get; set; }

        // timestamp of last modification
        public DateTime DateTime { get; set; }

        // username of person who last modified the record
        public string User { get; set; }

        // converts student object to csv string format
        public override string ToString()
        {
            return $"{IDNumber},{FirstName},{LastName},{YearLevel},{Gender},{ProgramCode},{CollegeCode},{DateTime},{User}";
        }

        // creates a new student object from a csv line
        public static Student FromCsv(string csvLine)
        {
            // split the csv line into individual values
            string[] values = csvLine.Split(',');

            // create new student object with parsed values
            return new Student
            {
                IDNumber = values[0],      // student id
                FirstName = values[1],     // first name
                LastName = values[2],      // last name
                YearLevel = int.Parse(values[3]),  // year level as integer
                Gender = values[4],        // gender
                ProgramCode = values[5],   // program code
                CollegeCode = values[6],   // college code
                DateTime = DateTime.Parse(values[7]),  // last modified date
                User = values[8]           // last modified by
            };
        }
    }
}