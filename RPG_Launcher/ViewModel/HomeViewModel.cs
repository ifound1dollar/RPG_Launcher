using RPG_Launcher.Model;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel
{
    public class HomeViewModel : ViewModelBase
    {
        private bool isHomeViewVisible = false;

        // Bindable properties here



        // Commands
        public ICommand LogoutClickedCommand { get; }



        public HomeViewModel()
        {
            LogoutClickedCommand = new ViewModelCommand(ExecuteLogoutClickedCommand, CanExecuteLogoutClickedCommand);
        }

        public override void ShowView()
        {
            isHomeViewVisible = true;
        }

        public override void HideView()
        {
            isHomeViewVisible = false;
        }



        #region Private: LogoutClickedCommand

        private void ExecuteLogoutClickedCommand(object? obj)
        {
            // Call API service logout method, which will always successfully log us out.
            LoginApiService.Instance.Logout();

            // After logout, we must return to the login screen (show login window and hide main).
            MainViewModel.Instance.ShowLoginViewCommand.Execute(obj);
        }

        private bool CanExecuteLogoutClickedCommand(object? obj)
        {
            // Can only logout if main subgrid is visible (already logged in).
            return isHomeViewVisible;
        }

        #endregion

    }
}
