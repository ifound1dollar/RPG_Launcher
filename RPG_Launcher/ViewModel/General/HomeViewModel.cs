using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RPG_Launcher.ViewModel.General
{
    public class HomeViewModel : ViewModelBase
    {
        private bool isHomeViewVisible = false;

        private string errorMessage = string.Empty;

        private DispatcherTimer playGameButtonTimer = new();
        private int gameProcessId = -1;
        private bool isPlayGameButtonEnabled = true;

        // Bindable properties here
        public string ErrorMessage
        {
            get => errorMessage;
            set { errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }
        public bool IsPlayGameButtonEnabled
        {
            get => isPlayGameButtonEnabled;
            set { isPlayGameButtonEnabled = value; OnPropertyChanged(nameof(IsPlayGameButtonEnabled)); }
        }



        // Commands
        public ICommand PlayGameClickedCommand { get; }
        public ICommand AccountClickedCommand { get; }



        public HomeViewModel()
        {
            playGameButtonTimer.Tick += OnPlayGameButtonTimerElapsed;

            PlayGameClickedCommand = new ViewModelCommand(ExecutePlayGameButtonCommand, CanExecutePlayGameButtonCommand);
            AccountClickedCommand = new ViewModelCommand(ExecuteAccountClickedCommand, CanExecuteAccountClickedCommand);
        }

        public override void ShowView()
        {
            isHomeViewVisible = true;
        }

        public override void HideView()
        {
            isHomeViewVisible = false;
        }



        #region Private: PlayGameClickedCommand

        private async Task ExecutePlayGameButtonCommand(object? obj)
        {
            // Immediately disable play game button and start timer, which will re-enable button if process not running.
            IsPlayGameButtonEnabled = false;
            playGameButtonTimer.Interval = TimeSpan.FromSeconds(5);
            playGameButtonTimer.Start();

            // Make API request to get a connect token, which is required to actually connect to the online service.
            (int statusCode, string response) = await LoginApiService.PlayGame();
            if (statusCode != 0)
            {
                // Play game will be re-enabled once timer elapses, so do not explicitly re-enable here.
                ErrorMessage = response;
                return;
            }

            // Create ProcessStartInfo for the future process, with different behavior based on whether development or release.
            ProcessStartInfo info;
            if (AppData.IsDevelopment)
            {
                // For development, the executable is UnrealEditor.exe. We have to pass in the game .uproject file path to run.
                info = new ProcessStartInfo
                {
                    FileName = AppData.GameInstallDirectory + "\\" + AppData.GameExecutableName + ".exe",
                    Arguments = $"\"E:\\Unreal Projects\\RPG_Main\\RPG_Main.uproject\" -game -connectToken={response}",
                    UseShellExecute = false                                                     // False ensures PID is returned.
                };
            }
            else
            {
                // For non-development (release), the executable is the compiled binary. We directly pass in the connect token.
                info = new ProcessStartInfo
                {
                    FileName = AppData.GameInstallDirectory + "\\" + AppData.GameExecutableName + ".exe",
                    Arguments = $"-connectToken={response}",
                    UseShellExecute = false
                };
            }

            // Actually start the process, setting process ID if successful, else returning to -1 and displaying error.
            try
            {
                using Process? p = Process.Start(info);
                if (p == null)
                {
                    gameProcessId = -1;
                    ErrorMessage = "Failed to start game process, please try again.";
                    return;
                }
                gameProcessId = p.Id;   // Process started successfully, so store ID.
            }
            catch (Exception ex)
            {
                gameProcessId = -1;
                ErrorMessage = ex.Message;
                return;
            }
        }

        private bool CanExecutePlayGameButtonCommand(object? obj)
        {
            // Button will be disabled by bindable property anyway, but check here also.
            return isHomeViewVisible && isPlayGameButtonEnabled;
        }

        private void OnPlayGameButtonTimerElapsed(object? sender, EventArgs e)
        {
            // If the game is still open, keep button disabled. If not open, re-enable button and stop timer.
            try
            {
                // Get process by ID and make sure the name matches.
                var proc = Process.GetProcessById(gameProcessId);
                if (proc != null && proc.ProcessName == AppData.GameExecutableName)
                {
                    // If match, simply return.
                    return;
                }
            }
            catch (Exception)
            {
                // Allow control to continue to below.
            }

            // If we do not return above, then game process is not running so focus window, re-enable button, and stop timer.
            gameProcessId = -1;
            playGameButtonTimer.Stop();

            App.Current.MainWindow.WindowState = WindowState.Normal;    // Un-minimizes if minimized.
            App.Current.MainWindow.Activate();                          // Focuses entire application.
            IsPlayGameButtonEnabled = true;
        }

        #endregion

        #region Private: AccountClickedCommand

        private void ExecuteAccountClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowAccountView();
        }

        private bool CanExecuteAccountClickedCommand(object? obj)
        {
            return isHomeViewVisible;
        }

        #endregion

    }
}
