using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using sis_app.Models;
using sis_app.Services;

namespace sis_app.Controls.Add
{
    public partial class AddProgramControl : UserControl
    {
        // character limits for program name and code
        private const int MAX_PROGRAM_NAME_LENGTH = 45;
        private const int MAX_PROGRAM_CODE_LENGTH = 7;

        // services for handling data operations
        private ProgramDataService _programDataService;
        private CollegeDataService _collegeDataService;

        // constructor initializes the control and sets up the data services
        public AddProgramControl(ProgramDataService programDataService, CollegeDataService collegeDataService)
        {
            InitializeComponent();
            _programDataService = programDataService;
            _collegeDataService = collegeDataService;

            // attach text changed event handlers for length validation
            ProgramNameTextBox.TextChanged += ProgramNameTextBox_TextChanged;
            ProgramCodeTextBox.TextChanged += ProgramCodeTextBox_TextChanged;

            LoadCollegeCodes();
        }

        // loads college codes into the combobox
        public void LoadCollegeCodes()
        {
            List<string> collegeCodes = _collegeDataService.GetAllColleges().Select(c => c.Code).ToList();
            CollegeCodeComboBox.ItemsSource = collegeCodes;
        }

        // handles the add program button click event
        private void AddProgramButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                try
                {
                    // create new program object with current data
                    Program newProgram = new Program
                    {
                        Name = ProgramNameTextBox.Text,
                        Code = ProgramCodeTextBox.Text,
                        CollegeCode = CollegeCodeComboBox.SelectedItem.ToString(),
                        DateTime = DateTime.Now,
                        User = _programDataService.CurrentUser
                    };

                    // validate program code doesn't already exist
                    var existingPrograms = _programDataService.GetAllPrograms();
                    if (existingPrograms.Exists(p => p.Code.Equals(newProgram.Code, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show($"A program with code '{newProgram.Code}' already exists.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // add the program to the data service
                    _programDataService.AddProgram(newProgram);

                    // show success message
                    MessageBox.Show($"Added program {newProgram.Name} ({newProgram.Code})", "Success");

                    // clear input fields after successful addition
                    ClearFields();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding program: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // clears the input fields
        private void ClearFields()
        {
            ProgramNameTextBox.Text = "";
            ProgramCodeTextBox.Text = "";
            CollegeCodeComboBox.SelectedIndex = -1;
        }

        // validates program name input to allow only letters
        private void ProgramNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // regex pattern to match any character that's not a letter
            Regex regex = new Regex("[^a-zA-Z]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // validates program code input to allow only letters
        private void ProgramCodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // regex pattern to match any character that's not a letter
            Regex regex = new Regex("[^a-zA-Z]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // handles program name text changes
        private void ProgramNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // enforce maximum length for program name
                if (textBox.Text.Length > MAX_PROGRAM_NAME_LENGTH)
                {
                    textBox.Text = textBox.Text.Substring(0, MAX_PROGRAM_NAME_LENGTH);
                    textBox.CaretIndex = MAX_PROGRAM_NAME_LENGTH;
                }
            }
        }

        // handles program code text changes
        private void ProgramCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // store current caret position
                int caretIndex = textBox.CaretIndex;

                // convert program code to uppercase
                textBox.Text = textBox.Text.ToUpper();

                // enforce maximum length for program code
                if (textBox.Text.Length > MAX_PROGRAM_CODE_LENGTH)
                {
                    textBox.Text = textBox.Text.Substring(0, MAX_PROGRAM_CODE_LENGTH);
                    caretIndex = MAX_PROGRAM_CODE_LENGTH;
                }

                // restore caret position
                textBox.CaretIndex = caretIndex;
            }
        }

        // performs comprehensive input validation
        private bool ValidateInput()
        {
            // check if program name is empty
            if (string.IsNullOrWhiteSpace(ProgramNameTextBox.Text))
            {
                MessageBox.Show("Please enter a program name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProgramNameTextBox.Focus();
                return false;
            }

            // check program name length
            if (ProgramNameTextBox.Text.Length > MAX_PROGRAM_NAME_LENGTH)
            {
                MessageBox.Show($"Program name cannot exceed {MAX_PROGRAM_NAME_LENGTH} characters.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProgramNameTextBox.Focus();
                return false;
            }

            // check if program code is empty
            if (string.IsNullOrWhiteSpace(ProgramCodeTextBox.Text))
            {
                MessageBox.Show("Please enter a program code.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProgramCodeTextBox.Focus();
                return false;
            }

            // check minimum length for program code
            if (ProgramCodeTextBox.Text.Length < 2)
            {
                MessageBox.Show("Program code must be at least 2 characters.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProgramCodeTextBox.Focus();
                return false;
            }

            // check maximum length for program code
            if (ProgramCodeTextBox.Text.Length > MAX_PROGRAM_CODE_LENGTH)
            {
                MessageBox.Show($"Program code cannot exceed {MAX_PROGRAM_CODE_LENGTH} characters.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProgramCodeTextBox.Focus();
                return false;
            }

            // check if college code is selected
            if (CollegeCodeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a college code.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CollegeCodeComboBox.Focus();
                return false;
            }

            return true;
        }

        private void ProgramNameTextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }
    }
}