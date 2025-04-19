// required namespaces for the application
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using sis_app.Services;
using sis_app.Controls.Add;
using sis_app.Controls.View;
using sis_app.Views;

namespace sis_app.Views
{
    public partial class MainWindow : Window
    {

        // default user if none provided
        private const string DEFAULT_USER = "Admin";



        // data services for different entities
        private CollegeDataService _collegeDataService;
        private ProgramDataService _programDataService;
        private StudentDataService _studentDataService;

        // controls for adding new entries
        private AddCollegeControl _addCollegeControl;
        private AddProgramControl _addProgramControl;
        private AddStudentControl _addStudentControl;

        // controls for viewing entries
        private ViewCollegesControl _viewCollegesControl;
        private ViewProgramsControl _viewProgramsControl;
        private ViewStudentControl _viewStudentControl;

        // dashboard view control
        private DashboardView _dashboardView;



        // current user of the application
        public string CurrentUser { get; private set; }


 
        // main window constructor
        public MainWindow(string username)
        {
            try
            {
                InitializeComponent();
                InitializeServices(username);
                InitializeControls();
                InitializeUserInterface();
            }
            catch (Exception ex)
            {
                HandleInitializationError(ex);
            }
        }



        // initialize data services
        private void InitializeServices(string username)
        {
            CurrentUser = username ?? DEFAULT_USER;

            _collegeDataService = new CollegeDataService("colleges.csv") { CurrentUser = CurrentUser };
            _programDataService = new ProgramDataService("programs.csv") { CurrentUser = CurrentUser };
            _studentDataService = new StudentDataService("students.csv") { CurrentUser = CurrentUser };
        }

        // initialize UI controls
        private void InitializeControls()
        {
            _dashboardView = new DashboardView();

            // initialize add controls
            _addCollegeControl = new AddCollegeControl(_collegeDataService);
            _addProgramControl = new AddProgramControl(_programDataService, _collegeDataService);
            _addStudentControl = new AddStudentControl(_studentDataService, _collegeDataService, _programDataService);

            // initialize view controls
            _viewCollegesControl = new ViewCollegesControl(_collegeDataService);
            _viewProgramsControl = new ViewProgramsControl(_programDataService, _studentDataService)
            {
                CurrentUser = CurrentUser
            };
            _viewStudentControl = new ViewStudentControl(_studentDataService, _programDataService, _collegeDataService);
        }

        private void SetSelectedButton(Button selectedButton)
        {

            // First, check if the clicked button is the dashboard button
            if (selectedButton.Name == "DashboardButton") // Make sure this matches your XAML button name
            {
                // Reset all buttons to default style
                ResetAllButtonStyles();
                // Don't apply the selected style to dashboard button
                return;
            }

            // For all other buttons, apply the normal selection logic
            ResetAllButtonStyles();
            selectedButton.Style = (Style)FindResource("SelectedSidebarButtonStyle");
         
        }
        private void ResetAllButtonStyles()
        {
            // Reset all buttons to default style
            AddCollegeButton.Style = (Style)FindResource("SidebarButtonStyle");
            AddProgramButton.Style = (Style)FindResource("SidebarButtonStyle");
            AddStudentButton.Style = (Style)FindResource("SidebarButtonStyle");
            ViewCollegeListButton.Style = (Style)FindResource("SidebarButtonStyle");
            ViewProgramListButton.Style = (Style)FindResource("SidebarButtonStyle");
            ViewStudentListButton.Style = (Style)FindResource("SidebarButtonStyle");
            ViewHistoryButton.Style = (Style)FindResource("SidebarButtonStyle");
            ViewSettingsButton.Style = (Style)FindResource("SidebarButtonStyle");
            DashboardButton.Style = (Style)FindResource("SidebarButtonStyle");
        }
                  

        // setup initial UI state
        private void InitializeUserInterface()
        {
            LoginStatus.Text = CurrentUser;
            MainContent.Content = _dashboardView;
            UpdateDirectory("Home");
        }

        // handle initialization errors
        private void HandleInitializationError(Exception ex)
        {
            MessageBox.Show(
                $"Error initializing application: {ex.Message}",
                "Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        // update directory path display
        private void UpdateDirectory(string page)
        {
            DirectoryText.Text = $"/{page}";
        }

        // navigation event handlers
        private void NavigateHome_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = _dashboardView;
            SetSelectedButton(sender as Button);
            UpdateDirectory("Home");
        }

        private void NavigateAddOption1_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = _addCollegeControl;
            SetSelectedButton(sender as Button);
            UpdateDirectory("Add/College");
        }

        private void NavigateAddOption2_Click(object sender, RoutedEventArgs e)
        {
            _addProgramControl.LoadCollegeCodes();
            MainContent.Content = _addProgramControl;
            SetSelectedButton(sender as Button);
            UpdateDirectory("Add/Program");
        }

        private void NavigateAddOption3_Click(object sender, RoutedEventArgs e)
        {
            _addStudentControl.LoadProgramCodes();
            MainContent.Content = _addStudentControl;
            SetSelectedButton(sender as Button);
            UpdateDirectory("Add/Student");
        }

        private void NavigateViewOption1_Click(object sender, RoutedEventArgs e)
        {
            _viewCollegesControl.LoadColleges();
            MainContent.Content = _viewCollegesControl;
            SetSelectedButton(sender as Button);
            UpdateDirectory("View/Colleges");
        }

        private void NavigateViewOption2_Click(object sender, RoutedEventArgs e)
        {
            _viewProgramsControl.LoadPrograms();
            MainContent.Content = _viewProgramsControl;
            SetSelectedButton(sender as Button);
            UpdateDirectory("View/Programs");
        }

        private void NavigateViewOption3_Click(object sender, RoutedEventArgs e)
        {
            _viewStudentControl.LoadStudents();
            MainContent.Content = _viewStudentControl;
            SetSelectedButton(sender as Button);
            UpdateDirectory("View/Students");
        }

        private void NavigateSettings_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new SettingsView();
            SetSelectedButton(sender as Button);
            UpdateDirectory("Settings");
        }

        private void NavigateHistory_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new HistoryView();
            SetSelectedButton(sender as Button);
            UpdateDirectory("History");
        }

        // handle external link clicks
        private void YouTube_Click(object sender, RoutedEventArgs e)
        {
            OpenExternalLink("https://www.youtube.com/@keane6635");
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            OpenExternalLink("https://github.com/keaneph");
        }

        private void LinkedIn_Click(object sender, RoutedEventArgs e)
        {
            OpenExternalLink("https://www.linkedin.com/in/keanepharelle/");
        }

        // open external links in default browser
        private void OpenExternalLink(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening link: {ex.Message}",
                    "External Link Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // placeholder for settings functionality
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Settings functionality coming soon!",
                "Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        
    }
}