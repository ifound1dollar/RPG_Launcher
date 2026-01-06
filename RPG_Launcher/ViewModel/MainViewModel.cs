using RPG_Launcher.Model;
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
        // TEMP BOOL JUST FOR EASY TESTING
        public bool TempTrue { get; } = true;
        // TEMP

        private string loginUsername            = string.Empty;
        private SecureString loginPassword      = new();
        private string loginErrorMessage        = string.Empty;
        private bool isLoginSubgridVisible      = false;
        private bool isMainSubgridVisible       = false;

        // View element properties
        public string LoginUsername
        {
            get => loginUsername;
            set { loginUsername = value; OnPropertyChanged(nameof(LoginUsername)); }
        }
        public SecureString LoginPassword
        {
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



        #region Private: LoginClickedCommand

        private void ExecuteLoginClickedCommand(object? obj)
        {
            // Clear error message, then validate input.
            LoginErrorMessage = string.Empty;
            if (LoginUsername.Length < 3 || LoginPassword.Length < 3)
            {
                LoginErrorMessage = "Both input fields must be set.";
                return;
            }

            // Call API service login method with Username and Password.
            bool loginSuccess = LoginApiService.Instance.Login(new System.Net.NetworkCredential(LoginUsername, LoginPassword));
            //Password.Clear();   // Immediately clear password once used.
            if (loginSuccess)
            {
                // Set login subgrid invisible, then show main subgrid.
                IsLoginSubgridVisible = false;
                IsMainSubgridVisible = true;
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
            IsLoginSubgridVisible = true;
            IsMainSubgridVisible = false;

            // TODO: WE MUST FIGURE OUT HOW TO CLEAR SECUREPASSWORD FIELD. IT CANNOT BE AUTO-POPULATED.
        }

        private bool CanExecuteLogoutClickedCommand(object? obj)
        {
            return IsMainSubgridVisible;
        }


        #endregion
    }
}
