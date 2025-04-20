using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using sis_app.Models;
using sis_app.Services;

namespace sis_app.Controls.View
{
    // main control for student information management
    public partial class ViewStudentControl : UserControl
    {
        // validation limits for student data
        private const int MAX_FIRSTNAME_LENGTH = 26;
        private const int MAX_LASTNAME_LENGTH = 14;
        private const string DELETED_MARKER = "DELETED";
        private const string ID_NUMBER_FORMAT = "YYYY-NNNN";
        private const int MIN_YEAR = 2016;
        private const int MAX_YEAR = 2025;
        private const int MIN_NUMBER = 1;
        private const int MAX_NUMBER = 9999;
    
        // services for data operations
        private readonly StudentDataService _studentDataService;
        private readonly ProgramDataService _programDataService;
        private readonly CollegeDataService _collegeDataService;

        // collections for data management
        private readonly ObservableCollection<Student> _students;
        private readonly Dictionary<Student, Student> _originalStudentData;
        private readonly List<string> _availableProgramCodes;
        private readonly List<int> _yearLevels;
        private readonly List<string> _genders;
        private ObservableCollection<Student> _allStudents;
        private string _currentSearchText = string.Empty;
       

        // exposed collections for binding
        public ObservableCollection<Student> Students => _students;
        public List<string> AvailableProgramCodes => _availableProgramCodes;
        public List<int> YearLevels => _yearLevels;
        public List<string> Genders => _genders;
       
        public ViewStudentControl(
            StudentDataService studentDataService,
            ProgramDataService programDataService,
            CollegeDataService collegeDataService)
        {
            InitializeComponent();

            // validate and initialize required services
            _studentDataService = studentDataService ?? throw new ArgumentNullException(nameof(studentDataService));
            _programDataService = programDataService ?? throw new ArgumentNullException(nameof(programDataService));
            _collegeDataService = collegeDataService ?? throw new ArgumentNullException(nameof(collegeDataService));

            // initialize data collections
            _students = new ObservableCollection<Student>();
            _allStudents = new ObservableCollection<Student>();
            _originalStudentData = new Dictionary<Student, Student>();
            _availableProgramCodes = new List<string>();
            _yearLevels = new List<int> { 1, 2, 3, 4 };
            _genders = new List<string> { "Male", "Female" };

            InitializeUserInterface();
        }

        // setup initial UI state
        private void InitializeUserInterface()
        {
            this.DataContext = this;
            StudentListView.ItemsSource = _students;
            LoadProgramCodes();
            LoadStudents();
            SortComboBox.SelectedIndex = 0;
        }

        // load available program codes
        private void LoadProgramCodes()
        {
            try
            {
                var programCodes = _programDataService.GetAllPrograms()
                    .Select(p => p.Code)
                    .OrderBy(code => code)
                    .ToList();

                _availableProgramCodes.Clear();
                _availableProgramCodes.AddRange(programCodes);
            }
            catch (Exception ex)
            {
                HandleLoadError("program codes", ex);
            }
        }
        

       
        // load all student data and update relationships
        public void LoadStudents()
        {
            try
            {
                var students = _studentDataService.GetAllStudents();
                var programs = _programDataService.GetAllPrograms()
                    .ToDictionary(p => p.Code.ToUpper(), StringComparer.OrdinalIgnoreCase);

                _allStudents = new ObservableCollection<Student>(students);

                _students.Clear();
                foreach (var student in students)
                {
                    UpdateStudentProgramRelationship(student, programs);
                    _students.Add(student);
                }

                // maintain search filter if active
                if (!string.IsNullOrWhiteSpace(_currentSearchText))
                {
                    ApplySearch();
                }
                else
                {
                    SortStudents();
                }
            }
            catch (Exception ex)
            {
                HandleLoadError("students", ex);
            }
        }

