using RPG_Launcher.Model;
using RPG_Launcher.Util;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace RPG_Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Initialize in-memory application state data. This will load any secure data into memory (ex. refresh token).
            AppData.Initialize();

            // TEMP: INITIALIZE TempLoginApiImitator
            TempLoginApiImitator.Initialize();

            // Create MainWindow, but do not show anything yet. Also set window title from application version.
            var mainWindow = new MainWindow();
            mainWindow.MainVM.WindowTitle = ("VERSION " + AppData.Version);

            // First, try to login with existing securely-stored login token (retrieved on Initialize() above).
            if (LoginApiService.Instance.TryLoginFromRefreshToken(AppData.RefreshToken))
            {
                // If our existing token is valid, hide login subgrid and show main subgrid, then show entire window.
                mainWindow.MainVM.ShowHomeViewCommand.Execute(sender);
                mainWindow.Show();
            }
            else
            {
                // If automatic login with refresh token failed, show login window.
                mainWindow.MainVM.ShowLoginViewCommand.Execute(sender);
                mainWindow.Show();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // De-initialize in-memory application state data on exit (ex. to securely write refresh token to file).
            // NOTE: This will not be called if the application closes unexpectedly (ex. crash, power outage, force
            //  closed with TaskManager, etc.). If the application does not close gracefully, the refresh token
            //  in the file will be expired and the user will need to explicitly log in the next time that the
            //  application is opened.

            //AppData.Deinitialize();
        }
    }

}
