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
    public class ChangePasswordViewModel : ViewModelBase
    {
        private bool isViewVisible = false;

        private SecureString currentPassword = new();
        private SecureString newPassword = new();
        private string errorMessage = string.Empty;

        private bool isButtonInputEnabled = true;

        public SecureString CurrentPassword
        {
            get => currentPassword;
            set
            {
                currentPassword = value;
                OnPropertyChanged(nameof(CurrentPassword));
                // Do not clear error message here, instead clear it manually from the View code-behind. Clearing
                //  it here has the unintended side effect of clearing the error message immediately when the view
                //  clears the password box, disallowing the user to read the error.
                //ErrorMessage = string.Empty;
            }
        }
        public SecureString NewPassword
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
            get => newPassword;
            set
            {
                newPassword = value;
                OnPropertyChanged(nameof(NewPassword));

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
        public bool IsButtonInputEnabled
        {
            get => isButtonInputEnabled;
            set { isButtonInputEnabled = value; OnPropertyChanged(nameof(IsButtonInputEnabled)); }
        }



        // Commands
        public ICommand SubmitButtonClickedCommand { get; }
        public ICommand CancelButtonClickedCommand { get; }



        public ChangePasswordViewModel()
        {
            SubmitButtonClickedCommand = new ViewModelCommand(ExecuteSubmitButtonClickedCommand, CanExecuteSubmitButtonClickedCommand);
            CancelButtonClickedCommand = new ViewModelCommand(ExecuteCancelButtonClickedCommand, CanExecuteCancelButtonClickedCommand);
        }

        public override void ShowView()
        {
            CurrentPassword.Clear();
            NewPassword.Clear();
            ErrorMessage = string.Empty;
            IsButtonInputEnabled = true;

            isViewVisible = true;
        }

        public override void HideView()
        {
            isViewVisible = false;
        }



        #region Private: SubmitButtonClicked (async)

        private async Task ExecuteSubmitButtonClickedCommand(object? obj)
        {
            NetworkCredential oldCredential = new(string.Empty, CurrentPassword);
            NetworkCredential newCredential = new(string.Empty, NewPassword);

            // NOTE: We already ensure that new password and confirm password match within the code-behind.

            // Ensure password field follows standard password regex.
            string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,64}$";   // 8-64 chars, 1+ upper lower digit special (all specials)
            if (!Regex.IsMatch(newCredential.Password, pattern))
            {
                CurrentPassword.Clear();
                NewPassword.Clear();
                ErrorMessage = "New password must be 8-64 characters and include at least one uppercase letter, lowercase letter, digit, and symbol.";
                return;
            }

            // Disable submit button before awaiting to prevent button spam.
            IsButtonInputEnabled = false;

            // After validating password, we can make actual API request to reset our password. Pulls reset token from AppData automatically.
            var (StatusCode, Message) = await LoginApiService.ChangePassword(newCredential, oldCredential);
            if (StatusCode == 0)
            {
                CurrentPassword.Clear();
                NewPassword.Clear();
                MainViewModel.Instance.ShowAccountView();
                return;
            }
            else
            {
                // Any other non-success code means either unexpected error (exception) or legitimate HTTP status code error.
                CurrentPassword.Clear();
                NewPassword.Clear();
                ErrorMessage = Message;
                IsButtonInputEnabled = true;
                return;
            }

        }

        private bool CanExecuteSubmitButtonClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: CancelButtonClicked

        private void ExecuteCancelButtonClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowAccountView();
        }

        private bool CanExecuteCancelButtonClickedCommand(object? obj)
        {
            // Disallow click if main button is not enabled (means awaiting API response).
            return isViewVisible && isButtonInputEnabled;
        }



        #endregion

    }
}
