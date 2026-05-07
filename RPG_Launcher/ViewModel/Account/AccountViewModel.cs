using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel.Account
{
    public class AccountViewModel : ViewModelBase
    {
        private bool isAccountViewVisible = false;

        private string accountUsername = string.Empty;
        private string accountEmail = string.Empty;
        private string errorMessage = string.Empty;

        private bool isButtonInputEnabled = true;

        public string AccountUsername
        {
            get => accountUsername;
            set { accountUsername = value; OnPropertyChanged(nameof(AccountUsername)); }
        }
        public string AccountEmail
        {
            get => accountEmail;
            set { accountEmail = value; OnPropertyChanged(nameof(AccountEmail)); }
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
        public ICommand ChangeUsernameClickedCommand { get; }
        public ICommand ChangePasswordClickedCommand { get; }
        public ICommand LogoutClickedCommand { get; }
        public ICommand BackToHomeClickedCommand { get; }



        public AccountViewModel()
        {
            ChangeUsernameClickedCommand = new ViewModelCommand(ExecuteChangeUsernameClickedCommand, CanExecuteChangeUsernameClickedCommand);
            ChangePasswordClickedCommand = new ViewModelCommand(ExecuteChangePasswordClickedCommand, CanExecuteChangePasswordClickedCommand);
            LogoutClickedCommand = new ViewModelCommand(ExecuteLogoutClickedCommand, CanExecuteLogoutClickedCommand);
            BackToHomeClickedCommand = new ViewModelCommand(ExecuteBackToHomeClickedCommand, CanExecuteBackToHomeClickedCommand);
        }

        public override void HideView()
        {
            isAccountViewVisible = false;
        }

        public override void ShowView()
        {
            AccountUsername = AppData.SavedUsername;
            AccountEmail = AppData.SavedEmail;
            ErrorMessage = string.Empty;
            IsButtonInputEnabled = true;

            isAccountViewVisible = true;
        }



        #region Private: ChangeUsernameClickedCommand

        private void ExecuteChangeUsernameClickedCommand(object? obj)
        {
            // Simply show the username reset view.
            MainViewModel.Instance.ShowChangeUsernameView();
        }

        private bool CanExecuteChangeUsernameClickedCommand(object? obj)
        {
            return isAccountViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: ChangePasswordClickedCommand (async)

        private async Task ExecuteChangePasswordClickedCommand(object? obj)
        {
            // Request a password reset code, then move onto confirmation code view with password change/reset context.
            IsButtonInputEnabled = false;
            int responseCode = await LoginApiService.Instance.SendConfirmationCode(accountUsername);
            IsButtonInputEnabled = true;
            if (responseCode == -1)
            {
                ErrorMessage = "Failed to perform API request to change password, please try again.";
                return;
            }

            MainViewModel.Instance.ShowConfirmationCodeView(ConfirmationCodeViewModel.CodeContext.ManualChangePassword, accountUsername);
        }

        private bool CanExecuteChangePasswordClickedCommand(object? obj)
        {
            return isAccountViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: LogoutClickedCommand (async)

        private async Task ExecuteLogoutClickedCommand(object? obj)
        {
            // Call API service logout method, which will always successfully log us out.
            await LoginApiService.Instance.Logout();

            // After logout, we must return to the login screen (show login window and hide main).
            MainViewModel.Instance.ShowLoginView();
        }

        private bool CanExecuteLogoutClickedCommand(object? obj)
        {
            return isAccountViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: BackToHomeClickedCommand

        private void ExecuteBackToHomeClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowHomeView();
        }

        private bool CanExecuteBackToHomeClickedCommand(object? obj)
        {
            return isAccountViewVisible && isButtonInputEnabled;
        }

        #endregion

    }
}
