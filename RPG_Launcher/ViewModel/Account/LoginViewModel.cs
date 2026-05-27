using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel.Account
{
    public class LoginViewModel : ViewModelBase
    {
        private bool isLoginViewVisible = false;

        private string username = string.Empty;
        private SecureString securePassword = new();
        private string errorMessage = string.Empty;

        private bool isButtonInputEnabled = true;

        public string Username
        {
            get => username;
            set
            {
                username = value;
                OnPropertyChanged(nameof(Username));
                ErrorMessage = string.Empty;
            }
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
        public ICommand LoginClickedCommand { get; }
        public ICommand ForgotPasswordClickedCommand { get; }
        public ICommand NewUserClickedCommand { get; }



        public LoginViewModel()
        {
            LoginClickedCommand = new ViewModelCommand(ExecuteLoginClickedCommand, CanExecuteLoginClickedCommand);
            ForgotPasswordClickedCommand = new ViewModelCommand((obj) => ExecuteForgotPasswordClickedCommand(Username), CanExecuteForgotPasswordClickedCommand);
            NewUserClickedCommand = new ViewModelCommand(ExecuteNewUserClickedCommand, CanExecuteNewUserClickedCommand);
        }

        public override void ShowView()
        {
            // Auto-populate username field with saved username and clear error message.
            Username = AppData.SavedUsername;
            SecurePassword.Clear();
            ErrorMessage = string.Empty;
            IsButtonInputEnabled = true;

            isLoginViewVisible = true;
        }

        public override void HideView()
        {
            isLoginViewVisible = false;
        }



        #region Private: LoginClickedCommand (async)

        private async Task ExecuteLoginClickedCommand(object? obj)
        {
            // Clear error message, then validate input.
            ErrorMessage = string.Empty;
            if (Username.Length == 0 || SecurePassword.Length == 0)
            {
                ErrorMessage = "Both input fields must be set.";
                return;
            }

            // Disable login button before awaiting to prevent button spam.
            IsButtonInputEnabled = false;

            // Call API service login method with Username and Password.
            var (StatusCode, Message) = await LoginApiService.Login(new NetworkCredential(Username, SecurePassword));
            if (StatusCode == 0)
            {
                // Code 0 means full login success, so show home view.
                MainViewModel.Instance.ShowHomeView();
                return;
            }
            else if (StatusCode == 1)
            {
                // Code 1 means account is not yet confirmed, so we must move onto confirmation code view.
                MainViewModel.Instance.ShowConfirmationCodeView(ConfirmationCodeViewModel.CodeContext.NewAccountConfirmation, Username);
                return;
            }
            else if (StatusCode == 2)
            {
                // Code 2 means account password must be reset for security reasons.
                MainViewModel.Instance.ShowConfirmationCodeView(ConfirmationCodeViewModel.CodeContext.ForgotPassword, Username);
                return;
            }
            else
            {
                // Code -1 (any other code) means generic login failure, so display login error message.
                ErrorMessage = Message;
                IsButtonInputEnabled = true;
                return;
            }
        }

        private bool CanExecuteLoginClickedCommand(object? obj)
        {
            // Login button can only be clicked if login subgrid is visible.
            return isLoginViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: ForgotPasswordClickedCommand

        private void ExecuteForgotPasswordClickedCommand(object? obj)
        {
            // Show forgot password view, auto-populating username or password field with Username.
            MainViewModel.Instance.ShowForgotPasswordView(Username);
        }

        private bool CanExecuteForgotPasswordClickedCommand(object? obj)
        {
            // Disallow click if main button is not enabled (means awaiting API response).
            return isLoginViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: NewUserClickedCommand

        private void ExecuteNewUserClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowRegisterView();
        }

        private bool CanExecuteNewUserClickedCommand(object? obj)
        {
            // Disallow click if main button is not enabled (means awaiting API response).
            return isLoginViewVisible && isButtonInputEnabled;
        }

        #endregion
    }
}
