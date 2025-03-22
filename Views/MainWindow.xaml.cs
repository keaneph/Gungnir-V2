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

        // setup initial UI state
        private void InitializeUserInterface()
        {
            LoginStatus.Text = CurrentUser;
            ProfileName.Text = CurrentUser;
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
            DirectoryText.Text = $" | /{page}";
        }

        // navigation event handlers
        private void NavigateHome_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = _dashboardView;
            UpdateDirectory("Home");
        }

        private void NavigateAddOption1_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = _addCollegeControl;
            UpdateDirectory("Add/College");
        }

        private void NavigateAddOption2_Click(object sender, RoutedEventArgs e)
        {
            _addProgramControl.LoadCollegeCodes();
            MainContent.Content = _addProgramControl;
            UpdateDirectory("Add/Program");
        }

        private void NavigateAddOption3_Click(object sender, RoutedEventArgs e)
        {
            _addStudentControl.LoadProgramCodes();
            MainContent.Content = _addStudentControl;
            UpdateDirectory("Add/Student");
        }

        private void NavigateViewOption1_Click(object sender, RoutedEventArgs e)
        {
            _viewCollegesControl.LoadColleges();
            MainContent.Content = _viewCollegesControl;
            UpdateDirectory("View/Colleges");
        }

        private void NavigateViewOption2_Click(object sender, RoutedEventArgs e)
        {
            _viewProgramsControl.LoadPrograms();
            MainContent.Content = _viewProgramsControl;
            UpdateDirectory("View/Programs");
        }

        private void NavigateViewOption3_Click(object sender, RoutedEventArgs e)
        {
            _viewStudentControl.LoadStudents();
            MainContent.Content = _viewStudentControl;
            UpdateDirectory("View/Students");
        }

        private void NavigateAbout_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new AboutView();
            UpdateDirectory("About");
        }

        private void NavigateHistory_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new HistoryView();
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