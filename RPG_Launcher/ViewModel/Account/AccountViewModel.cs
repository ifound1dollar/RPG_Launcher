using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static RPG_Launcher.ViewModel.Account.ConfirmationCodeViewModel;

namespace RPG_Launcher.ViewModel.Account
{
    public class AccountViewModel : ViewModelBase
    {
        private bool isViewVisible = false;

        private string accountUsername = string.Empty;
        private string accountEmail = string.Empty;
        private string secondaryEmail = string.Empty;
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
        public string SecondaryEmail
        {
            get => secondaryEmail;
            set { secondaryEmail = value; OnPropertyChanged(nameof(SecondaryEmail)); }
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
        public ICommand ChangeEmailClickedCommand { get; }
        public ICommand ChangePasswordClickedCommand { get; }
        public ICommand ResetMfaConfigurationClickedCommand { get; }
        public ICommand RegenerateRecoveryCodeClickedCommand { get; }
        public ICommand ChangeSecondaryEmailClickedCommand { get; }
        public ICommand LogoutClickedCommand { get; }
        public ICommand CloseClickedCommand { get; }



        public AccountViewModel()
        {
            ChangeUsernameClickedCommand = new ViewModelCommand(ExecuteChangeUsernameClickedCommand, CanExecuteChangeUsernameClickedCommand);
            ChangeEmailClickedCommand = new ViewModelCommand(ExecuteChangeEmailClickedCommand, CanExecuteChangeEmailClickedCommand);
            ChangePasswordClickedCommand = new ViewModelCommand(ExecuteChangePasswordClickedCommand, CanExecuteChangePasswordClickedCommand);

            ResetMfaConfigurationClickedCommand = new ViewModelCommand(ExecuteResetMfaConfigurationClickedCommand, CanExecuteResetMfaConfigurationClickedCommand);
            RegenerateRecoveryCodeClickedCommand = new ViewModelCommand(ExecuteRegenerateRecoveryCodeClickedCommand, CanExecuteRegenerateRecoveryCodeClickedCommand);

            ChangeSecondaryEmailClickedCommand = new ViewModelCommand(ExecuteChangeSecondaryEmailClickedCommand, CanExecuteChangeSecondaryEmailClickedCommand);

            LogoutClickedCommand = new ViewModelCommand(ExecuteLogoutClickedCommand, CanExecuteLogoutClickedCommand);
            CloseClickedCommand = new ViewModelCommand(ExecuteCloseClickedCommand, CanExecuteCloseClickedCommand);
        }

        public override void HideView()
        {
            isViewVisible = false;
        }

        public override void ShowView()
        {
            AccountUsername = AppData.SavedUsername;
            AccountEmail = AppData.SavedEmail;
            SecondaryEmail = (AppData.SecondaryEmail == string.Empty) ? "None" : AppData.SecondaryEmail;
            ErrorMessage = string.Empty;
            IsButtonInputEnabled = true;

            isViewVisible = true;
        }



        #region Private: ChangeUsernameClickedCommand

        private void ExecuteChangeUsernameClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowChangeUsernameView();
        }

        private bool CanExecuteChangeUsernameClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: ChangeEmailClickedCommand (async)

        private async Task ExecuteChangeEmailClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowSubmitNewEmailView(isForMainEmail: true);
        }

        private bool CanExecuteChangeEmailClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: ChangePasswordClickedCommand (async)

        private async Task ExecuteChangePasswordClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowChangePasswordView();
        }

        private bool CanExecuteChangePasswordClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion



        #region Private: ResetMfaConfigurationClickedCommand

        private void ExecuteResetMfaConfigurationClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowManageMfaView(ManageMfaViewModel.ManageMfaContext.ResetMfa);
        }

        private bool CanExecuteResetMfaConfigurationClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: RegenerateRecoveryCodeClickedCommand

        private void ExecuteRegenerateRecoveryCodeClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowManageMfaView(ManageMfaViewModel.ManageMfaContext.GenerateNewRecovery);
        }

        private bool CanExecuteRegenerateRecoveryCodeClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion



        #region Private: ChangeSecondaryEmailClickedCommand

        private void ExecuteChangeSecondaryEmailClickedCommand(object? obj)
        {
            // Simply show the change email view with secondary email context.
            MainViewModel.Instance.ShowSubmitNewEmailView(isForMainEmail: false);
        }

        private bool CanExecuteChangeSecondaryEmailClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion



        #region Private: LogoutClickedCommand (async)

        private void ExecuteLogoutClickedCommand(object? obj)
        {
            // Call API service logout method, which will always successfully log us out.
            _ = LoginApiService.Logout();

            // After logout, we must return to the login screen (show login window and hide main).
            MainViewModel.Instance.ShowLoginView();
        }

        private bool CanExecuteLogoutClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: BackToHomeClickedCommand

        private void ExecuteCloseClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowHomeView();
        }

        private bool CanExecuteCloseClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

    }
}
