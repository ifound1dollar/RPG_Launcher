using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel.Account
{
    public class ChangeUsernameViewModel : ViewModelBase
    {
        private bool isChangeUsernameViewVisible = false;

        private string existingUsername = string.Empty;
        private string newUsername = string.Empty;
        private string errorMessage = string.Empty;

        private bool isButtonInputEnabled = true;

        public string ExistingUsername
        {
            get => existingUsername;
            set
            {
                existingUsername = value;
                OnPropertyChanged(nameof(ExistingUsername));
                ErrorMessage = string.Empty;
            }
        }
        public string NewUsername
        {
            get => newUsername;
            set
            {
                newUsername = value;
                OnPropertyChanged(nameof(NewUsername));
                ErrorMessage = string.Empty;
            }
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
        public ICommand SubmitButtonClickedCommand { get; }
        public ICommand CancelButtonClickedCommand { get; }



        public ChangeUsernameViewModel()
        {
            SubmitButtonClickedCommand = new ViewModelCommand(ExecuteSubmitButtonClickedCommand, CanExecuteSubmitButtonClickedCommand);
            CancelButtonClickedCommand = new ViewModelCommand(ExecuteCancelButtonClickedCommand, CanExecuteCancelButtonClickedCommand);
        }

        public override void HideView()
        {
            isChangeUsernameViewVisible = false;
        }

        public override void ShowView()
        {
            ExistingUsername = AppData.SavedUsername;
            NewUsername = string.Empty;
            ErrorMessage = string.Empty;
            IsButtonInputEnabled = true;

            isChangeUsernameViewVisible = true;
        }



        #region Private: SubmitButtonClickedCommand (async)

        private async Task ExecuteSubmitButtonClickedCommand(object? obj)
        {
            string trimmedUsername = NewUsername.Trim();

            // First, ensure new username matches basic username regex.
            string usernamePattern = @"^[a-zA-Z0-9_ ]{3,16}$";
            if (!Regex.IsMatch(trimmedUsername, usernamePattern))
            {
                ErrorMessage = "New username must be 3-16 characters and can only include letters, digits, underscores, and spaces.";
                return;
            }

            // Disable button input before performing async request.
            IsButtonInputEnabled = false;

            // After validating username, we can make actual API request to change username.
            var (StatusCode, Message) = await LoginApiService.ChangeUsername(trimmedUsername);
            if (StatusCode == 0)
            {
                // Code 0 indicates success, username has been changed (AppData also already updated) and we can return to account view.
                MainViewModel.Instance.ShowAccountView();
                return;
            }
            else
            {
                // Any other non-success code means either unexpected error (exception) or legitimate HTTP status code error.
                ErrorMessage = Message;
                IsButtonInputEnabled = true;
                return;
            }
        }

        private bool CanExecuteSubmitButtonClickedCommand(object? obj)
        {
            return isChangeUsernameViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: CancelButtonClickedCommand

        private void ExecuteCancelButtonClickedCommand(object? obj)
        {
            // Always return to account view.
            MainViewModel.Instance.ShowAccountView();
        }

        private bool CanExecuteCancelButtonClickedCommand(object? obj)
        {
            return isChangeUsernameViewVisible && isButtonInputEnabled;
        }

        #endregion
    }
}
