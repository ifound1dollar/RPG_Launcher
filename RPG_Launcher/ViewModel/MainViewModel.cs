using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Base;
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
        public static MainViewModel Instance { get; private set; } = null!;     // STATIC SINGLETON

        private string windowTitle;
        private ViewModelBase currentViewModel;

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
        private HomeViewModel HomeVM { get; } = new HomeViewModel();
        private LoginViewModel LoginVM { get; } = new LoginViewModel();
        private RegisterViewModel RegisterVM { get; } = new RegisterViewModel();
        private VerificationCodeViewModel VerificationCodeVM { get; } = new VerificationCodeViewModel();
        private ResetPasswordViewModel ResetPasswordVM { get; } = new ResetPasswordViewModel();
        private ReturnToLoginViewModel ReturnToLoginVM { get; } = new ReturnToLoginViewModel();
        private ForgotPasswordViewModel ForgotPasswordVM { get; } = new ForgotPasswordViewModel();

        public MainViewModel()
        {
            Instance = this;

            windowTitle = string.Empty;     // Make window title empty, but is set in App.xaml.cs.
            currentViewModel = HomeVM;      // Default to HomeViewModel, but don't show yet. Set local property (no event).
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
            HomeVM.HideView();
            LoginVM.HideView();
            RegisterVM.HideView();
            VerificationCodeVM.HideView();
        }



        public void ShowHomeView()
        {
            HideAllViews();
            HomeVM.ShowView();

            CurrentViewModel = HomeVM;
        }

        public void ShowLoginView()
        {
            HideAllViews();
            LoginVM.ShowView();

            CurrentViewModel = LoginVM;
        }

        public void ShowRegisterView()
        {
            HideAllViews();
            RegisterVM.ShowView();

            CurrentViewModel = RegisterVM;
        }

        public void ShowVerificationCodeView(bool isForNewAccount, string targetUser)
        {
            HideAllViews();
            VerificationCodeVM.ShowView();

            if (isForNewAccount)
            {
                VerificationCodeVM.SetViewContext(VerificationCodeViewModel.CodeContext.NewAccountConfirmation);
            }
            else
            {
                VerificationCodeVM.SetViewContext(VerificationCodeViewModel.CodeContext.ResetPassword);
            }
            VerificationCodeVM.TargetUser = targetUser;

            CurrentViewModel = VerificationCodeVM;
        }

        public void ShowResetPasswordView(string targetUser)
        {
            HideAllViews();
            ResetPasswordVM.ShowView();

            ResetPasswordVM.TargetUser = targetUser;

            CurrentViewModel = ResetPasswordVM;
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

            ForgotPasswordVM.Username = targetUser;

            CurrentViewModel = ForgotPasswordVM;
        }
    }
}
