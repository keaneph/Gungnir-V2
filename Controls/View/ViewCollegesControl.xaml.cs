using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using MySql.Data.MySqlClient;
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

        public ViewCollegesControl(CollegeDataService collegeDataService, ProgramDataService programDataService)
        {
            InitializeComponent();

            // initialize services and collections
            _collegeDataService = collegeDataService ?? throw new ArgumentNullException(nameof(collegeDataService));
            _programDataService = programDataService ?? throw new ArgumentNullException(nameof(programDataService));
            _colleges = new ObservableCollection<College>();
            _allColleges = new ObservableCollection<College>();
            _originalCollegeData = new Dictionary<College, College>();

            InitializeUserInterface();
        }

        private void InitializeUserInterface()
        {
            CollegeListView.ItemsSource = _colleges;
            LoadColleges();
            SortComboBox.SelectedIndex = 0;
        }

        public void LoadColleges()
        {
            try
            {
                var colleges = _collegeDataService.GetAllColleges();
                if (colleges == null)
                {
                    throw new Exception("Failed to retrieve colleges from database");
                }

                _allColleges = new ObservableCollection<College>(colleges);
                _colleges.Clear();
                foreach (var college in colleges)
                {
                    _colleges.Add(college);
                }

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
            _currentSearchText = SearchBox.Text.Trim().ToLower();
            ApplySearch();
        }

        private void ApplySearch()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentSearchText))
                {
                    _colleges.Clear();
                    foreach (var college in _allColleges)
                    {
                        _colleges.Add(college);
                    }
                }
                else
                {
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
            MessageBox.Show($"Error loading colleges: {ex.Message}", "Load Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CollegeNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValidNameInput(e.Text);
        }

        private void CollegeCodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
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

        private void CollegeNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                EnforceLengthLimit(textBox, MAX_COLLEGE_NAME_LENGTH);
            }
        }

        private void CollegeCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
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
            return new string(text.Where(char.IsLetter).Take(MAX_COLLEGE_CODE_LENGTH).ToArray());
        }

        private void EditModeToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            StoreOriginalData();
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
            try
            {
                using (var connection = new MySqlConnection(App.DatabaseService._connectionString))
                {
                    connection.Open();
                    using var transaction = connection.BeginTransaction();

                    try
                    {
                        foreach (var college in _colleges.ToList())
                        {
                            if (!_originalCollegeData.TryGetValue(college, out College originalCollege))
                                continue;

                            if (!HasChanges(college, originalCollege))
                                continue;

                            if (!ValidateAndUpdateCollege(college, originalCollege))
                            {
                                RevertChanges(college, originalCollege);
                            }
                        }

                        transaction.Commit();
                        LoadColleges();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Database error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private static bool HasChanges(College current, College original)
        {
            return current.Name != original.Name || current.Code != original.Code;
        }

        private void RevertChanges(College current, College original)
        {
            current.Name = original.Name;
            current.Code = original.Code;
        }

        private bool ValidateAndUpdateCollege(College college, College originalCollege)
        {
            if (!ValidateEditedData(college))
                return false;

            if (IsDuplicateCode(college))
            {
                ShowDuplicateCodeError(college.Code);
                return false;
            }

            try
            {
                // Remove the UpdateRelatedPrograms call and let the database handle it
                _collegeDataService.UpdateCollege(originalCollege, college);

                // After successful update, refresh the programs to show the cascaded changes
                var affectedPrograms = _programDataService.GetAllPrograms()
                    .Where(p => p.CollegeCode.Equals(college.Code, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (affectedPrograms.Any())
                {
                    ShowProgramsUpdatedMessage(originalCollege.Code, college.Code, affectedPrograms.Count);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating college: {ex.Message}", "Update Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool ValidateEditedData(College college)
        {
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
            var affectedPrograms = _programDataService.GetAllPrograms()
                .Where(p => p.CollegeCode.Equals(oldCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!affectedPrograms.Any()) return;

            ShowProgramsUpdatedMessage(oldCode, newCode, affectedPrograms.Count);

            foreach (var program in affectedPrograms)
            {
                var originalProgram = new Program
                {
                    Code = program.Code,
                    Name = program.Name,
                    CollegeCode = program.CollegeCode,
                    User = program.User,
                    DateTime = program.DateTime
                };

                program.CollegeCode = newCode;
                _programDataService.UpdateProgram(originalProgram, program);
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
                try
                {
                    using (var connection = new MySqlConnection(App.DatabaseService._connectionString))
                    {
                        connection.Open();
                        using var transaction = connection.BeginTransaction();

                        try
                        {
                            DeleteColleges(selectedItems);
                            transaction.Commit();
                            LoadColleges();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting colleges: {ex.Message}", "Delete Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearCollegesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmClearAll())
            {
                try
                {
                    using (var connection = new MySqlConnection(App.DatabaseService._connectionString))
                    {
                        connection.Open();
                        using var transaction = connection.BeginTransaction();

                        try
                        {
                            var allPrograms = _programDataService.GetAllPrograms();

                            foreach (var college in _colleges.ToList())
                            {
                                DeleteCollegeAndUpdatePrograms(college);
                            }

                            transaction.Commit();
                            LoadColleges();

                            MessageBox.Show(
                                $"All college data has been cleared successfully. {allPrograms.Count} programs were updated.",
                                "Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
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
        }

        private static void ShowNoSelectionMessage()
        {
            MessageBox.Show("Please select colleges to delete.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool ConfirmDeletion(List<College> selectedItems)
        {
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
            var affectedPrograms = _programDataService.GetAllPrograms()
                .Where(p => p.CollegeCode.Equals(college.Code, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _collegeDataService.DeleteCollege(college);
            _colleges.Remove(college);
            _allColleges.Remove(college);

            foreach (var program in affectedPrograms)
            {
                var originalProgram = new Program
                {
                    Code = program.Code,
                    Name = program.Name,
                    CollegeCode = program.CollegeCode,
                    User = program.User,
                    DateTime = program.DateTime
                };

                program.CollegeCode = DELETED_MARKER;
                _programDataService.UpdateProgram(originalProgram, program);
            }
        }

        private List<Program> GetAffectedPrograms(List<College> colleges)
        {
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