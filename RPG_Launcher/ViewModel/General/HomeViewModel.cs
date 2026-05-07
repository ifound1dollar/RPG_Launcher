using RPG_Launcher.Model;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel.General
{
    public class HomeViewModel : ViewModelBase
    {
        private bool isHomeViewVisible = false;

        // Bindable properties here



        // Commands
        public ICommand AccountClickedCommand { get; }



        public HomeViewModel()
        {
            AccountClickedCommand = new ViewModelCommand(ExecuteAccountClickedCommand, CanExecuteAccountClickedCommand);
        }

        public override void ShowView()
        {
            isHomeViewVisible = true;
        }

        public override void HideView()
        {
            isHomeViewVisible = false;
        }



        #region Private: AccountClickedCommand

        private void ExecuteAccountClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowAccountView();
        }

        private bool CanExecuteAccountClickedCommand(object? obj)
        {
            return isHomeViewVisible;
        }

        #endregion

    }
}
