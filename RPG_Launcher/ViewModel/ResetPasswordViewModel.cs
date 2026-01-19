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

namespace RPG_Launcher.ViewModel
{
    public class ResetPasswordViewModel : ViewModelBase
    {
        private bool isResetPasswordViewVisible = false;

        private string targetUser = string.Empty;
        private SecureString securePassword = new();
        private string errorMessage = string.Empty;

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
            set { securePassword = value; OnPropertyChanged(nameof(SecurePassword)); }
        }
        public string ErrorMessage
        {
            get => errorMessage;
            set { errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }



        // Commands
        public ICommand SubmitButtonClickedCommand { get; }
        public ICommand CancelButtonClickedCommand { get; }



        public ResetPasswordViewModel()
        {
            SubmitButtonClickedCommand = new ViewModelCommand(ExecuteSubmitButtonClickedCommand, CanExecuteSubmitButtonClickedCommand);
            CancelButtonClickedCommand = new ViewModelCommand(ExecuteCancelButtonClickedCommand, CanExecuteCancelButtonClickedCommand);
        }

        public override void ShowView()
        {
            TargetUser = string.Empty;
            SecurePassword.Clear();
            ErrorMessage = string.Empty;

            isResetPasswordViewVisible = true;
        }

        public override void HideView()
        {
            isResetPasswordViewVisible = false;
        }



        #region Private: SubmitButtonClicked

        private void ExecuteSubmitButtonClickedCommand(object? obj)
        {
            NetworkCredential credential = new(TargetUser, SecurePassword); // TargetUser will always be valid username here.

            // NOTE: We already ensure that both password fields match within the code-behind.

            // Ensure password field follows standard password regex.
            string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$";
            if (!Regex.IsMatch(credential.Password, pattern))
            {
                ErrorMessage = "Password must be minimum 8 characters and include at least one uppercase letter, lowercase letter, number, and symbol.";
                return;
            }

            // After validating password, we can make actual API request to reset our password.
            int resetCode = LoginApiService.Instance.ResetPasswordFromToken(credential);    // Pulls reset token from AppData automatically.
            if (resetCode == 0)
            {
                // Code 0 indicates success, password has been reset and we must now log in again.
                MainViewModel.Instance.ShowReturnToLoginView(isError: false, "Password reset successfully.");

                // Also updated saved username to the newly-reset account's username.
                AppData.SavedUsername = TargetUser;
                return;
            }
            else if (resetCode == 1)
            {
                ErrorMessage = "Invalid input state, please try again.";
                return;
            }
            else if (resetCode == 2)
            {
                ErrorMessage = "New password cannot be the same as old password.";
                return;
            }
            else
            {
                // Reset failure (code -1) indicates expired reset token, so force return to login view.
                MainViewModel.Instance.ShowReturnToLoginView(isError: true, "Password reset failed, please try again.");
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
            LoginApiService.Instance.CancelPasswordReset();
            MainViewModel.Instance.ShowLoginView();
        }

        private bool CanExecuteCancelButtonClickedCommand(object? obj)
        {
            return isResetPasswordViewVisible;
        }



        #endregion
    }
}
