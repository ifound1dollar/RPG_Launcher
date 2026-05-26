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

        private string usernameOrEmail = string.Empty;
        private string errorMessage = string.Empty;

        private bool isButtonInputEnabled = true;

        public string UsernameOrEmail
        {
            get => usernameOrEmail;
            set
            {
                usernameOrEmail = value;
                OnPropertyChanged(nameof(UsernameOrEmail));
                ErrorMessage = string.Empty;
            }
        }
        public string ErrorMessage
        {
            get => errorMessage;
            set { errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }
        public bool IsButtonInputEnabled
        {
            get => isButtonInputEnabled;
            set { isButtonInputEnabled = value; OnPropertyChanged(nameof(IsButtonInputEnabled)); }
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
            UsernameOrEmail = string.Empty;
            ErrorMessage = string.Empty;
            IsButtonInputEnabled = true;

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
            if (UsernameOrEmail.Length == 0)
            {
                ErrorMessage = "Please enter a valid username or email.";
                return;
            }

            // Disable send code button before awaiting to prevent button spam.
            IsButtonInputEnabled = false;

            // Request a password reset code, not knowing whether successful for security reasons.
            int responseCode = await LoginApiService.Instance.ForgotPassword(UsernameOrEmail);
            if (responseCode == -1)
            {
                ErrorMessage = "Failed to perform API request, please try again.";
                return;
            }

            // Else, status code was good so show verification code view with password reset context.
            MainViewModel.Instance.ShowConfirmationCodeView(ConfirmationCodeViewModel.CodeContext.ForgotPassword, UsernameOrEmail);
        }

        private bool CanExecuteSubmitButtonClickedCommand(object? obj)
        {
            return isForgotPasswordViewVisible && isButtonInputEnabled;
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
            return isForgotPasswordViewVisible && isButtonInputEnabled;
        }

        #endregion
    }
}
