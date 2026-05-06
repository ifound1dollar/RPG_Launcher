using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel;
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
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            // Initialize in-memory application state data. This will load any secure data into memory (ex. refresh token).
            AppData.Initialize();

            // Create MainWindow, but do not show anything yet. Also set window title from application version.
            var mainWindow = new MainWindow();
            mainWindow.MainVM.WindowTitle = ("VERSION " + AppData.Version);

            // Load and display the entry view, which pings the server and handles entry logic.
            mainWindow.MainVM.ShowEntryView();
            mainWindow.Show();                      // Actually shows the application window.
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            // De-initialize in-memory application state data on exit (ex. to securely write refresh token to file).
            // NOTE: This will not be called if the application closes unexpectedly (ex. crash, power outage, force
            //  closed with TaskManager, etc.). If the application does not close gracefully, the refresh token
            //  in the file will be expired and the user will need to explicitly log in the next time that the
            //  application is opened.

            // We do not need to await this, and should not because we want to allow the launcher to exit immediately.
            _ = LoginApiService.Instance.NotifyLauncherExit();

            //AppData.Deinitialize();
        }
    }

}
