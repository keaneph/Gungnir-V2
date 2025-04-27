using System.Configuration;
using System.Data;
using System.Windows;
using sis_app.Services;

namespace sis_app
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static DatabaseService DatabaseService { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string connectionString = "Server=localhost;Database=accademia;Uid=root;Pwd=root;";
            DatabaseService = new DatabaseService(connectionString);
        }
    }

}