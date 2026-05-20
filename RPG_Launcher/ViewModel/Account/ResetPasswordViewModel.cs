using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel.Account
{
    public class ResetPasswordViewModel : ViewModelBase
    {
        private bool isResetPasswordViewVisible = false;

        private bool isForgotPasswordContext = false;

        private string targetUser = string.Empty;
        private SecureString securePassword = new();
        private string errorMessage = string.Empty;

        private bool isSubmitButtonEnabled = true;

        public string TargetUser
        {
            get => targetUser;
            set { targetUser = value; OnPropertyChanged(nameof(TargetUser)); }
        }
        public SecureString SecurePassword
        {
            // IMPORTANT: This is not directly bound to via the MVVM pattern. SecureStrings do not support
            //  binding by default, so we had to shift behavior to the code-behind. Whenever the PasswordBox
            //  field is updated, we receive an update directly from the code-behind rather than binding
            //  it directly to this ViewModel. This is necessary to allow PasswordBox.Clear() to be called,
            //  which must be done the instant that the login button is pressed. We still process the login
            //  here, pulling directly from this variable (which IMPORTANTLY is automatically updated via
            //  the code-behind whenever the PasswordBox is changed in the UI).
            // The only real difference is how this field is updated; the code-behind has more
            //  responsibility in this case and directly controls what this value reads, rather than the
            //  other way around.
            get => securePassword;
            set
            {
                securePassword = value;
                OnPropertyChanged(nameof(SecurePassword));

                // Do not clear error message here, instead clear it manually from the View code-behind. Clearing
                //  it here has the unintended side effect of clearing the error message immediately when the view
                //  clears the password box, disallowing the user to read the error.
                //ErrorMessage = string.Empty;
            }
        }
        public string ErrorMessage
        {
            get => errorMessage;
            set { errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }
        public bool IsSubmitButtonEnabled
        {
            get => isSubmitButtonEnabled;
            set { isSubmitButtonEnabled = value; OnPropertyChanged(nameof(IsSubmitButtonEnabled)); }
        }



        // Commands
        public ICommand SubmitButtonClickedCommand { get; }
        public ICommand CancelButtonClickedCommand { get; }



        public ResetPasswordViewModel()
        {
            SubmitButtonClickedCommand = new ViewModelCommand(ExecuteSubmitButtonClickedCommand, CanExecuteSubmitButtonClickedCommand);
            CancelButtonClickedCommand = new ViewModelCommand(ExecuteCancelButtonClickedCommand, CanExecuteCancelButtonClickedCommand);
        }

        public void SetViewContext(bool isForgotPassword)
        {
            isForgotPasswordContext = isForgotPassword;
        }

        public override void ShowView()
        {
            TargetUser = string.Empty;
            SecurePassword.Clear();
            ErrorMessage = string.Empty;
            IsSubmitButtonEnabled = true;

            isResetPasswordViewVisible = true;
        }

        public override void HideView()
        {
            isResetPasswordViewVisible = false;
        }



        #region Private: SubmitButtonClicked (async)

        private async Task ExecuteSubmitButtonClickedCommand(object? obj)
        {
            NetworkCredential credential = new(TargetUser, SecurePassword); // TargetUser will always be valid username here.

            // NOTE: We already ensure that both password fields match within the code-behind.

            // Ensure password field follows standard password regex.
            string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,64}$";   // 8-64 chars, 1+ upper lower digit special (all specials)
            if (!Regex.IsMatch(credential.Password, pattern))
            {
                SecurePassword.Clear();
                ErrorMessage = "Password must be 8-64 characters and include at least one uppercase letter, lowercase letter, digit, and symbol.";
                return;
            }

            // Disable submit button before awaiting to prevent button spam.
            IsSubmitButtonEnabled = false;

            // After validating password, we can make actual API request to reset our password. Pulls reset token from AppData automatically.
            var (StatusCode, Message) = await LoginApiService.Instance.ResetPasswordFromToken(credential);
            if (StatusCode == 0)
            {
                SecurePassword.Clear();

                // Code 0 indicates success, password has been reset and we must now log in again.
                MainViewModel.Instance.ShowReturnToLoginView(isError: false, Message);

                // Also updated saved username to the newly-reset account's username.
                AppData.SavedUsername = TargetUser;
                return;
            }
            else
            {
                // Any other non-success code means either unexpected error (exception) or legitimate HTTP status code error.
                SecurePassword.Clear();
                ErrorMessage = Message;
                IsSubmitButtonEnabled = true;
                return;
            }

        }

        private bool CanExecuteSubmitButtonClickedCommand(object? obj)
        {
            return isResetPasswordViewVisible;
        }

        #endregion

        #region Private: CancelButtonClicked

        private void ExecuteCancelButtonClickedCommand(object? obj)
        {
            // Cancel button can be clicked at any time to cancel the current reset process and return
            //  to login. Note that an enforced password reset (flag set in database) will bring the
            //  user back to the reset password screen every time.

            AppData.PasswordResetToken = string.Empty;
            
            // If for manual password change, return to account view if currenly logged in.
            if (!isForgotPasswordContext && !string.IsNullOrEmpty(AppData.RefreshToken))
            {
                MainViewModel.Instance.ShowAccountView();
            }
            else
            {
                MainViewModel.Instance.ShowLoginView();
            }
        }

        private bool CanExecuteCancelButtonClickedCommand(object? obj)
        {
            // Disallow click if main button is not enabled (means awaiting API response).
            return isResetPasswordViewVisible && isSubmitButtonEnabled;
        }



        #endregion
    }
}