        // update program and college relationships for student
        private void UpdateStudentProgramRelationship(Student student, Dictionary<string, Program> programs)
        {
            if (programs.TryGetValue(student.ProgramCode, out var program))
            {
                student.CollegeCode = program.CollegeCode;
            }
            else if (student.ProgramCode != DELETED_MARKER)
            {
                student.ProgramCode = DELETED_MARKER;
                student.CollegeCode = DELETED_MARKER;
                _studentDataService.UpdateStudent(student, student);
            }
        }

        // display error message for load failures
        private void HandleLoadError(string dataType, Exception ex)
        {
            MessageBox.Show(
                $"Error loading {dataType}: {ex.Message}",
                "Load Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        

       
        // validate id number input
        private void IDNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValidIDNumberInput(e.Text);
        }

        // validate first name input
        private void FirstNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValidNameInput(e.Text);
        }

        // validate last name input
        private void LastNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValidNameInput(e.Text);
        }

        // check if input contains only digits and hyphens
        private static bool IsValidIDNumberInput(string text)
        {
            return text.All(c => char.IsDigit(c) || c == '-');
        }

        // check if input contains only letters
        private static bool IsValidNameInput(string text)
        {
            return text.All(char.IsLetter);
        }

        // comprehensive validation of student data
        private bool ValidateEditedData(Student student)
        {
            if (!ValidateIDNumber(student.IDNumber))
                return false;

            if (!ValidateFirstName(student.FirstName))
                return false;

            if (!ValidateLastName(student.LastName))
                return false;

            if (!ValidateProgramCode(student.ProgramCode))
                return false;

            return true;
        }

        // validate id number format and range
        private bool ValidateIDNumber(string idNumber)
        {
            if (string.IsNullOrWhiteSpace(idNumber))
            {
                ShowValidationError("ID Number cannot be empty.");
                return false;
            }

            var idParts = idNumber.Split('-');
            if (idParts.Length != 2 ||
                !int.TryParse(idParts[0], out int year) ||
                !int.TryParse(idParts[1], out int number) ||
                year < MIN_YEAR || year > MAX_YEAR ||
                number < MIN_NUMBER || number > MAX_NUMBER)
            {
                ShowValidationError(
                    $"ID Number must be in format {ID_NUMBER_FORMAT} where:\n" +
                    $"YYYY is between {MIN_YEAR}-{MAX_YEAR}\n" +
                    $"NNNN is between {MIN_NUMBER:D4}-{MAX_NUMBER:D4}"
                );
                return false;
            }

            return true;
        }

        // validate first name requirements
        private bool ValidateFirstName(string firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
            {
                ShowValidationError("First name cannot be empty.");
                return false;
            }

            if (firstName.Length > MAX_FIRSTNAME_LENGTH)
            {
                ShowValidationError($"First name cannot exceed {MAX_FIRSTNAME_LENGTH} characters.");
                return false;
            }

            if (!firstName.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                ShowValidationError("First name can only contain letters and spaces.");
                return false;
            }

            return true;
        }

        // validate last name requirements
        private bool ValidateLastName(string lastName)
        {
            if (string.IsNullOrWhiteSpace(lastName))
            {
                ShowValidationError("Last name cannot be empty.");
                return false;
            }

            if (lastName.Length > MAX_LASTNAME_LENGTH)
            {
                ShowValidationError($"Last name cannot exceed {MAX_LASTNAME_LENGTH} characters.");
                return false;
            }

            if (!lastName.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                ShowValidationError("Last name can only contain letters and spaces.");
                return false;
            }

            return true;
        }

        // validate program code exists and has valid college
        private bool ValidateProgramCode(string programCode)
        {
            if (programCode != DELETED_MARKER)
            {
                var program = _programDataService.GetAllPrograms()
                    .FirstOrDefault(p => p.Code.Equals(programCode, StringComparison.OrdinalIgnoreCase));

                if (program == null)
                {
                    ShowValidationError($"Program code '{programCode}' does not exist.");
                    return false;
                }

                var college = _collegeDataService.GetAllColleges()
                    .FirstOrDefault(c => c.Code.Equals(program.CollegeCode, StringComparison.OrdinalIgnoreCase));

                if (college == null)
                {
                    ShowValidationError($"The college for program '{programCode}' does not exist.");
                    return false;
                }
            }

            return true;
        }

