using RPG_Launcher.Model;
using RPG_Launcher.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private string windowTitle              = string.Empty;
        private string loginUsername            = string.Empty;
        private SecureString loginPassword      = new();
        private string loginErrorMessage        = string.Empty;
        private bool isLoginSubgridVisible      = false;
        private bool isMainSubgridVisible       = false;

        // View element properties
        public string WindowTitle
        {
            get => windowTitle;
            set { windowTitle = value; OnPropertyChanged(nameof(WindowTitle)); }
        }
        public string LoginUsername
        {
            get => loginUsername;
            set { loginUsername = value; OnPropertyChanged(nameof(LoginUsername)); }
        }
        public SecureString LoginPassword
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
            get => loginPassword;
            set { loginPassword = value; OnPropertyChanged(nameof(LoginPassword)); }
        }
        public string LoginErrorMessage
        {
            get => loginErrorMessage;
            set { loginErrorMessage = value; OnPropertyChanged(nameof(LoginErrorMessage)); }
        }
        public bool IsLoginSubgridVisible
        {
            get => isLoginSubgridVisible;
            set { isLoginSubgridVisible = value; OnPropertyChanged(nameof(IsLoginSubgridVisible)); }
        }
        public bool IsMainSubgridVisible
        {
            get => isMainSubgridVisible;
            set { isMainSubgridVisible = value; OnPropertyChanged(nameof(IsMainSubgridVisible)); }
        }

        // Commands
        public ICommand LoginClickedCommand { get; }
        public ICommand ForgotPasswordClickedCommand { get; }
        public ICommand LogoutClickedCommand { get; }
        


        public MainViewModel()
        {
            LoginClickedCommand = new ViewModelCommand(ExecuteLoginClickedCommand, CanExecuteLoginClickedCommand);
            ForgotPasswordClickedCommand = new ViewModelCommand((obj) => ExecuteForgotPasswordClickedCommand(LoginUsername), CanExecuteForgotPasswordClickedCommand);

            LogoutClickedCommand = new ViewModelCommand(ExecuteLogoutClickedCommand, CanExecuteLogoutClickedCommand);
        }

        public void ShowLoginSubgrid()
        {
            // Populate username field with saved username.
            LoginUsername = AppData.SavedUsername;

            IsLoginSubgridVisible = true;
            IsMainSubgridVisible = false;
        }

        public void HideLoginSubgrid()
        {
            IsMainSubgridVisible = true;
            IsLoginSubgridVisible = false;
        }



        #region Private: LoginClickedCommand

        private void ExecuteLoginClickedCommand(object? obj)
        {
            // Clear error message, then validate input.
            LoginErrorMessage = string.Empty;
            if (LoginUsername.Length < 0 || LoginPassword.Length < 0)
            {
                LoginErrorMessage = "Both input fields must be set.";
                return;
            }

            // Call API service login method with Username and Password.
            bool loginSuccess = LoginApiService.Instance.Login(new System.Net.NetworkCredential(LoginUsername, LoginPassword));
            if (loginSuccess)
            {
                HideLoginSubgrid();
                return;
            }

            // Display login error if unsuccessful
            LoginErrorMessage = "Login failed, please try again.";
        }

        private bool CanExecuteLoginClickedCommand(object? obj)
        {
            // Login button can only be clicked if login subgrid is visible.
            return IsLoginSubgridVisible;
        }

        #endregion

        #region Private: ForgotPasswordClickedCommand

        private void ExecuteForgotPasswordClickedCommand(string username)
        {
            throw new NotImplementedException();
        }

        private bool CanExecuteForgotPasswordClickedCommand(object? obj)
        {
            return true;
        }

        #endregion

        #region Private: LogoutClickedCommand

        private void ExecuteLogoutClickedCommand(object? obj)
        {
            // Call API service logout method, which will always successfully log us out.
            LoginApiService.Instance.Logout();

            // After logout, we must return to the login screen (show login window and hide main).
            ShowLoginSubgrid();
        }

        private bool CanExecuteLogoutClickedCommand(object? obj)
        {
            // Can only logout if main subgrid is visible (already logged in).
            return IsMainSubgridVisible;
        }


        #endregion
    }
}
