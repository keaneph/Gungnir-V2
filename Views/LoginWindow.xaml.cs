using System.Windows;
using System.Windows.Controls;
using sis_app.Services;

namespace sis_app.Views
{
    public partial class LoginWindow : Window
    {
        private readonly UserDataService _userDataService;

        public LoginWindow()
        {
            InitializeComponent();
            // Remove the file path parameter since we're using MySQL now
            _userDataService = new UserDataService();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameTextBox.Text;
                string password = PasswordBox.Password;

                // Validate input fields
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show(
                        "Please fill in all fields.",
                        "Login Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // Validate credentials and open main window
                if (_userDataService.ValidateUser(username, password))
                {
                    MainWindow mainWindow = new MainWindow(username);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show(
                        "Invalid credentials. Please try again or register if you don't have an account.",
                        "Login Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"Login error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameTextBox.Text;
                string password = PasswordBox.Password;

                // Validate input fields
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show(
                        "Please fill in all fields.",
                        "Registration Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // Check for existing username
                if (_userDataService.UsernameExists(username))
                {
                    MessageBox.Show(
                        "Username already exists. Please choose a different username.",
                        "Registration Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // Attempt registration
                if (_userDataService.RegisterUser(username, password))
                {
                    MessageBox.Show(
                        "Registration successful! You can now sign in.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Clear input fields after success
                    UsernameTextBox.Text = "";
                    PasswordBox.Password = "";
                }
                else
                {
                    MessageBox.Show(
                        "Registration failed. Please try again.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"Registration error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // You can add username validation logic here if needed
        }
    }
}