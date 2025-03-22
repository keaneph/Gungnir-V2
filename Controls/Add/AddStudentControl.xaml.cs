using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using sis_app.Models;
using sis_app.Services;

namespace sis_app.Controls.Add
{
    public partial class AddStudentControl : UserControl
    {
        // character limits and validation constants
        private const int MAX_FIRSTNAME_LENGTH = 26;
        private const int MAX_LASTNAME_LENGTH = 14;
        private const int YEAR_LENGTH = 4;
        private const int NUMBER_LENGTH = 4;
        private const int MIN_YEAR = 2016;
        private const int MAX_YEAR = 2025;

        // services for handling data operations
        private StudentDataService _studentDataService;
        private CollegeDataService _collegeDataService;
        private ProgramDataService _programDataService;

        // constructor initializes the control and sets up the data services
        public AddStudentControl(StudentDataService studentDataService, CollegeDataService collegeDataService, ProgramDataService programDataService)
        {
            InitializeComponent();
            _studentDataService = studentDataService;
            _collegeDataService = collegeDataService;
            _programDataService = programDataService;

            // attach text changed event handlers for length validation
            FirstNameTextBox.TextChanged += FirstNameTextBox_TextChanged;
            LastNameTextBox.TextChanged += LastNameTextBox_TextChanged;
        }

        // loads program codes into combobox
        public void LoadProgramCodes()
        {
            var programCodes = _programDataService.GetAllPrograms().Select(p => p.Code).ToList();
            ProgramCodeComboBox.ItemsSource = programCodes;

            CollegeCodeComboBox.Items.Clear();
            CollegeCodeComboBox.Items.Add("Select College");
            CollegeCodeComboBox.SelectedIndex = 0;
            CollegeCodeComboBox.IsEnabled = false;
        }

