﻿using System;
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
    public partial class ViewProgramsControl : UserControl
    {
        // constants for validation and data management
        #region Constants
        private const int MAX_PROGRAM_NAME_LENGTH = 27;
        private const int MAX_PROGRAM_CODE_LENGTH = 7;
        private const int MIN_CODE_LENGTH = 2;
        private const string DELETED_MARKER = "DELETED";
        #endregion

        // private fields for data services and collections
        #region Private Fields
        private readonly ProgramDataService _programDataService;
        private readonly StudentDataService _studentDataService;
        private readonly CollegeDataService _collegeDataService;
        private readonly ObservableCollection<Program> _programs;
        private readonly Dictionary<Program, Program> _originalProgramData;
        private List<string> _availableCollegeCodes;
        private ObservableCollection<Program> _allPrograms;
        private string _currentSearchText = string.Empty;
        #endregion

        // public properties
        #region Public Properties
        public string CurrentUser { get; set; }

        public List<string> AvailableCollegeCodes
        {
            get { return _availableCollegeCodes; }
            private set { _availableCollegeCodes = value; }
        }
        #endregion

        // constructor and initialization methods
        #region Constructor and Initialization
        public ViewProgramsControl(ProgramDataService programDataService, StudentDataService studentDataService)
        {
            InitializeComponent();

            // validate and initialize services
            _programDataService = programDataService ?? throw new ArgumentNullException(nameof(programDataService));
            _studentDataService = studentDataService ?? throw new ArgumentNullException(nameof(studentDataService));
            _collegeDataService = new CollegeDataService("colleges.csv");

            // initialize collections
            _programs = new ObservableCollection<Program>();
            _allPrograms = new ObservableCollection<Program>();
            _originalProgramData = new Dictionary<Program, Program>();
            _availableCollegeCodes = new List<string>();

            InitializeUserInterface();
        }

        private void InitializeUserInterface()
        {
            ProgramListView.ItemsSource = _programs;
            LoadPrograms();
            LoadAvailableCollegeCodes();
            SortComboBox.SelectedIndex = 0;
        }

        // loads available college codes from service
        private void LoadAvailableCollegeCodes()
        {
            try
            {
                _availableCollegeCodes = _collegeDataService.GetAllColleges()
                    .Select(c => c.Code)
                    .OrderBy(code => code)
                    .ToList();
            }
            catch (Exception ex)
            {
                HandleLoadError("college codes", ex);
            }
        }
        #endregion

        // methods for loading program data
        #region Data Loading Methods
        public void LoadPrograms()
        {
            try
            {
                var programs = _programDataService.GetAllPrograms();
                _allPrograms = new ObservableCollection<Program>(programs);

                _programs.Clear();
                foreach (var program in programs)
                {
                    _programs.Add(program);
                }

                if (!string.IsNullOrWhiteSpace(_currentSearchText))
                {
                    ApplySearch();
                }
                else
                {
                    SortPrograms();
                }
            }
            catch (Exception ex)
            {
                HandleLoadError("programs", ex);
            }
        }

        private void HandleLoadError(string dataType, Exception ex)
        {
            MessageBox.Show(
                $"Error loading {dataType}: {ex.Message}",
                "Load Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        #endregion

        // input validation methods
        #region Input Validation Methods
        private void ProgramNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValidNameInput(e.Text);
        }

        private void ProgramCodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValidCodeInput(e.Text);
        }

        private static bool IsValidNameInput(string text)
        {
            return text.All(c => char.IsLetter(c));
        }

        private static bool IsValidCodeInput(string text)
        {
            return text.All(c => char.IsLetter(c));
        }

        private bool ValidateEditedData(Program program)
        {
            if (!ValidateProgramName(program.Name))
                return false;

            if (!ValidateProgramCode(program.Code))
                return false;

            if (!ValidateCollegeCode(program.CollegeCode))
                return false;

            return true;
        }

        // validates program name according to rules
        private bool ValidateProgramName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowValidationError("Program name cannot be empty.");
                return false;
            }

            if (name.Length > MAX_PROGRAM_NAME_LENGTH)
            {
                ShowValidationError($"Program name cannot exceed {MAX_PROGRAM_NAME_LENGTH} characters.");
                return false;
            }

            if (!name.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                ShowValidationError("Program name can only contain letters and spaces.");
                return false;
            }

            return true;
        }

        // validates program code according to rules
        private bool ValidateProgramCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowValidationError("Program code cannot be empty.");
                return false;
            }

            if (code.Length < MIN_CODE_LENGTH)
            {
                ShowValidationError($"Program code must be at least {MIN_CODE_LENGTH} characters.");
                return false;
            }

            if (code.Length > MAX_PROGRAM_CODE_LENGTH)
            {
                ShowValidationError($"Program code cannot exceed {MAX_PROGRAM_CODE_LENGTH} characters.");
                return false;
            }

            if (!code.All(char.IsLetter))
            {
                ShowValidationError("Program code can only contain letters.");
                return false;
            }

            return true;
        }

        // validates college code exists
        private bool ValidateCollegeCode(string collegeCode)
        {
            if (!_availableCollegeCodes.Contains(collegeCode, StringComparer.OrdinalIgnoreCase))
            {
                ShowValidationError($"College code '{collegeCode}' does not exist.");
                return false;
            }

            return true;
        }
        #endregion

        // handlers for text changes
        #region Text Change Handlers
        private void ProgramNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                EnforceLengthLimit(textBox, MAX_PROGRAM_NAME_LENGTH);
            }
        }

        private void ProgramCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                int caretIndex = textBox.CaretIndex;
                string newText = textBox.Text.ToUpper();

                newText = EnforceCodeRules(newText);

                textBox.Text = newText;
                textBox.CaretIndex = Math.Min(caretIndex, newText.Length);
            }
        }

        private static void EnforceLengthLimit(TextBox textBox, int maxLength)
        {
            if (textBox.Text.Length > maxLength)
            {
                textBox.Text = textBox.Text.Substring(0, maxLength);
                textBox.CaretIndex = maxLength;
            }
        }

        private static string EnforceCodeRules(string text)
        {
            return new string(text.Where(char.IsLetter).Take(MAX_PROGRAM_CODE_LENGTH).ToArray());
        }

        private static void ShowValidationError(string message)
        {
            MessageBox.Show(
                message,
                "Validation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }
        #endregion

        // handlers for edit mode
        #region Edit Mode Handlers
        private void EditModeToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            LoadAvailableCollegeCodes();
            StoreOriginalData();
            UpdateComboBoxes();
        }

        private void EditModeToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ProcessEditedData();
            if (!string.IsNullOrWhiteSpace(_currentSearchText))
            {
                ApplySearch();
            }
        }

        private void StoreOriginalData()
        {
            _originalProgramData.Clear();
            foreach (var program in _programs)
            {
                _originalProgramData[program] = new Program
                {
                    Name = program.Name,
                    Code = program.Code,
                    CollegeCode = program.CollegeCode,
                    DateTime = program.DateTime,
                    User = program.User
                };
            }
        }

        private void UpdateComboBoxes()
        {
            if (ProgramListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                foreach (var program in _programs)
                {
                    var container = ProgramListView.ItemContainerGenerator.ContainerFromItem(program) as ListViewItem;
                    if (container != null)
                    {
                        var collegeCodeComboBox = FindVisualChild<ComboBox>(container);
                        if (collegeCodeComboBox != null)
                        {
                            collegeCodeComboBox.ItemsSource = _availableCollegeCodes;
                            collegeCodeComboBox.SelectedItem = program.CollegeCode;
                        }
                    }
                }
            }
        }

        private void ProcessEditedData()
        {
            try
            {
                foreach (var program in _programs.ToList())
                {
                    if (!_originalProgramData.TryGetValue(program, out Program originalProgram))
                        continue;

                    if (!HasChanges(program, originalProgram))
                        continue;

                    if (!ValidateAndUpdateProgram(program, originalProgram))
                    {
                        RevertChanges(program, originalProgram);
                    }
                }
            }
            finally
            {
                LoadPrograms();
            }
        }

        private static bool HasChanges(Program current, Program original)
        {
            return current.Name != original.Name ||
                   current.Code != original.Code ||
                   current.CollegeCode != original.CollegeCode;
        }

        private void RevertChanges(Program current, Program original)
        {
            current.Name = original.Name;
            current.Code = original.Code;
            current.CollegeCode = original.CollegeCode;
        }
        #endregion

        // search functionality
        #region Search Functionality
        public void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearchText = SearchBox.Text.Trim().ToLower();
            ApplySearch();
        }

        private void ApplySearch()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentSearchText))
                {
                    _programs.Clear();
                    foreach (var program in _allPrograms)
                    {
                        _programs.Add(program);
                    }
                }
                else
                {
                    var filteredPrograms = _allPrograms.Where(program =>
                        program.Name.ToLower().Contains(_currentSearchText) ||
                        program.Code.ToLower().Contains(_currentSearchText) ||
                        program.CollegeCode.ToLower().Contains(_currentSearchText)
                    ).ToList();

                    _programs.Clear();
                    foreach (var program in filteredPrograms)
                    {
                        _programs.Add(program);
                    }
                }

                SortPrograms();
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
        #endregion

        // data management methods
        #region Data Management Methods
        private bool ValidateAndUpdateProgram(Program program, Program originalProgram)
        {
            if (!ValidateEditedData(program))
                return false;

            if (IsDuplicateCode(program))
            {
                ShowDuplicateCodeError(program.Code);
                return false;
            }

            UpdateRelatedStudents(originalProgram.Code, program.Code, program.CollegeCode);
            _programDataService.UpdateProgram(originalProgram, program);
            return true;
        }

        private bool IsDuplicateCode(Program program)
        {
            return _programs.Any(p =>
                p != program &&
                p.Code.Equals(program.Code, StringComparison.OrdinalIgnoreCase));
        }

        private void UpdateRelatedStudents(string oldCode, string newCode, string newCollegeCode)
        {
            var affectedStudents = _studentDataService.GetAllStudents()
                .Where(s => s.ProgramCode.Equals(oldCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!affectedStudents.Any()) return;

            ShowStudentsUpdatedMessage(oldCode, newCode, affectedStudents.Count);

            foreach (var student in affectedStudents)
            {
                student.ProgramCode = newCode;
                student.CollegeCode = newCollegeCode;
                _studentDataService.UpdateStudent(student, student);
            }
        }

        private static void ShowDuplicateCodeError(string code)
        {
            MessageBox.Show(
                $"A program with code '{code}' already exists.",
                "Duplicate Code Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private static void ShowStudentsUpdatedMessage(string oldCode, string newCode, int count)
        {
            MessageBox.Show(
                $"Program code changed from '{oldCode}' to '{newCode}'. {count} students will be updated.",
                "Students Affected",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                    return found;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
        #endregion

        // delete operations
        #region Delete Operations
        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ProgramListView.SelectedItems.OfType<Program>().ToList();
            if (!selectedItems.Any())
            {
                ShowNoSelectionMessage();
                return;
            }

            if (ConfirmDeletion(selectedItems))
            {
                DeletePrograms(selectedItems);
                LoadPrograms();
            }
        }

        private void ClearProgramsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmClearAll())
            {
                ClearAllData();
            }
        }

        private static void ShowNoSelectionMessage()
        {
            MessageBox.Show(
                "Please select programs to delete.",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private bool ConfirmDeletion(List<Program> selectedItems)
        {
            var affectedStudents = GetAffectedStudents(selectedItems);
            string message = BuildDeleteConfirmationMessage(selectedItems.Count, affectedStudents.Count);

            return MessageBox.Show(
                message,
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            ) == MessageBoxResult.Yes;
        }

        private void DeletePrograms(List<Program> programs)
        {
            foreach (var program in programs)
            {
                DeleteProgramAndUpdateStudents(program);
            }
        }

        private void DeleteProgramAndUpdateStudents(Program program)
        {
            _programDataService.DeleteProgram(program);
            _programs.Remove(program);
            _allPrograms.Remove(program);

            var affectedStudents = _studentDataService.GetAllStudents()
                .Where(s => s.ProgramCode.Equals(program.Code, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var student in affectedStudents)
            {
                student.ProgramCode = DELETED_MARKER;
                student.CollegeCode = DELETED_MARKER;
                _studentDataService.UpdateStudent(student, student);
            }
        }

        private List<Student> GetAffectedStudents(List<Program> programs)
        {
            return _studentDataService.GetAllStudents()
                .Where(s => programs.Any(p =>
                    p.Code.Equals(s.ProgramCode, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private static string BuildDeleteConfirmationMessage(int programCount, int studentCount)
        {
            string message = $"Are you sure you want to delete the selected {programCount} programs?";
            if (studentCount > 0)
            {
                message += $"\nWarning: {studentCount} students are enrolled in these programs and will be affected.";
            }
            return message;
        }

        private static bool ConfirmClearAll()
        {
            return MessageBox.Show(
                "Warning: Clearing programs will affect all students enrolled in these programs. Continue?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            ) == MessageBoxResult.Yes;
        }

        private void ClearAllData()
        {
            try
            {
                var students = _studentDataService.GetAllStudents();
                var affectedStudents = students.Where(s => s.ProgramCode != DELETED_MARKER).ToList();

                if (affectedStudents.Any())
                {
                    foreach (var student in affectedStudents)
                    {
                        student.ProgramCode = DELETED_MARKER;
                        student.CollegeCode = DELETED_MARKER;
                        _studentDataService.UpdateStudent(student, student);
                    }
                }

                File.WriteAllText(_programDataService._filePath, string.Empty);
                LoadPrograms();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error clearing program data: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // sorting methods
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SortPrograms();
        }

        private void SortPrograms()
        {
            if (SortComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                ApplySorting(selectedItem.Content.ToString());
            }
        }

        private void ApplySorting(string sortOption)
        {
            switch (sortOption)
            {
                case "Date and Time Modified (Oldest First)":
                    SortList(p => p.DateTime, ListSortDirection.Ascending);
                    break;
                case "Date and Time Modified (Newest First)":
                    SortList(p => p.DateTime, ListSortDirection.Descending);
                    break;
                case "Alphabetical Program Name":
                    SortList(p => p.Name, ListSortDirection.Ascending);
                    break;
                case "Alphabetical Program Code":
                    SortList(p => p.Code, ListSortDirection.Ascending);
                    break;
                case "Alphabetical College Code":
                    SortList(p => p.CollegeCode, ListSortDirection.Ascending);
                    break;
                case "Alphabetical User":
                    SortList(p => p.User, ListSortDirection.Ascending);
                    break;
            }
        }

        private void SortList<TKey>(Func<Program, TKey> keySelector, ListSortDirection direction)
        {
            var sortedList = direction == ListSortDirection.Ascending
                ? _programs.OrderBy(keySelector).ToList()
                : _programs.OrderByDescending(keySelector).ToList();

            _programs.Clear();
            foreach (var item in sortedList)
            {
                _programs.Add(item);
            }
            ProgramListView.Items.Refresh();
        }
        #endregion
    }
}