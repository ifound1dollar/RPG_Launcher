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

        // Commands (for showing Views)
        public ICommand ShowHomeViewCommand;
        public ICommand ShowLoginViewCommand;
        public ICommand ShowRegisterViewCommand;
        public ICommand ShowEmailConfirmViewCommand;
        


        public MainViewModel()
        {
            Instance = this;

            windowTitle = string.Empty;     // Make window title empty, but is set in App.xaml.cs.
            currentViewModel = HomeVM;      // Default to HomeViewModel, but don't show yet. Set local property (no event).

            ShowHomeViewCommand = new ViewModelCommand(ExecuteShowHomeViewCommand);
            ShowLoginViewCommand = new ViewModelCommand(ExecuteShowLoginViewCommand);
            ShowRegisterViewCommand = new ViewModelCommand(ExecuteShowRegisterViewCommand);
            ShowEmailConfirmViewCommand = new ViewModelCommand(ExecuteShowEmailConfirmViewCommand);
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



        private void ExecuteShowHomeViewCommand(object? obj)
        {
            HideAllViews();
            HomeVM.ShowView();

            CurrentViewModel = HomeVM;
        }

        private void ExecuteShowLoginViewCommand(object? obj)
        {
            HideAllViews();
            LoginVM.ShowView();

            CurrentViewModel = LoginVM;
        }

        private void ExecuteShowRegisterViewCommand(object? obj)
        {
            HideAllViews();
            RegisterVM.ShowView();

            CurrentViewModel = RegisterVM;
        }

        private void ExecuteShowEmailConfirmViewCommand(object? obj)
        {
            HideAllViews();
            VerificationCodeVM.ShowView();

            VerificationCodeVM.SetCodeContext(VerificationCodeViewModel.CodeContext.NewAccountConfirmation);

            CurrentViewModel = VerificationCodeVM;
        }

    }
}
