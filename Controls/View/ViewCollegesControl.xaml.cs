using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using sis_app.Models;
using sis_app.Services;

namespace sis_app.Controls.View
{
    public partial class ViewCollegesControl : UserControl
    {

        // maximum lengths for input validation
        private const int MAX_COLLEGE_NAME_LENGTH = 27;
        private const int MAX_COLLEGE_CODE_LENGTH = 9;
        private const int MIN_CODE_LENGTH = 2;

        // marker for deleted college references
        private const string DELETED_MARKER = "DELETED";
        // main data services for college and program management
        private readonly CollegeDataService _collegeDataService;
        private readonly ProgramDataService _programDataService;

        // collections for managing college data
        private readonly ObservableCollection<College> _colleges;
        private readonly Dictionary<College, College> _originalCollegeData;

        // search functionality support
        private ObservableCollection<College> _allColleges;
        private string _currentSearchText = string.Empty;

        public ViewCollegesControl(CollegeDataService collegeDataService)
        {
            InitializeComponent();

            // initialize services and collections
            _collegeDataService = collegeDataService ?? throw new ArgumentNullException(nameof(collegeDataService));
            _programDataService = new ProgramDataService("programs.csv");
            _colleges = new ObservableCollection<College>();
            _allColleges = new ObservableCollection<College>();
            _originalCollegeData = new Dictionary<College, College>();

            InitializeUserInterface();
        }
        private void InitializeUserInterface()
        {
            // setup initial UI state
            CollegeListView.ItemsSource = _colleges;
            LoadColleges();
            SortComboBox.SelectedIndex = 0;
        }

        public void LoadColleges()
        {
            try
            {
                // load and populate college collections
                var colleges = _collegeDataService.GetAllColleges();
                _allColleges = new ObservableCollection<College>(colleges);

                _colleges.Clear();
                foreach (var college in colleges)
                {
                    _colleges.Add(college);
                }

                // maintain search state
                if (!string.IsNullOrWhiteSpace(_currentSearchText))
                {
                    ApplySearch();
                }
                else
                {
                    SortColleges();
                }
            }
            catch (Exception ex)
            {
                HandleLoadError(ex);
            }
        }
        public void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // update search text and filter
            _currentSearchText = SearchBox.Text.Trim().ToLower();
            ApplySearch();
        }

