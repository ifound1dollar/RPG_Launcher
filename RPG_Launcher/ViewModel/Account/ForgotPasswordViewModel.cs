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

            isForgotPasswordViewVisible = true;
        }

        public override void HideView()
        {
            isForgotPasswordViewVisible = false;
        }



        #region Private: SubmitButtonClicked

        private void ExecuteSubmitButtonClickedCommand(object? obj)
        {
            // Clear error message, then validate input.
            ErrorMessage = string.Empty;
            if (Username.Length == 0)
            {
                ErrorMessage = "Please enter a valid username or email.";
                return;
            }

            // Request a password reset code, not knowing whether successful for security reasons.
            LoginApiService.Instance.SendEmailConfirmationCode(Username);

            // Show verification code view with password reset context.
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
            return isForgotPasswordViewVisible;
        }

        #endregion
    }
}
