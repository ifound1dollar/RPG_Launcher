using Microsoft.Win32;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel.General
{
    public class SettingsViewModel : ViewModelBase
    {
        private bool isViewVisible = false;

        private string errorMessage = string.Empty;

        public string GameDirectory
        {
            get { return AppData.GameInstallDirectory; }
            set { AppData.GameInstallDirectory = value; OnPropertyChanged(nameof(GameDirectory)); }
        }
        public string ErrorMessage
        {
            get => errorMessage;
            set { errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }



        // Commands
        public ICommand SetGameDirectoryClickedCommand { get; }
        public ICommand CloseButtonClickedCommand { get; }



        public SettingsViewModel()
        {
            SetGameDirectoryClickedCommand = new ViewModelCommand(ExecuteSetGameDirectoryClickedCommand, CanExecuteSetGameDirectoryClickedCommand);
            CloseButtonClickedCommand = new ViewModelCommand(ExecuteCloseButtonClickedCommand, CanExecuteCloseButtonClickedCommand);
        }

        public override void ShowView()
        {
            isViewVisible = true;
        }
        
        public override void HideView()
        {
            isViewVisible = false;
        }



        #region Private: SetGameDirectoryClicked

        private void ExecuteSetGameDirectoryClickedCommand(object? obj)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select game installation directory...",
                DefaultDirectory = AppData.GameInstallDirectory,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                GameDirectory = dialog.FolderName;
            }
        }

        private bool CanExecuteSetGameDirectoryClickedCommand(object? obj)
        {
            return isViewVisible;
        }

        #endregion

        #region Private: CloseButtonClicked

        private void ExecuteCloseButtonClickedCommand(object? obj)
        {
            if (MainViewModel.Instance.SettingsButtonClickedCommand.CanExecute(obj))
            {
                MainViewModel.Instance.SettingsButtonClickedCommand.Execute(obj);
            }
        }

        private bool CanExecuteCloseButtonClickedCommand(object? obj)
        {
            return isViewVisible;
        }

        #endregion

    }
}
