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
    public class SubmitMfaCodeViewModel : ViewModelBase
    {
        private bool isViewVisible = false;
        private MainViewModel.MfaContext context;

        private string mfaCode = string.Empty;
        private string errorMessage = string.Empty;

        private bool isUseRecoveryCodeButtonVisible = false;

        private bool isButtonInputEnabled = true;

        public string MfaCode
        {
            get => mfaCode;
            set
            {
                mfaCode = value;
                OnPropertyChanged(nameof(MfaCode));
                ErrorMessage = string.Empty;
            }
        }
        public string ErrorMessage
        {
            get => errorMessage;
            set { errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }
        public bool IsUseRecoveryCodeButtonVisible
        {
            get => isUseRecoveryCodeButtonVisible;
            set { isUseRecoveryCodeButtonVisible = value; OnPropertyChanged(nameof(IsUseRecoveryCodeButtonVisible)); }
        }
        public bool IsButtonInputEnabled
        {
            get => isButtonInputEnabled;
            set { isButtonInputEnabled = value; OnPropertyChanged(nameof(IsButtonInputEnabled)); }
        }



        // Commands
        public ICommand SubmitButtonClickedCommand { get; }
        public ICommand UseRecoveryCodeButtonClickedCommand { get; }
        public ICommand CancelButtonClickedCommand { get; }



        public SubmitMfaCodeViewModel()
        {
            SubmitButtonClickedCommand = new ViewModelCommand(ExecuteSubmitButtonClickedCommand, CanExecuteSubmitButtonClickedCommand);
            UseRecoveryCodeButtonClickedCommand = new ViewModelCommand(ExecuteUseRecoveryCodeButtonClickedCommand, CanExecuteUseRecoveryCodeButtonClickedCommand);
            CancelButtonClickedCommand = new ViewModelCommand(ExecuteCancelButtonClickedCommand, CanExecuteCancelButtonClickedCommand);
        }

        public void SetViewContext(MainViewModel.MfaContext context)
        {
            this.context = context;

            // Only show use recovery code option if trying to log in.
            if (context == MainViewModel.MfaContext.MfaLogin)
            {
                IsUseRecoveryCodeButtonVisible = true;
            }
            else
            {
                IsUseRecoveryCodeButtonVisible = false;
            }
        }

        public override void ShowView()
        {
            MfaCode = string.Empty;
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
            // Clear error message, then validate input.
            ErrorMessage = string.Empty;
            if (MfaCode.Length != 6)
            {
                ErrorMessage = "MFA code must be length 6.";
                return;
            }

            // Disable send code button before awaiting to prevent button spam.
            IsButtonInputEnabled = false;

            // Determine which API call to make based on context (submit MFA code is different from verifying MFA setup).
            int StatusCode; string Response = string.Empty;
            switch (context)
            {
                case MainViewModel.MfaContext.MfaLogin:
                    {
                        // Login MFA submission will behave differently from MFA setup.
                        (StatusCode, Response) = await LoginApiService.SubmitMfaCodeForLogin(MfaCode);
                        if (StatusCode == 0)
                        {
                            // If status code is good, so we are now fully logged in and can move onto home view.
                            _ = LoginApiService.PingInLauncher();       // Notify the API that we are logged into the launcher right away.
                            MainViewModel.Instance.ShowHomeView();
                            return;
                        }
                        else if (StatusCode >= 1 && StatusCode < 100)
                        {
                            // If code 1, 10, 20, or 30 (success response code but bad account state), return to login view.
                            _ = LoginApiService.Logout();
                            MainViewModel.Instance.ShowReturnToLoginView(true,
                                "Correct MFA code submitted, but unexpected account state detected in response. Please log in again.");
                            return;
                        }
                        break;
                    }
                case MainViewModel.MfaContext.InitialSetup:
                case MainViewModel.MfaContext.RecoverySetup:
                case MainViewModel.MfaContext.ManualSetup:
                    {
                        // All setup tasks perform a different API call.
                        (StatusCode, Response) = await LoginApiService.VerifyMfaSetup(MfaCode);
                        if (StatusCode == 0)
                        {
                            // If good status code, then API returned a recovery code that must be displayed.
                            MainViewModel.Instance.ShowRecoveryCodeDisplayView(context, Response);
                            return;
                        }
                        else if (StatusCode >= 1 && StatusCode < 100)
                        {
                            // If code 1, 10, 20, or 30 (success response code but bad account state), return to login view.
                            _ = LoginApiService.Logout();
                            MainViewModel.Instance.ShowReturnToLoginView(true,
                                "Multi-factor authentication set up successfully, but unexpected account state detected in response. Please log in again.");
                            return;
                        }
                        break;
                    }
            }

            // If no explicit return in switch statement, just display error message from response (400 or 500 code).
            ErrorMessage = Response;
            IsButtonInputEnabled = true;
        }

        private bool CanExecuteSubmitButtonClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: UseRecoveryCodeButtonClicked

        private void ExecuteUseRecoveryCodeButtonClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowRecoverMfaView(MainViewModel.MfaContext.RecoverySetup);
        }

        private bool CanExecuteUseRecoveryCodeButtonClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: CancelButtonClicked

        private void ExecuteCancelButtonClickedCommand(object? obj)
        {
            // Determine what view to return to based on context.
            switch (context)
            {
                case MainViewModel.MfaContext.InitialSetup:
                case MainViewModel.MfaContext.RecoverySetup:
                case MainViewModel.MfaContext.MfaLogin:
                    {
                        // Cancelling on non-manual setup OR login means we do not have access so logout and return to login.
                        _ = LoginApiService.Logout();   // Do not await logout.

                        MainViewModel.Instance.ShowLoginView();
                        break;
                    }
                case MainViewModel.MfaContext.ManualSetup:
                    {
                        // Cancelling manual setup means we DO have access, so return to account view.
                        MainViewModel.Instance.ShowAccountView();
                        break;
                    }
            }
        }

        private bool CanExecuteCancelButtonClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

    }
}
