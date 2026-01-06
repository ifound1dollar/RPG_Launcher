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
            // Initialize in-memory application state data. This will load any secure data into memory.
            AppStateData.Initialize();

            // TEMP: INITIALIZE TempLoginApiImitator
            TempLoginApiImitator.Initialize();

            // Create MainWindow, but do not show anything yet.
            var mainWindow = new MainWindow();

            // First, try to login with existing securely-stored login token (retrieved on Initialize() above).
            if (LoginApiService.Instance.TryLoginFromRefreshToken(AppStateData.RefreshToken))
            {
                // If our existing token is valid, hide login subgrid and show main subgrid, then show entire window.
                mainWindow.MainViewModel.IsLoginSubgridVisible = false;
                mainWindow.MainViewModel.IsMainSubgridVisible = true;
                mainWindow.Show();
                return;
            }

            // If we cannot auto-login with saved refresh token, destroy the token and show login window.
            AppStateData.RefreshToken = string.Empty;
            mainWindow.MainViewModel.IsMainSubgridVisible = false;
            mainWindow.MainViewModel.IsLoginSubgridVisible = true;
            mainWindow.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // De-initialize in-memory application state data on exit (ex. to securely write refresh token to file).
            // NOTE: This will not be called if the application closes unexpectedly (ex. crash, power outage, force
            //  closed with TaskManager, etc.). If the application does not close gracefully, the refresh token
            //  in the file will be expired and the user will need to explicitly log in the next time that the
            //  application is opened.
            // Refresh token is only read from file once on application startup, and once on application shutdown.

            AppStateData.Deinitialize();
        }
    }

}
