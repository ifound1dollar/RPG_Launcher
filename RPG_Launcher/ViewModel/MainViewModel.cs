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
        public HomeViewModel HomeVM { get; } = new HomeViewModel();
        public LoginViewModel LoginVM { get; } = new LoginViewModel();
        public RegisterViewModel RegisterVM { get; } = new RegisterViewModel();

        // Commands (for showing Views)
        public ICommand ShowHomeViewCommand;
        public ICommand ShowLoginViewCommand;
        public ICommand ShowRegisterViewCommand;
        


        public MainViewModel()
        {
            Instance = this;

            windowTitle = string.Empty;     // Make window title empty, but is set in App.xaml.cs.
            currentViewModel = HomeVM;      // Default to HomeViewModel, but don't show yet. Set local property (no event).

            ShowHomeViewCommand = new ViewModelCommand(ExecuteShowHomeViewCommand);
            ShowLoginViewCommand = new ViewModelCommand(ExecuteShowLoginViewCommand);
            ShowRegisterViewCommand = new ViewModelCommand(ExecuteShowRegisterViewCommand);
        }

        public override void HideView()
        {
            // Main ViewModel is always running and visible.
        }

        public override void ShowView()
        {
            // Main ViewModel is always running and visible.
        }



        private void ExecuteShowHomeViewCommand(object? obj)
        {
            CurrentViewModel = HomeVM;

            HomeVM.ShowView();
            LoginVM.HideView();
            RegisterVM.HideView();
        }

        private void ExecuteShowLoginViewCommand(object? obj)
        {
            CurrentViewModel = LoginVM;

            LoginVM.ShowView();
            HomeVM.HideView();
            RegisterVM.HideView();
        }

        private void ExecuteShowRegisterViewCommand(object? obj)
        {
            CurrentViewModel = RegisterVM;

            RegisterVM.ShowView();
            LoginVM.HideView();
            HomeVM.HideView();
        }

    }
}
