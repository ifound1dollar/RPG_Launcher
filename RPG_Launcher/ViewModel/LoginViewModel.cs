using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
        private string username = string.Empty;
        private SecureString securePassword = new();
        private string errorMessage = string.Empty;

        private bool isLoginViewVisible = false;

        public string Username
        {
            get => username;
            private set { username = value; OnPropertyChanged(nameof(Username)); }
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
            private set { securePassword = value; OnPropertyChanged(nameof(SecurePassword)); }
        }
        public string ErrorMessage
        {
            get => errorMessage;
            private set { errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }
        public bool IsLoginViewVisible
        {
            get => isLoginViewVisible;
            private set { isLoginViewVisible = value; OnPropertyChanged(nameof(IsLoginViewVisible)); }
        }

        // Explicit setter for password so we can keep LoginPassword private set.
        public void SetSecurePassword(SecureString password)
        {
            SecurePassword = password;
        }



        // Commands
        public ICommand LoginClickedCommand { get; }
        public ICommand ForgotPasswordClickedCommand { get; }



        public LoginViewModel()
        {
            LoginClickedCommand = new ViewModelCommand(ExecuteLoginClickedCommand, CanExecuteLoginClickedCommand);
            ForgotPasswordClickedCommand = new ViewModelCommand((obj) => ExecuteForgotPasswordClickedCommand(Username), CanExecuteForgotPasswordClickedCommand);
        }

        public void ShowLoginView()
        {
            // Auto-populate username field with saved username.
            Username = AppData.SavedUsername;

            IsLoginViewVisible = true;
        }

        public void HideLoginView()
        {
            IsLoginViewVisible = false;
        }



        #region Private: LoginClickedCommand

        private void ExecuteLoginClickedCommand(object? obj)
        {
            // Clear error message, then validate input.
            ErrorMessage = string.Empty;
            if (Username.Length < 0 || SecurePassword.Length < 0)
            {
                ErrorMessage = "Both input fields must be set.";
                return;
            }

            // Call API service login method with Username and Password.
            bool loginSuccess = LoginApiService.Instance.Login(new System.Net.NetworkCredential(Username, SecurePassword));
            if (loginSuccess)
            {
                MainViewModel.Instance.ShowHomeViewCommand.Execute(obj);
                return;
            }

            // Display login error if unsuccessful
            ErrorMessage = "Login failed, please try again.";
        }

        private bool CanExecuteLoginClickedCommand(object? obj)
        {
            // Login button can only be clicked if login subgrid is visible.
            return IsLoginViewVisible;
        }

        #endregion

        #region Private: ForgotPasswordClickedCommand

        private void ExecuteForgotPasswordClickedCommand(string username)
        {
            throw new NotImplementedException();
        }

        private bool CanExecuteForgotPasswordClickedCommand(object? obj)
        {
            return IsLoginViewVisible;
        }

        #endregion


    }
}