        private void ApplySearch()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentSearchText))
                {
                    // show all colleges if search is empty
                    _colleges.Clear();
                    foreach (var college in _allColleges)
                    {
                        _colleges.Add(college);
                    }
                }
                else
                {
                    // filter colleges by name or code
                    var filteredColleges = _allColleges.Where(college =>
                        college.Name.ToLower().Contains(_currentSearchText) ||
                        college.Code.ToLower().Contains(_currentSearchText)
                    ).ToList();

                    _colleges.Clear();
                    foreach (var college in filteredColleges)
                    {
                        _colleges.Add(college);
                    }
                }

                SortColleges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during search: {ex.Message}", "Search Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleLoadError(Exception ex)
        {
            // display error message for loading failures
            MessageBox.Show($"Error loading colleges: {ex.Message}", "Load Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CollegeNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // validate name input - letters only
            e.Handled = !IsValidNameInput(e.Text);
        }

        private void CollegeCodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // validate code input - letters only
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
        private void CollegeNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // enforce name length limit
                EnforceLengthLimit(textBox, MAX_COLLEGE_NAME_LENGTH);
            }
        }

        private void CollegeCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                int caretIndex = textBox.CaretIndex;

                // convert to uppercase and apply rules
                string newText = textBox.Text.ToUpper();
                newText = EnforceCodeRules(newText);

                textBox.Text = newText;
                textBox.CaretIndex = Math.Min(caretIndex, newText.Length);
            }
        }

        private static void EnforceLengthLimit(TextBox textBox, int maxLength)
        {
            // trim text to maximum length
            if (textBox.Text.Length > maxLength)
            {
                textBox.Text = textBox.Text.Substring(0, maxLength);
                textBox.CaretIndex = maxLength;
            }
        }

        private static string EnforceCodeRules(string text)
        {
            // keep only letters and enforce length limit
            return new string(text.Where(char.IsLetter).Take(MAX_COLLEGE_CODE_LENGTH).ToArray());
        }
        private void EditModeToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            // backup original data before editing
            StoreOriginalData();
        }

        private void EditModeToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // validate and save changes
            ProcessEditedData();

            // maintain search state
            if (!string.IsNullOrWhiteSpace(_currentSearchText))
            {
                ApplySearch();
            }
        }

        private void StoreOriginalData()
        {
            // create backup of current data
            _originalCollegeData.Clear();
            foreach (var college in _colleges)
            {
                _originalCollegeData[college] = new College
                {
                    Name = college.Name,
                    Code = college.Code,
                    DateTime = college.DateTime,
                    User = college.User
                };
            }
        }

        private void ProcessEditedData()
        {
            foreach (var college in _colleges.ToList())
            {
                if (!_originalCollegeData.TryGetValue(college, out College originalCollege))
                    continue;

                // process only changed items
                if (!HasChanges(college, originalCollege))
                    continue;

                // validate and update or revert changes
                if (!ValidateAndUpdateCollege(college, originalCollege))
                {
                    RevertChanges(college, originalCollege);
                }
            }
            LoadColleges();
        }

        private static bool HasChanges(College current, College original)
        {
            return current.Name != original.Name || current.Code != original.Code;
        }

        private void RevertChanges(College current, College original)
        {
            // restore original values
            current.Name = original.Name;
            current.Code = original.Code;
        }
        private bool ValidateAndUpdateCollege(College college, College originalCollege)
        {
            // perform all validations
            if (!ValidateEditedData(college))
                return false;

            // check for duplicate codes
            if (IsDuplicateCode(college))
            {
                ShowDuplicateCodeError(college.Code);
                return false;
            }

            // update college and related programs
            UpdateRelatedPrograms(originalCollege.Code, college.Code);
            _collegeDataService.UpdateCollege(originalCollege, college);
            return true;
        }

        private bool ValidateEditedData(College college)
        {
            // validate name
            if (string.IsNullOrWhiteSpace(college.Name))
            {
                ShowValidationError("College name cannot be empty.");
                return false;
            }

            if (college.Name.Length > MAX_COLLEGE_NAME_LENGTH)
            {
                ShowValidationError($"College name cannot exceed {MAX_COLLEGE_NAME_LENGTH} characters.");
                return false;
            }

            // validate code
            if (string.IsNullOrWhiteSpace(college.Code))
            {
                ShowValidationError("College code cannot be empty.");
                return false;
            }

            if (college.Code.Length < MIN_CODE_LENGTH)
            {
                ShowValidationError($"College code must be at least {MIN_CODE_LENGTH} characters.");
                return false;
            }

            if (college.Code.Length > MAX_COLLEGE_CODE_LENGTH)
            {
                ShowValidationError($"College code cannot exceed {MAX_COLLEGE_CODE_LENGTH} characters.");
                return false;
            }

            // validate character types
            if (!college.Name.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                ShowValidationError("College name can only contain letters and spaces.");
                return false;
            }

            if (!college.Code.All(char.IsLetter))
            {
                ShowValidationError("College code can only contain letters.");
                return false;
            }

            return true;
        }

        private bool IsDuplicateCode(College college)
        {
            // check if code already exists
            return _colleges.Any(c =>
                c != college &&
                c.Code.Equals(college.Code, StringComparison.OrdinalIgnoreCase));
        }

        private static void ShowValidationError(string message)
        {
            MessageBox.Show(message, "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static void ShowDuplicateCodeError(string code)
        {
            MessageBox.Show($"A college with code '{code}' already exists.",
                "Duplicate Code Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        private void UpdateRelatedPrograms(string oldCode, string newCode)
        {
            // find programs using the old college code
            var affectedPrograms = _programDataService.GetAllPrograms()
                .Where(p => p.CollegeCode.Equals(oldCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!affectedPrograms.Any()) return;

            ShowProgramsUpdatedMessage(oldCode, newCode, affectedPrograms.Count);

            // update programs with new college code
            foreach (var program in affectedPrograms)
            {
                program.CollegeCode = newCode;
                _programDataService.UpdateProgram(program, program);
            }
        }

        private static void ShowProgramsUpdatedMessage(string oldCode, string newCode, int count)
        {
            MessageBox.Show(
                $"College code updated from '{oldCode}' to '{newCode}'. {count} associated programs have been updated.",
                "Programs Updated", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = CollegeListView.SelectedItems.OfType<College>().ToList();
            if (!selectedItems.Any())
            {
                ShowNoSelectionMessage();
                return;
            }

            if (ConfirmDeletion(selectedItems))
            {
                DeleteColleges(selectedItems);
                LoadColleges();
            }
        }

        private void ClearCollegesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmClearAll())
            {
                ClearAllData();
            }
        }

        private static void ShowNoSelectionMessage()
        {
            MessageBox.Show("Please select colleges to delete.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool ConfirmDeletion(List<College> selectedItems)
        {
            // check for affected programs
            var affectedPrograms = GetAffectedPrograms(selectedItems);
            string message = BuildDeleteConfirmationMessage(selectedItems.Count, affectedPrograms.Count);

            return MessageBox.Show(message, "Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        private void DeleteColleges(List<College> colleges)
        {
            foreach (var college in colleges)
            {
                DeleteCollegeAndUpdatePrograms(college);
            }
        }

        private void DeleteCollegeAndUpdatePrograms(College college)
        {

            // mark related programs as deleted
            var affectedPrograms = _programDataService.GetAllPrograms()
                .Where(p => p.CollegeCode.Equals(college.Code, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // remove college from all collections
            _collegeDataService.DeleteCollege(college);
            _colleges.Remove(college);
            _allColleges.Remove(college);

            foreach (var program in affectedPrograms)
            {
                var originalProgram = new Program
                {
                    Name = program.Name,
                    Code = program.Code,
                    CollegeCode = program.CollegeCode,
                    DateTime = program.DateTime,
                    User = program.User
                };

                program.CollegeCode = DELETED_MARKER;
                _programDataService.UpdateProgram(originalProgram, program);
            }
        }

        private List<Program> GetAffectedPrograms(List<College> colleges)
        {
            // find all programs using the selected colleges
            return _programDataService.GetAllPrograms()
                .Where(p => colleges.Any(c =>
                    c.Code.Equals(p.CollegeCode, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private static string BuildDeleteConfirmationMessage(int collegeCount, int programCount)
        {
            string message = $"Are you sure you want to delete the selected {collegeCount} colleges?";
            if (programCount > 0)
            {
                message += $"\nWarning: {programCount} programs are using these colleges and will be affected.";
            }
            return message;
        }

        private static bool ConfirmClearAll()
        {
            return MessageBox.Show(
                "Warning: Clearing colleges will also affect programs using these college codes. Continue?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        private void ClearAllData()
        {
            try
            {
                // clear only college data
                File.WriteAllText(_collegeDataService._filePath, string.Empty);
                LoadColleges();

                MessageBox.Show(
                    "All college data has been cleared successfully.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error clearing college data: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SortColleges();
        }

        private void SortColleges()
        {
            if (SortComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                ApplySorting(selectedItem.Content.ToString());
            }
        }

        private void ApplySorting(string sortOption)
        {
            // apply selected sort option
            switch (sortOption)
            {
                case "Date and Time Modified (Oldest First)":
                    SortList(c => c.DateTime, ListSortDirection.Ascending);
                    break;
                case "Date and Time Modified (Newest First)":
                    SortList(c => c.DateTime, ListSortDirection.Descending);
                    break;
                case "Alphabetical College Name":
                    SortList(c => c.Name, ListSortDirection.Ascending);
                    break;
                case "Alphabetical College Code":
                    SortList(c => c.Code, ListSortDirection.Ascending);
                    break;
                case "Alphabetical User":
                    SortList(c => c.User, ListSortDirection.Ascending);
                    break;
            }
        }

        private void SortList<TKey>(Func<College, TKey> keySelector, ListSortDirection direction)
        {
            // sort and refresh the list view
            var sortedList = direction == ListSortDirection.Ascending
                ? _colleges.OrderBy(keySelector).ToList()
                : _colleges.OrderByDescending(keySelector).ToList();

            _colleges.Clear();
            foreach (var item in sortedList)
            {
                _colleges.Add(item);
            }
            CollegeListView.Items.Refresh();
        }
    }
}