        // display validation error messages
        private static void ShowValidationError(string message)
        {
            MessageBox.Show(
                message,
                "Validation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

        
        // handle search box text changes
        public void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearchText = SearchBox.Text.Trim().ToLower();
            ApplySearch();
        }

        // filter students based on search criteria
        private void ApplySearch()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentSearchText))
                {
                    _students.Clear();
                    foreach (var student in _allStudents)
                    {
                        _students.Add(student);
                    }
                }
                else
                {
                    var filteredStudents = _allStudents.Where(student =>
                        student.FirstName.ToLower().Contains(_currentSearchText) ||
                        student.LastName.ToLower().Contains(_currentSearchText) ||
                        student.IDNumber.ToLower().Contains(_currentSearchText) ||
                        student.ProgramCode.ToLower().Contains(_currentSearchText) ||
                        student.CollegeCode.ToLower().Contains(_currentSearchText) ||
                        student.Gender.ToLower().Contains(_currentSearchText) ||
                        student.YearLevel.ToString().Contains(_currentSearchText)
                    ).ToList();

                    _students.Clear();
                    foreach (var student in filteredStudents)
                    {
                        _students.Add(student);
                    }
                }

                SortStudents();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during search: {ex.Message}",
                    "Search Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        
        // handle id number text changes and formatting
        private void IDNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                FormatIDNumber(textBox);
            }
        }

        // handle first name text changes
        private void FirstNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                EnforceLengthLimit(textBox, MAX_FIRSTNAME_LENGTH);
            }
        }

        // handle last name text changes
        private void LastNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                EnforceLengthLimit(textBox, MAX_LASTNAME_LENGTH);
            }
        }

        // format id number with proper structure
        private static void FormatIDNumber(TextBox textBox)
        {
            string text = textBox.Text;
            int caretIndex = textBox.CaretIndex;

            if (text.Contains("-"))
            {
                var parts = text.Split('-');
                if (parts.Length == 2)
                {
                    string yearPart = parts[0].Length > 4 ? parts[0].Substring(0, 4) : parts[0];
                    string numberPart = parts[1].Length > 4 ? parts[1].Substring(0, 4) : parts[1];

                    textBox.Text = $"{yearPart}-{numberPart}";
                    textBox.CaretIndex = Math.Min(caretIndex, textBox.Text.Length);
                }
            }
            else if (text.Length > 4 && !text.Contains("-"))
            {
                textBox.Text = $"{text.Substring(0, 4)}-{text.Substring(4)}";
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        // enforce maximum length for text input
        private static void EnforceLengthLimit(TextBox textBox, int maxLength)
        {
            if (textBox.Text.Length > maxLength)
            {
                textBox.Text = textBox.Text.Substring(0, maxLength);
                textBox.CaretIndex = maxLength;
            }
        }
        
        // handle entering edit mode
        private void EditModeToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            LoadProgramCodes();
            StoreOriginalData();
            UpdateComboBoxes();
        }

        // handle exiting edit mode
        private void EditModeToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ProcessEditedData();
            // reapply search after edit mode
            if (!string.IsNullOrWhiteSpace(_currentSearchText))
            {
                ApplySearch();
            }
        }

        // store original data before editing
        private void StoreOriginalData()
        {
            _originalStudentData.Clear();
            foreach (var student in _students)
            {
                _originalStudentData[student] = new Student
                {
                    IDNumber = student.IDNumber,
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    YearLevel = student.YearLevel,
                    Gender = student.Gender,
                    ProgramCode = student.ProgramCode,
                    CollegeCode = student.CollegeCode,
                    DateTime = student.DateTime,
                    User = student.User
                };
            }
        }

        // update comboboxes for all students
        private void UpdateComboBoxes()
        {
            if (StudentListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                foreach (var student in _students)
                {
                    UpdateComboBoxesForStudent(student);
                }
            }
        }

        // update comboboxes for individual student
        private void UpdateComboBoxesForStudent(Student student)
        {
            var container = StudentListView.ItemContainerGenerator.ContainerFromItem(student) as ListViewItem;
            if (container == null) return;

            var comboBoxes = new List<ComboBox>();
            FindVisualChildren<ComboBox>(container, comboBoxes);

            foreach (var comboBox in comboBoxes)
            {
                var binding = comboBox.GetBindingExpression(ComboBox.SelectedItemProperty);
                if (binding == null) continue;

                string bindingPath = binding.ParentBinding.Path.Path;
                UpdateComboBoxBasedOnPath(comboBox, bindingPath, student);
            }
        }
        // update specific combobox based on data binding path
        private void UpdateComboBoxBasedOnPath(ComboBox comboBox, string bindingPath, Student student)
        {
            switch (bindingPath)
            {
                case "YearLevel":
                    comboBox.ItemsSource = _yearLevels;
                    comboBox.SelectedItem = student.YearLevel;
                    break;
                case "Gender":
                    comboBox.ItemsSource = _genders;
                    comboBox.SelectedItem = student.Gender;
                    break;
                case "ProgramCode":
                    comboBox.ItemsSource = _availableProgramCodes;
                    comboBox.SelectedItem = student.ProgramCode;
                    break;
            }
        }

        // process and validate edited student data
        private void ProcessEditedData()
        {
            try
            {
                foreach (var student in _students.ToList())
                {
                    if (!_originalStudentData.TryGetValue(student, out Student originalStudent))
                        continue;

                    if (!HasChanges(student, originalStudent))
                        continue;

                    if (!ValidateAndUpdateStudent(student, originalStudent))
                    {
                        RevertChanges(student, originalStudent);
                    }
                }
            }
            finally
            {
                LoadStudents();
            }
        }

        // check if student data has been modified
        private static bool HasChanges(Student current, Student original)
        {
            return current.FirstName != original.FirstName ||
                   current.LastName != original.LastName ||
                   current.IDNumber != original.IDNumber ||
                   current.YearLevel != original.YearLevel ||
                   current.Gender != original.Gender ||
                   current.ProgramCode != original.ProgramCode;
        }

        // revert student data to original state
        private void RevertChanges(Student current, Student original)
        {
            current.FirstName = original.FirstName;
            current.LastName = original.LastName;
            current.IDNumber = original.IDNumber;
            current.YearLevel = original.YearLevel;
            current.Gender = original.Gender;
            current.ProgramCode = original.ProgramCode;
            current.CollegeCode = original.CollegeCode;
        }

        
        // validate and update student record
        private bool ValidateAndUpdateStudent(Student student, Student originalStudent)
        {
            if (!ValidateEditedData(student))
                return false;

            if (IsDuplicateIDNumber(student))
            {
                ShowDuplicateIDError(student.IDNumber);
                return false;
            }

            UpdateStudentCollegeCode(student);
            _studentDataService.UpdateStudent(originalStudent, student);
            return true;
        }

        // check for duplicate student ID numbers
        private bool IsDuplicateIDNumber(Student student)
        {
            return _students.Any(s =>
                s != student &&
                s.IDNumber.Equals(student.IDNumber, StringComparison.OrdinalIgnoreCase));
        }

        // update student's college code based on program
        private void UpdateStudentCollegeCode(Student student)
        {
            if (student.ProgramCode != DELETED_MARKER)
            {
                var program = _programDataService.GetAllPrograms()
                    .FirstOrDefault(p => p.Code.Equals(student.ProgramCode, StringComparison.OrdinalIgnoreCase));

                if (program != null)
                {
                    student.CollegeCode = program.CollegeCode;
                }
            }
        }

        // show error message for duplicate ID numbers
        private static void ShowDuplicateIDError(string idNumber)
        {
            MessageBox.Show(
                $"A student with ID number '{idNumber}' already exists.",
                "Duplicate ID Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        // recursively find visual child elements
        private void FindVisualChildren<T>(DependencyObject parent, List<T> results) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                {
                    results.Add(t);
                }
                FindVisualChildren<T>(child, results);
            }
        }

        // handle deletion of selected students
        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = StudentListView.SelectedItems.OfType<Student>().ToList();
            if (!selectedItems.Any())
            {
                ShowNoSelectionMessage();
                return;
            }

            if (ConfirmDeletion(selectedItems))
            {
                DeleteStudents(selectedItems);
                LoadStudents();
            }
        }

        // handle clearing all student data
        private void ClearStudentsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmClearAll())
            {
                ClearAllData();
            }
        }

        // show message when no students are selected for deletion
        private static void ShowNoSelectionMessage()
        {
            MessageBox.Show(
                "Please select students to delete.",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // confirm deletion of selected students
        private bool ConfirmDeletion(List<Student> selectedItems)
        {
            string message = BuildDeleteConfirmationMessage(selectedItems.Count);

            return MessageBox.Show(
                message,
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            ) == MessageBoxResult.Yes;
        }

        // build confirmation message for deletion
        private static string BuildDeleteConfirmationMessage(int count)
        {
            return $"Are you sure you want to delete the selected {count} students?";
        }

        // delete selected students from database and collections
        private void DeleteStudents(List<Student> students)
        {
            foreach (var student in students)
            {
                _studentDataService.DeleteStudent(student);
                _students.Remove(student);
                _allStudents.Remove(student);
            }
        }

        // confirm clearing all student data
        private static bool ConfirmClearAll()
        {
            return MessageBox.Show(
                "Are you sure you want to clear all student data?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            ) == MessageBoxResult.Yes;
        }

        // clear all student data from file
        private void ClearAllData()
        {
            try
            {
                File.WriteAllText(_studentDataService._filePath, string.Empty);
                LoadStudents();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error clearing student data: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

       
        // handle program deletion and update affected students
        private void HandleProgramDeletion(string programCode)
        {
            var affectedStudents = _students
                .Where(s => s.ProgramCode.Equals(programCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!affectedStudents.Any()) return;

            ShowProgramDeletionMessage(programCode, affectedStudents.Count);

            foreach (var student in affectedStudents)
            {
                student.ProgramCode = DELETED_MARKER;
                student.CollegeCode = DELETED_MARKER;
                _studentDataService.UpdateStudent(student, student);
            }
        }

        // handle college code changes for history shi
        private void HandleCollegeCodeChange(string oldCollegeCode, string newCollegeCode)
        {
            var affectedPrograms = _programDataService.GetAllPrograms()
                .Where(p => p.CollegeCode.Equals(oldCollegeCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var program in affectedPrograms)
            {
                UpdateStudentsForCollegeChange(program.Code, newCollegeCode);
            }
        }

        // handle college deletion for history shi
        private void HandleCollegeDeletion(string collegeCode)
        {
            var affectedPrograms = _programDataService.GetAllPrograms()
                .Where(p => p.CollegeCode.Equals(collegeCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var program in affectedPrograms)
            {
                HandleProgramDeletion(program.Code);
            }
        }

        // update students affected by college changes
        private void UpdateStudentsForCollegeChange(string programCode, string newCollegeCode)
        {
            var affectedStudents = _students
                .Where(s => s.ProgramCode.Equals(programCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var student in affectedStudents)
            {
                student.CollegeCode = newCollegeCode;
                _studentDataService.UpdateStudent(student, student);
            }
        }

        // show message about program deletion impact
        private static void ShowProgramDeletionMessage(string programCode, int studentCount)
        {
            MessageBox.Show(
                $"Warning: {studentCount} students are enrolled in program '{programCode}'. " +
                "Their program code will be marked as 'DELETED'.",
                "Students Affected",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }
        
        // handle sort option changes
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SortStudents();
        }

        // sort student list based on selected criteria
        private void SortStudents()
        {
            if (SortComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                ApplySorting(selectedItem.Content.ToString());
            }
        }

        // apply sorting based on selected option
        private void ApplySorting(string sortOption)
        {
            switch (sortOption)
            {
                case "Date and Time Modified (Oldest First)":
                    SortList(s => s.DateTime, ListSortDirection.Ascending);
                    break;
                case "Date and Time Modified (Newest First)":
                    SortList(s => s.DateTime, ListSortDirection.Descending);
                    break;
                case "ID Number (Ascending)":
                    SortList(s => s.IDNumber, ListSortDirection.Ascending);
                    break;
                case "ID Number (Descending)":
                    SortList(s => s.IDNumber, ListSortDirection.Descending);
                    break;
                case "Alphabetical First Name":
                    SortList(s => s.FirstName, ListSortDirection.Ascending);
                    break;
                case "Alphabetical Last Name":
                    SortList(s => s.LastName, ListSortDirection.Ascending);
                    break;
                case "Year Level (Ascending)":
                    SortList(s => s.YearLevel, ListSortDirection.Ascending);
                    break;
                case "Year Level (Descending)":
                    SortList(s => s.YearLevel, ListSortDirection.Descending);
                    break;
                case "Alphabetical Gender":
                    SortList(s => s.Gender, ListSortDirection.Ascending);
                    break;
                case "Alphabetical Program Code":
                    SortList(s => s.ProgramCode, ListSortDirection.Ascending);
                    break;
                case "Alphabetical College Code":
                    SortList(s => s.CollegeCode, ListSortDirection.Ascending);
                    break;
                case "Alphabetical User":
                    SortList(s => s.User, ListSortDirection.Ascending);
                    break;
            }
        }

        // perform the actual sorting operation
        private void SortList<TKey>(Func<Student, TKey> keySelector, ListSortDirection direction)
        {
            var sortedList = direction == ListSortDirection.Ascending
                ? _students.OrderBy(keySelector).ToList()
                : _students.OrderByDescending(keySelector).ToList();

            _students.Clear();
            foreach (var item in sortedList)
            {
                _students.Add(item);
            }
            StudentListView.Items.Refresh();
        }
        
    


        private void StudentListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
                {
                    // for history shi
                }

        private void ProgramCodeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
            {
                string selectedProgramCode = comboBox.SelectedItem.ToString();

                if (ValidateProgramCodeChange(selectedProgramCode))
                {
                    var program = _programDataService.GetAllPrograms()
                        .FirstOrDefault(p => p.Code.Equals(selectedProgramCode, StringComparison.OrdinalIgnoreCase));

                    if (program != null && comboBox.DataContext is Student student)
                    {
                        student.CollegeCode = program.CollegeCode;
                    }
                }
                else
                {
                    // Revert selection if validation fails
                    if (comboBox.DataContext is Student student)
                    {
                        comboBox.SelectedItem = student.ProgramCode;
                    }
                }
            }
        }

        private bool ValidateProgramCodeChange(string newProgramCode)
        {
            var program = _programDataService.GetAllPrograms()
                .FirstOrDefault(p => p.Code.Equals(newProgramCode, StringComparison.OrdinalIgnoreCase));

            if (program == null && newProgramCode != DELETED_MARKER)
            {
                ShowValidationError($"Program code '{newProgramCode}' does not exist.");
                return false;
            }

            if (program != null)
            {
                var college = _collegeDataService.GetAllColleges()
                    .FirstOrDefault(c => c.Code.Equals(program.CollegeCode, StringComparison.OrdinalIgnoreCase));

                if (college == null)
                {
                    ShowValidationError($"The college for program '{newProgramCode}' does not exist.");
                    return false;
                }
            }

            return true;
        }
       
    }
}
