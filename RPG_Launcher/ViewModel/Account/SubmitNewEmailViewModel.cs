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
    public class SubmitNewEmailViewModel : ViewModelBase
    {
        private bool isSubmitNewEmailViewVisible = false;

        private bool isForMainEmail = true;

        private string contextTitle = string.Empty;
        private string existingEmail = string.Empty;
        private string newEmail = string.Empty;
        private string errorMessage = string.Empty;

        private bool isButtonInputEnabled = true;

        public string ContextTitle
        {
            get => contextTitle;
            set { contextTitle = value; OnPropertyChanged(nameof(ContextTitle)); }
        }
        public string ExistingEmail
        {
            get => existingEmail;
            set
            {
                existingEmail = value;
                OnPropertyChanged(nameof(ExistingEmail));
                ErrorMessage = string.Empty;
            }
        }
        public string NewEmail
        {
            get => newEmail;
            set
            {
                newEmail = value;
                OnPropertyChanged(nameof(NewEmail));
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



        public SubmitNewEmailViewModel()
        {
            SubmitButtonClickedCommand = new ViewModelCommand(ExecuteSubmitButtonClickedCommand, CanExecuteSubmitButtonClickedCommand);
            CancelButtonClickedCommand = new ViewModelCommand(ExecuteCancelButtonClickedCommand, CanExecuteCancelButtonClickedCommand);
        }

        public void SetViewContext(bool isForMainEmail)
        {
            this.isForMainEmail = isForMainEmail;
            if (isForMainEmail)
            {
                ContextTitle = "Change Primary Email";
                ExistingEmail = AppData.SavedEmail;
            }
            else
            {
                ContextTitle = "Secondary Email Setup";
                ExistingEmail = (AppData.SecondaryEmail == string.Empty) ? "None" : AppData.SecondaryEmail;
            }
        }

        public override void ShowView()
        {
            NewEmail = string.Empty;
            ErrorMessage = string.Empty;
            IsButtonInputEnabled = true;

            isSubmitNewEmailViewVisible = true;
        }

        public override void HideView()
        {
            isSubmitNewEmailViewVisible = false;
        }



        #region Private: SubmitButtonClickedCommand (async)

        private async Task ExecuteSubmitButtonClickedCommand(object? obj)
        {
            string trimmedEmail = NewEmail.Trim();

            // Verify that email is legitimate using simple regex.
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(trimmedEmail, pattern))
            {
                ErrorMessage = "Please enter a valid email.";
                return;
            }

            // Disable button input before performing async request.
            IsButtonInputEnabled = false;

            // After validating email, we can make actual API request to submit the new password.
            int StatusCode; string Message = string.Empty;
            if (isForMainEmail)
            {
                (StatusCode, Message) = await LoginApiService.SubmitChangedEmail(trimmedEmail);
            }
            else
            {
                (StatusCode, Message) = await LoginApiService.SubmitSecondaryEmail(trimmedEmail);
            }

            // Move on based on response and context.
            if (StatusCode == 0)
            {
                // Code 0 indicates success, meaning the API has accepted our new email. We can move onto new email verification screen.
                var context = (isForMainEmail) ? ConfirmationCodeViewModel.CodeContext.VerifyNewEmail : ConfirmationCodeViewModel.CodeContext.VerifySecondaryEmail;
                MainViewModel.Instance.ShowConfirmationCodeView(context, NewEmail);
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
            return isSubmitNewEmailViewVisible && isButtonInputEnabled;
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
            return isSubmitNewEmailViewVisible && isButtonInputEnabled;
        }

        #endregion

    }
}
