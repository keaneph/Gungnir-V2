using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using sis_app.Models;
using sis_app.Services;

namespace sis_app.Controls.Add
{
    public partial class AddCollegeControl : UserControl
    {
        // character limits for college name and code
        private const int MAX_COLLEGE_NAME_LENGTH = 27;
        private const int MAX_COLLEGE_CODE_LENGTH = 9;

        // service for handling college data operations
        private CollegeDataService _collegeDataService;

        // constructor initializes the control and sets up the data service
        public AddCollegeControl(CollegeDataService collegeDataService)
        {
            InitializeComponent();
            _collegeDataService = collegeDataService;

            // attach text changed event handlers for length validation
            CollegeNameTextBox.TextChanged += CollegeNameTextBox_TextChanged;
            CollegeCodeTextBox.TextChanged += CollegeCodeTextBox_TextChanged;
        }

        // handles the add college button click event
        private void AddCollegeButton_Click(object sender, RoutedEventArgs e)
        {
            // validate input before proceeding
            if (ValidateInput())
            {
                try
                {
                    // create new college object with current data
                    College newCollege = new College
                    {
                        Name = CollegeNameTextBox.Text,
                        Code = CollegeCodeTextBox.Text,
                        DateTime = DateTime.Now,
                        User = _collegeDataService.CurrentUser
                    };

                    // validate college code doesn't already exist
                    var existingColleges = _collegeDataService.GetAllColleges();
                    if (existingColleges.Exists(c => c.Code.Equals(newCollege.Code, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show($"A college with code '{newCollege.Code}' already exists.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // add the college to the data service
                    _collegeDataService.AddCollege(newCollege);

                    // show success message
                    MessageBox.Show($"Added college {newCollege.Name} ({newCollege.Code})", "Success");

                    // clear input fields after successful addition
                    ClearFields();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding college: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // clears the input fields
        private void ClearFields()
        {
            CollegeNameTextBox.Text = "";
            CollegeCodeTextBox.Text = "";
        }

        // validates college name input to allow only letters
        private void CollegeNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // regex pattern to match any character that's not a letter
            Regex regex = new Regex("[^a-zA-Z]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // validates college code input to allow only letters
        private void CollegeCodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // regex pattern to match any character that's not a letter
            Regex regex = new Regex("[^a-zA-Z]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // handles college name text changes
        private void CollegeNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // enforce maximum length for college name
                if (textBox.Text.Length > MAX_COLLEGE_NAME_LENGTH)
                {
                    textBox.Text = textBox.Text.Substring(0, MAX_COLLEGE_NAME_LENGTH);
                    textBox.CaretIndex = MAX_COLLEGE_NAME_LENGTH;
                }
            }
        }

        // handles college code text changes
        private void CollegeCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // store current caret position
                int caretIndex = textBox.CaretIndex;

                // convert college code to uppercase
                textBox.Text = textBox.Text.ToUpper();

                // enforce maximum length for college code
                if (textBox.Text.Length > MAX_COLLEGE_CODE_LENGTH)
                {
                    textBox.Text = textBox.Text.Substring(0, MAX_COLLEGE_CODE_LENGTH);
                    caretIndex = MAX_COLLEGE_CODE_LENGTH;
                }

                // restore caret position
                textBox.CaretIndex = caretIndex;
            }
        }

        // performs comprehensive input validation
        private bool ValidateInput()
        {
            // check if college name is empty
            if (string.IsNullOrWhiteSpace(CollegeNameTextBox.Text))
            {
                MessageBox.Show("Please enter a college name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CollegeNameTextBox.Focus();
                return false;
            }

            // check college name length
            if (CollegeNameTextBox.Text.Length > MAX_COLLEGE_NAME_LENGTH)
            {
                MessageBox.Show($"College name cannot exceed {MAX_COLLEGE_NAME_LENGTH} characters.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CollegeNameTextBox.Focus();
                return false;
            }

            // check if college code is empty
            if (string.IsNullOrWhiteSpace(CollegeCodeTextBox.Text))
            {
                MessageBox.Show("Please enter a college code.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CollegeCodeTextBox.Focus();
                return false;
            }

            // check minimum length for college code
            if (CollegeCodeTextBox.Text.Length < 2)
            {
                MessageBox.Show("College code must be at least 2 characters.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CollegeCodeTextBox.Focus();
                return false;
            }

            // check maximum length for college code
            if (CollegeCodeTextBox.Text.Length > MAX_COLLEGE_CODE_LENGTH)
            {
                MessageBox.Show($"College code cannot exceed {MAX_COLLEGE_CODE_LENGTH} characters.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CollegeCodeTextBox.Focus();
                return false;
            }

            return true;
        }
    }
}