        // handles the add student button click event
        private void AddStudentButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                try
                {
                    string idNumber = $"{YearTextBox.Text}-{NumberTextBox.Text}";

                    // get selected values from comboboxes
                    var yearLevelContent = ((ComboBoxItem)YearLevelComboBox.SelectedItem).Content.ToString();
                    var genderContent = ((ComboBoxItem)GenderComboBox.SelectedItem).Content.ToString();

                    // create new student object with current data
                    Student newStudent = new Student
                    {
                        IDNumber = idNumber,
                        FirstName = FirstNameTextBox.Text,
                        LastName = LastNameTextBox.Text,
                        YearLevel = int.Parse(yearLevelContent),
                        Gender = genderContent,
                        ProgramCode = ProgramCodeComboBox.SelectedItem?.ToString(),
                        CollegeCode = CollegeCodeComboBox.SelectedItem.ToString(),
                        DateTime = DateTime.Now,
                        User = _studentDataService.CurrentUser
                    };

                    // validate student id doesn't already exist
                    var existingStudents = _studentDataService.GetAllStudents();
                    if (existingStudents.Exists(s => s.IDNumber.Equals(idNumber, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show($"A student with ID number '{idNumber}' already exists.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // add the student to the data service
                    _studentDataService.AddStudent(newStudent);

                    // show success message
                    MessageBox.Show($"Added student {newStudent.FirstName} {newStudent.LastName} ({newStudent.IDNumber})", "Success");

                    // clear input fields after successful addition
                    ClearFields();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding student: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // clears all input fields
        private void ClearFields()
        {
            YearTextBox.Clear();
            NumberTextBox.Clear();
            FirstNameTextBox.Clear();
            LastNameTextBox.Clear();
            YearLevelComboBox.SelectedIndex = -1;
            GenderComboBox.SelectedIndex = -1;
            ProgramCodeComboBox.SelectedIndex = -1;
            CollegeCodeComboBox.SelectedIndex = -1;
        }

        // validates name input to allow only letters
        private void FirstNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^a-zA-Z]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void LastNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^a-zA-Z]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // validates numeric input
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // validates year input
        private void YearTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            if (regex.IsMatch(e.Text) || YearTextBox.Text.Length >= YEAR_LENGTH)
            {
                e.Handled = true;
            }
        }

        // validates number input
        private void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            if (regex.IsMatch(e.Text) || NumberTextBox.Text.Length >= NUMBER_LENGTH)
            {
                e.Handled = true;
            }
        }

        // handles first name text changes
        private void FirstNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text.Length > MAX_FIRSTNAME_LENGTH)
                {
                    textBox.Text = textBox.Text.Substring(0, MAX_FIRSTNAME_LENGTH);
                    textBox.CaretIndex = MAX_FIRSTNAME_LENGTH;
                }
            }
        }

        // handles last name text changes
        private void LastNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text.Length > MAX_LASTNAME_LENGTH)
                {
                    textBox.Text = textBox.Text.Substring(0, MAX_LASTNAME_LENGTH);
                    textBox.CaretIndex = MAX_LASTNAME_LENGTH;
                }
            }
        }

        // handles program code selection change
        private void ProgramCodeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProgramCodeComboBox.SelectedItem != null)
            {
                string selectedProgramCode = ProgramCodeComboBox.SelectedItem.ToString();
                var selectedProgram = _programDataService.GetAllPrograms().FirstOrDefault(p => p.Code == selectedProgramCode);
                if (selectedProgram != null)
                {
                    CollegeCodeComboBox.IsEnabled = true;
                    CollegeCodeComboBox.Items.Clear();
                    CollegeCodeComboBox.Items.Add(selectedProgram.CollegeCode);
                    CollegeCodeComboBox.SelectedIndex = 0;
                }
            }
        }

        // performs comprehensive input validation
        private bool ValidateInput()
        {
            // validate year
            if (string.IsNullOrWhiteSpace(YearTextBox.Text))
            {
                MessageBox.Show("Please enter a year.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                YearTextBox.Focus();
                return false;
            }

            if (!int.TryParse(YearTextBox.Text, out int year) ||
                year < MIN_YEAR || year > MAX_YEAR)
            {
                MessageBox.Show($"Year must be between {MIN_YEAR} and {MAX_YEAR}.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                YearTextBox.Focus();
                return false;
            }

            // validate student number
            if (string.IsNullOrWhiteSpace(NumberTextBox.Text))
            {
                MessageBox.Show("Please enter a student number.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return false;
            }

            if (!int.TryParse(NumberTextBox.Text, out int number) ||
                number <= 0 || number > 9999)
            {
                MessageBox.Show("Student number must be between 0001 and 9999.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return false;
            }

            // validate first name
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("Please enter a first name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameTextBox.Focus();
                return false;
            }

            if (FirstNameTextBox.Text.Length > MAX_FIRSTNAME_LENGTH)
            {
                MessageBox.Show($"First name cannot exceed {MAX_FIRSTNAME_LENGTH} characters.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameTextBox.Focus();
                return false;
            }

            // validate last name
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Please enter a last name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameTextBox.Focus();
                return false;
            }

            if (LastNameTextBox.Text.Length > MAX_LASTNAME_LENGTH)
            {
                MessageBox.Show($"Last name cannot exceed {MAX_LASTNAME_LENGTH} characters.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameTextBox.Focus();
                return false;
            }

            // validate year level selection
            if (YearLevelComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a year level.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                YearLevelComboBox.Focus();
                return false;
            }

            // validate gender selection
            if (GenderComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a gender.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                GenderComboBox.Focus();
                return false;
            }

            // validate program code selection
            if (ProgramCodeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a program code.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProgramCodeComboBox.Focus();
                return false;
            }

            // validate college code selection
            if (CollegeCodeComboBox.SelectedItem == null ||
                CollegeCodeComboBox.SelectedItem.ToString() == "Select College")
            {
                MessageBox.Show("Please select a college code.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CollegeCodeComboBox.Focus();
                return false;
            }

            return true;
        }
    }
}