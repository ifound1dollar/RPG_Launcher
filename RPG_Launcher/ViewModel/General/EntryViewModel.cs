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
using System.Windows.Media;

namespace RPG_Launcher.ViewModel.General
{
    public class EntryViewModel : ViewModelBase
    {
        private readonly Brush infoBrush = Brushes.CornflowerBlue;
        private readonly Brush errorBrush = Brushes.IndianRed;

        private bool isEntryViewVisible = false;
        
        private string statusMessage = string.Empty;
        private Brush messageBrush = Brushes.White;

        private bool isRetryButtonVisible = false;

        public string StatusMessage
        {
            get => statusMessage;
            set { statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }
        public Brush MessageBrush
        {
            get => messageBrush;
            set { messageBrush = value; OnPropertyChanged(nameof(MessageBrush)); }
        }
        public bool IsRetryButtonVisible
        {
            get => isRetryButtonVisible;
            set { isRetryButtonVisible = value; OnPropertyChanged(nameof(IsRetryButtonVisible)); }
        }



        public ICommand PingServerCommand { get; }

        public EntryViewModel()
        {
            PingServerCommand = new ViewModelCommand(ExecutePingServerCommand, CanExecutePingServerCommand);
        }

        public void SetMessageIsError(bool isError)
        {
            MessageBrush = (isError) ? errorBrush : infoBrush;
        }

        public override void ShowView()
        {
            StatusMessage = string.Empty;
            MessageBrush = Brushes.White;
            isRetryButtonVisible = false;

            isEntryViewVisible = true;
        }

        public override void HideView()
        {
            isEntryViewVisible = false;
        }



        #region Public: PingServerCommand (async)

        private async Task ExecutePingServerCommand(object? obj)
        {
            // Disable retry button before awaiting to prevent button spam.
            IsRetryButtonVisible = false;

            // Display loading status message.
            MessageBrush = infoBrush;
            StatusMessage = "Connecting to the login API...";

            // Ping the server 
            int pingStatus = await LoginApiService.Instance.PingServer();
            if (pingStatus == 1)
            {
                MessageBrush = errorBrush;
                StatusMessage = "Failed to connect to the login API. Please try again later.";
                IsRetryButtonVisible = true;
                return;
            }
            else if (pingStatus == -1)
            {
                MessageBrush = errorBrush;
                StatusMessage = "An unexpected error occurred when trying to connect to the login API. Please try again later.";
                IsRetryButtonVisible = true;
                return;
            }

            // If no error, display success message for two seconds then move onto login logic.
            MessageBrush = infoBrush;
            StatusMessage = "Connected to login API!";
            await Task.Delay(2000);

            // Then, try to login with existing securely-stored refresh token (retrieved on Initialize() above).
            int loginCode = await LoginApiService.Instance.TryLoginFromRefreshToken();
            if (loginCode == 0)
            {
                // Code 0 means existing token is valid, so move on to home screen.
                MainViewModel.Instance.ShowHomeView();
            }
            // IMPORTANT: WE PRIORITIZE PASSWORD RESET BECAUSE A SUCCESSFUL PASSWORD RESET REQUIRES A VALID EMAIL.
            // IF THE USER SUCCESSFULLY RESETS THEIR PASSWORD, IT WILL IMPLICITLY CONFIRM THE ACCOUNT EMAIL.
            else if (loginCode == 2)
            {
                // Code 2 means password must be reset for security reasons.
                MainViewModel.Instance.ShowVerificationCodeView(isForNewAccount: false, AppData.SavedUsername);
            }
            else if (loginCode == 1)
            {
                // Code 1 means account email needs confirmation before we can fully log in.
                MainViewModel.Instance.ShowVerificationCodeView(isForNewAccount: true, AppData.SavedUsername);
            }
            else
            {
                // Any other code (ex. -1) indicates generic failure, so return to login screen.
                MainViewModel.Instance.ShowLoginView();
            }
        }

        private bool CanExecutePingServerCommand(object? obj)
        {
            return isEntryViewVisible;
        }

        #endregion

    }
}
