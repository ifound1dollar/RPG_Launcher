using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            int statusCode = 0; string response = Guid.NewGuid().ToString();    // TODO: ACTUALLY IMPLEMENT API REQUEST
            if (statusCode != 0)
            {
                ErrorMessage = response;
                return;
            }

            try
            {
                // Create ProcessStartInfo with our path and executable name, setting UseShellExecute to false to ensure PID is returned.
                // TODO: UPDATE WITH COMPILED BINARY LAUNCH INSTEAD OF UNREAL EDITOR
                ProcessStartInfo info = new()
                {
                    FileName = AppData.PathToExecutable + AppData.ExecutableName + ".exe",  // IS CURRENTLY UNREAL EDITOR
                    Arguments = $"\"E:\\Unreal Projects\\RPG_Main\\RPG_Main.uproject\" -game -connectToken={response}",
                    UseShellExecute = false                                                 // False ensures PID is returned.
                };

                // Actually start the process, resetting process ID to -1 if failure then returning.
                using Process? p = Process.Start(info);
                if (p == null)
                {
                    gameProcessId = -1;
                    ErrorMessage = "Failed to start game process, please try again.";
                    return;
                }

                // Else process started successfully, so store ID.
                gameProcessId = p.Id;
            }
            catch (Exception ex)
            {
                // If exception, ensure the process ID is reset to -1 and display error message.
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
                if (proc == null || proc.ProcessName != AppData.ExecutableName)
                {
                    // If mismatch (not running), stop timer and enable button.
                    gameProcessId = -1;
                    playGameButtonTimer.Stop();
                    IsPlayGameButtonEnabled = true;
                }
            }
            catch (Exception)
            {
                // Throws exception if not running, so re-enable button and stop timer.
                gameProcessId = -1;
                playGameButtonTimer.Stop();
                IsPlayGameButtonEnabled = true;
            }
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
