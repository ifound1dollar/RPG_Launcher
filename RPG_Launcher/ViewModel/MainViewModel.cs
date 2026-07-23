using QRCoder;
using QRCoder.Xaml;
using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Account;
using RPG_Launcher.ViewModel.Base;
using RPG_Launcher.ViewModel.General;
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
        public enum MfaContext { None, InitialSetup, ManualSetup, RecoverySetup, MfaLogin }

        public static MainViewModel Instance { get; private set; } = null!;     // STATIC SINGLETON

        private string windowTitle;
        private ViewModelBase currentViewModel;

        private bool isSettingsViewOpen = false;
        private ViewModelBase settingsRememberViewModel;

        // View element properties
        public string WindowTitle
        {
            get => windowTitle;
            set { windowTitle = value; OnPropertyChanged(nameof(WindowTitle)); }
        }
        public ViewModelBase CurrentViewModel
        {
            get => currentViewModel;
            private set { currentViewModel = value; OnPropertyChanged(nameof(CurrentViewModel)); }
        }

        // Sub-viewmodels (read-only)
        private SettingsViewModel SettingsVM { get; } = new SettingsViewModel();
        private EntryViewModel EntryVM { get; } = new EntryViewModel();
        private HomeViewModel HomeVM { get; } = new HomeViewModel();
        private AccountViewModel AccountVM { get; } = new AccountViewModel();
        private LoginViewModel LoginVM { get; } = new LoginViewModel();
        private SubmitMfaCodeViewModel SubmitMfaCodeVM { get; } = new SubmitMfaCodeViewModel();
        private RegisterViewModel RegisterVM { get; } = new RegisterViewModel();
        private ConfirmationCodeViewModel ConfirmationCodeVM { get; } = new ConfirmationCodeViewModel();
        private ResetPasswordViewModel ResetPasswordVM { get; } = new ResetPasswordViewModel();
        private ChangeUsernameViewModel ChangeUsernameVM { get; } = new ChangeUsernameViewModel();
        private ReturnToLoginViewModel ReturnToLoginVM { get; } = new ReturnToLoginViewModel();
        private ForgotPasswordViewModel ForgotPasswordVM { get; } = new ForgotPasswordViewModel();
        private SubmitNewEmailViewModel SubmitNewEmailVM { get; } = new SubmitNewEmailViewModel();
        private MfaSetupViewModel MfaSetupVM { get; } = new MfaSetupViewModel();
        private RecoverMfaViewModel RecoverMfaVM { get; } = new RecoverMfaViewModel();
        private RecoveryCodeDisplayViewModel RecoveryCodeDisplayVM { get; } = new RecoveryCodeDisplayViewModel();
        private ManageMfaViewModel ManageMfaVM { get; } = new ManageMfaViewModel();

        // Commands
        public ICommand SettingsButtonClickedCommand { get; }

        public MainViewModel()
        {
            SettingsButtonClickedCommand = new ViewModelCommand(ExecuteSettingsButtonClickedCommand, CanExecuteSettingsButtonClickedCommand);

            Instance = this;

            windowTitle = string.Empty;     // Make window title empty, but is set in App.xaml.cs.
            currentViewModel = EntryVM;     // Default to HomeViewModel, but don't show yet. Set local property (no event).
            settingsRememberViewModel = currentViewModel;

            // Set timer to automatically call the API PingInLauncher method to notify it that we are open and logged in.
            System.Timers.Timer timer = new(60000);     // Once per minute.
            timer.Elapsed += (sender, e) =>
            {
                // If access token is empty or expired, do not ping in launcher.
                if (string.IsNullOrEmpty(AppData.AccessToken) || AppData.AccessTokenExpiration < DateTime.UtcNow) return;

                // Else we have valid access token, so call API method.
                _ = LoginApiService.PingInLauncher();
            };
            timer.AutoReset = true;
            timer.Start();
        }

        public override void HideView()
        {
            // Main ViewModel is always running and visible.
        }

        public override void ShowView()
        {
            // Main ViewModel is always running and visible.
        }

        private void HideAllViews()
        {
            // Hide all views here so we don't have to update every method each time a new VM is added.
            EntryVM.HideView();
            HomeVM.HideView();
            AccountVM.HideView();
            LoginVM.HideView();
            SubmitMfaCodeVM.HideView();
            RegisterVM.HideView();
            ConfirmationCodeVM.HideView();
            ResetPasswordVM.HideView();
            ReturnToLoginVM.HideView();
            ForgotPasswordVM.HideView();
            SubmitNewEmailVM.HideView();

            MfaSetupVM.HideView();
            RecoverMfaVM.HideView();
            RecoveryCodeDisplayVM.HideView();
            ManageMfaVM.HideView();
        }



        #region Public: View Showing

        public void ShowEntryView()
        {
            HideAllViews();
            EntryVM.ShowView();

            // Call method in entry VM to ping the server (does not take user input, so must be done here).
            EntryVM.PingServerCommand.Execute(this);

            CurrentViewModel = EntryVM;
        }

        public void ShowHomeView()
        {
            HideAllViews();
            HomeVM.ShowView();

            CurrentViewModel = HomeVM;
        }

        public void ShowAccountView()
        {
            HideAllViews();
            AccountVM.ShowView();

            CurrentViewModel = AccountVM;
        }

        public void ShowLoginView()
        {
            HideAllViews();
            LoginVM.ShowView();

            CurrentViewModel = LoginVM;
        }

        public void ShowSubmitMfaCodeView(MfaContext context)
        {
            HideAllViews();
            SubmitMfaCodeVM.ShowView();
            SubmitMfaCodeVM.SetViewContext(context);

            CurrentViewModel = SubmitMfaCodeVM;
        }

        public void ShowRegisterView()
        {
            HideAllViews();
            RegisterVM.ShowView();

            CurrentViewModel = RegisterVM;
        }

        public void ShowConfirmationCodeView(ConfirmationCodeViewModel.CodeContext codeContext, string targetEmail)
        {
            HideAllViews();
            ConfirmationCodeVM.ShowView();

            ConfirmationCodeVM.SetViewContext(codeContext);
            ConfirmationCodeVM.TargetEmail = targetEmail;

            CurrentViewModel = ConfirmationCodeVM;
        }

        public void ShowResetPasswordView(bool isForgotPasswordContext, string targetUser)
        {
            HideAllViews();
            ResetPasswordVM.ShowView();

            ResetPasswordVM.SetViewContext(isForgotPasswordContext);
            ResetPasswordVM.TargetUser = targetUser;

            CurrentViewModel = ResetPasswordVM;
        }

        public void ShowChangeUsernameView()
        {
            HideAllViews();
            ChangeUsernameVM.ShowView();

            CurrentViewModel = ChangeUsernameVM;
        }

        public void ShowReturnToLoginView(bool isError, string statusMessage)
        {
            HideAllViews();
            ReturnToLoginVM.ShowView();

            ReturnToLoginVM.StatusMessage = statusMessage;
            ReturnToLoginVM.SetMessageIsError(isError);

            CurrentViewModel = ReturnToLoginVM;
        }

        public void ShowForgotPasswordView(string targetUser)
        {
            HideAllViews();
            ForgotPasswordVM.ShowView();

            ForgotPasswordVM.UsernameOrEmail = targetUser;

            CurrentViewModel = ForgotPasswordVM;
        }

        public void ShowSubmitNewEmailView(bool isForMainEmail)
        {
            HideAllViews();
            SubmitNewEmailVM.ShowView();
            SubmitNewEmailVM.SetViewContext(isForMainEmail);

            CurrentViewModel = SubmitNewEmailVM;
        }

        public void ShowMfaSetupView(MfaContext context, string otpAuthLink)
        {
            HideAllViews();
            MfaSetupVM.ShowView();
            MfaSetupVM.SetViewContext(context);
            MfaSetupVM.OtpAuthLink = otpAuthLink;

            CurrentViewModel = MfaSetupVM;
        }

        public void ShowRecoverMfaView(MfaContext context)
        {
            HideAllViews();
            RecoverMfaVM.ShowView();
            RecoverMfaVM.SetViewContext(context);

            CurrentViewModel = RecoverMfaVM;
        }

        public void ShowRecoveryCodeDisplayView(MfaContext context, string recoveryCode)
        {
            HideAllViews();
            RecoveryCodeDisplayVM.ShowView();
            RecoveryCodeDisplayVM.SetViewContext(context);
            RecoveryCodeDisplayVM.RecoveryCode = recoveryCode;

            CurrentViewModel = RecoveryCodeDisplayVM;
        }

        public void ShowManageMfaView(ManageMfaViewModel.ManageMfaContext context)
        {
            HideAllViews();
            ManageMfaVM.ShowView();
            ManageMfaVM.SetViewContext(context);

            CurrentViewModel = ManageMfaVM;
        }

        #endregion

        #region Private: SettingsButtonClickedCommand

        private void ExecuteSettingsButtonClickedCommand(object? obj)
        {
            // TOGGLE BETWEEN SHOWING AND HIDING, USING LOCAL VARIABLE
            if (isSettingsViewOpen)
            {
                SettingsVM.HideView();
                isSettingsViewOpen = false;
                
                // Return current view model to the temporarily-stored previous VM.
                CurrentViewModel = settingsRememberViewModel;
            }
            else
            {
                SettingsVM.ShowView();
                isSettingsViewOpen = true;

                // Store the current view model temporarily, then set it to settings for now.
                settingsRememberViewModel = CurrentViewModel;
                CurrentViewModel = SettingsVM;
            }
        }

        private bool CanExecuteSettingsButtonClickedCommand(object? obj)
        {
            // Always can toggle.
            return true;
        }

        #endregion
    }
}
