using System.Windows;
using System.Windows.Controls;
using sis_app.Services;

namespace sis_app.Views
{
    // login window handling user authentication
    public partial class LoginWindow : Window
    {
        // service for user data operations
        private readonly UserDataService _userDataService;

        // initialize window and user service
        public LoginWindow()
        {
            InitializeComponent();
            _userDataService = new UserDataService("users.csv");
        }

        // handle login button click
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // validate credentials and open main window
            if (_userDataService.ValidateUser(username, password))
            {
                MainWindow mainWindow = new MainWindow(username);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid credentials. Please try again or register if you don't have an account.",
                    "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // handle registration button click
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // validate input fields
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please fill in all fields.", "Registration Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // check for existing username
            if (_userDataService.UsernameExists(username))
            {
                MessageBox.Show("Username already exists. Please choose a different username.",
                    "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // attempt registration
            if (_userDataService.RegisterUser(username, password))
            {
                MessageBox.Show("Registration successful! You can now sign in.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // clear input fields after success
                UsernameTextBox.Text = "";
                PasswordBox.Password = "";
            }
            else
            {
                MessageBox.Show("Registration failed. Please try again.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // handle username text changes
        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // placeholder for username validation for history shi
        }
    }
}