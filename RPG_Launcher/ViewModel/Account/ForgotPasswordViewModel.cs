using RPG_Launcher.Model;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel.Account
{
    public class ForgotPasswordViewModel : ViewModelBase
    {
        private bool isForgotPasswordViewVisible = false;

        private string username = string.Empty;
        private string errorMessage = string.Empty;

        private bool isSendCodeButtonEnabled = true;

        public string Username
        {
            get => username;
            set { username = value; OnPropertyChanged(nameof(Username)); }
        }
        public string ErrorMessage
        {
            get => errorMessage;
            set { errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }
        public bool IsSendCodeButtonEnabled
        {
            get => isSendCodeButtonEnabled;
            set { isSendCodeButtonEnabled = value; OnPropertyChanged(nameof(IsSendCodeButtonEnabled)); }
        }



        // Commands
        public ICommand SubmitButtonClickedCommand { get; }
        public ICommand ReturnToLoginClickedCommand { get; }



        public ForgotPasswordViewModel()
        {
            SubmitButtonClickedCommand = new ViewModelCommand(ExecuteSubmitButtonClickedCommand, CanExecuteSubmitButtonClickedCommand);
            ReturnToLoginClickedCommand = new ViewModelCommand(ExecuteReturnToLoginClickedCommand, CanExecuteReturnToLoginClickedCommand);
        }

        public override void ShowView()
        {
            Username = string.Empty;
            ErrorMessage = string.Empty;
            IsSendCodeButtonEnabled = true;

            isForgotPasswordViewVisible = true;
        }

        public override void HideView()
        {
            isForgotPasswordViewVisible = false;
        }



        #region Private: SubmitButtonClicked (async)

        private async Task ExecuteSubmitButtonClickedCommand(object? obj)
        {
            // Clear error message, then validate input.
            ErrorMessage = string.Empty;
            if (Username.Length == 0)
            {
                ErrorMessage = "Please enter a valid username or email.";
                return;
            }

            // Disable send code button before awaiting to prevent button spam.
            IsSendCodeButtonEnabled = false;

            // Request a password reset code, not knowing whether successful for security reasons.
            int responseCode = await LoginApiService.Instance.SendConfirmationCode(Username);
            if (responseCode == -1)
            {
                ErrorMessage = "Failed to perform API request, please try again.";
                return;
            }

            // Else, status code was good so show verification code view with password reset context.
            MainViewModel.Instance.ShowVerificationCodeView(isForNewAccount: false, Username);
        }

        private bool CanExecuteSubmitButtonClickedCommand(object? obj)
        {
            return isForgotPasswordViewVisible;
        }

        #endregion

        #region Private: ReturnToLoginClicked

        private void ExecuteReturnToLoginClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowLoginView();
        }

        private bool CanExecuteReturnToLoginClickedCommand(object? obj)
        {
            // Disallow click if main button is not enabled (means awaiting API response).
            return isForgotPasswordViewVisible && isSendCodeButtonEnabled;
        }

        #endregion
    }
}
