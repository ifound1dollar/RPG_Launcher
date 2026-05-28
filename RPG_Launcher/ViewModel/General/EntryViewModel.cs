using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Account;
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
            int pingStatus = await LoginApiService.PingServer();
            if (pingStatus == -1)
            {
                MessageBrush = errorBrush;
                StatusMessage = "Failed to connect to the login API. Please try again later.";
                IsRetryButtonVisible = true;
                return;
            }

            // If no error, display success message for two seconds then move onto login logic.
            MessageBrush = infoBrush;
            StatusMessage = "Connected to login API!";
            await Task.Delay(1500);

            // Then, try to login with existing securely-stored refresh token (retrieved on Initialize() above).
            var (StatusCode, Response) = await LoginApiService.TryLoginFromRefreshToken();
            if (StatusCode == 0)
            {
                // Code 0 means successful login with full access, to move onto home screen.
                MainViewModel.Instance.ShowHomeView();
            }
            // IMPORTANT: WE PRIORITIZE PASSWORD RESET BECAUSE A SUCCESSFUL PASSWORD RESET REQUIRES A VALID EMAIL.
            // IF THE USER SUCCESSFULLY RESETS THEIR PASSWORD, IT WILL IMPLICITLY CONFIRM THE ACCOUNT EMAIL.
            else if (StatusCode == 20)
            {
                // Code 20 means password must be reset for security reasons.
                MainViewModel.Instance.ShowConfirmationCodeView(ConfirmationCodeViewModel.CodeContext.ForgotPassword, AppData.SavedUsername);
            }
            else if (StatusCode == 10)
            {
                // Code 10 means account email needs confirmation before we can fully log in.
                MainViewModel.Instance.ShowConfirmationCodeView(ConfirmationCodeViewModel.CodeContext.NewAccountConfirmation, AppData.SavedUsername);
            }
            else if (StatusCode == 30)
            {
                // Code 30 means MFA is not yet enabled, so request MFA setup and then show setup view.
                (StatusCode, Response) = await LoginApiService.SetupMfa();
                if (StatusCode != 0)
                {
                    // If status code is bad, logout and return to login view.
                    MainViewModel.Instance.ShowLoginView();
                    return;
                }

                // Else good status code means the response is our new QR code, so move onto setup screen.
                MainViewModel.Instance.ShowMfaSetupView(MainViewModel.MfaContext.InitialSetup, Response);
            }
            else
            {
                // Any other code (ex. -1 or 400/500 status code) indicates generic failure, so return to login screen.
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
