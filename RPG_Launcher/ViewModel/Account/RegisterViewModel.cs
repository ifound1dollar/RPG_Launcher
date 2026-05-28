using RPG_Launcher.Model;
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
    public class RegisterViewModel : ViewModelBase
    {
        private bool isRegisterViewVisible = false;

        private string email = string.Empty;
        private string username = string.Empty;
        private SecureString securePassword = new();
        private string errorMessage = string.Empty;

        private bool isButtonInputEnabled = true;

        public string Email
        {
            get => email;
            set
            {
                email = value;
                OnPropertyChanged(nameof(Email));
                ErrorMessage = string.Empty;
            }
        }
        public string Username
        {
            get => username;
            set
            {
                username = value;
                OnPropertyChanged(nameof(Username));
                ErrorMessage = string.Empty;
            }
        }
        public SecureString SecurePassword
        {
            // IMPORTANT: This is not directly bound to via the MVVM pattern. SecureStrings do not support
            //  binding by default, so we had to shift behavior to the code-behind. Whenever the PasswordBox
            //  field is updated, we receive an update directly from the code-behind rather than binding
            //  it directly to this ViewModel. This is necessary to allow PasswordBox.Clear() to be called,
            //  which must be done the instant that the login button is pressed. We still process the login
            //  here, pulling directly from this variable (which IMPORTANTLY is automatically updated via
            //  the code-behind whenever the PasswordBox is changed in the UI).
            // The only real difference is how this field is updated; the code-behind has more
            //  responsibility in this case and directly controls what this value reads, rather than the
            //  other way around.
            get => securePassword;
            set
            {
                securePassword = value;
                OnPropertyChanged(nameof(SecurePassword));

                // Do not clear error message here, instead clear it manually from the View code-behind. Clearing
                //  it here has the unintended side effect of clearing the error message immediately when the view
                //  clears the password box, disallowing the user to read the error.
                //ErrorMessage = string.Empty;
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
        public ICommand RegisterClickedCommand { get; }
        public ICommand AlreadyHaveClickedCommand { get; }



        public RegisterViewModel()
        {
            RegisterClickedCommand = new ViewModelCommand(ExecuteRegisterClickedCommand, CanExecuteRegisterClickedCommand);
            AlreadyHaveClickedCommand = new ViewModelCommand(ExecuteAlreadyHaveClickedCommand, CanExecuteAlreadyHaveClickedCommand);
        }

        public override void ShowView()
        {
            // Clear all fields again.
            Email = string.Empty;
            Username = string.Empty;
            SecurePassword.Clear();
            ErrorMessage = string.Empty;
            IsButtonInputEnabled = true;

            isRegisterViewVisible = true;
        }
        public override void HideView()
        {
            isRegisterViewVisible = false;
        }



        #region Private: RegisterClickedCommand (async)

        private async Task ExecuteRegisterClickedCommand(object? obj)
        {
            // NOTE: We have to compare Password and Confirm Password fields within the View, not here.
            //  It is not trivial to compare SecureStrings, so we compare the PasswordBoxes in RegisterView.

            string trimmedUsername = Username.Trim();
            string trimmedEmail = Email.Trim();
            NetworkCredential credential = new(trimmedUsername, SecurePassword);

            // Clear error message, then validate input.
            ErrorMessage = string.Empty;
            if (trimmedEmail.Length <= 0 || trimmedUsername.Length <= 0 || SecurePassword.Length <= 0)
            {
                SecurePassword.Clear();
                ErrorMessage = "All input fields must be set.";
                return;
            }

            // Verify that email is legitimate using simple regex.
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(trimmedEmail, pattern))
            {
                SecurePassword.Clear();
                ErrorMessage = "Please enter a valid email.";
                return;
            }
            // Verify username is valid.
            pattern = @"^[a-zA-Z0-9_ ]{3,16}$";
            if (!Regex.IsMatch(trimmedUsername, pattern))
            {
                SecurePassword.Clear();
                ErrorMessage = "Username must be 3-16 characters and can only include letters, digits, underscores, and spaces.";
                return;
            }
            // Verify password is valid.
            pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,64}$";      // 8-64 chars, 1+ upper lower digit special (all specials)
            if (!Regex.IsMatch(credential.Password, pattern))
            {
                SecurePassword.Clear();
                ErrorMessage = "Password must be 8-64 characters and include at least one uppercase letter, lowercase letter, digit, and symbol.";
                return;
            }

            // Disable register button before awaiting to prevent button spam.
            IsButtonInputEnabled = false;

            // Call API service login method with Username and Password.
            var (StatusCode, Message) = await LoginApiService.Register(trimmedEmail, credential);
            if (StatusCode == 10)       // Will always need email confirmation.
            {
                SecurePassword.Clear();

                // If status code is good, immediately move onto confirmation code view so the user can verify their email.
                MainViewModel.Instance.ShowConfirmationCodeView(ConfirmationCodeViewModel.CodeContext.NewAccountConfirmation, Email);
                return;
            }
            else
            {
                // Any other non-success code means either unexpected error (exception) or legitimate HTTP status code error.
                SecurePassword.Clear();
                ErrorMessage = Message;
                IsButtonInputEnabled = true;
                return;
            }
        }

        private bool CanExecuteRegisterClickedCommand(object? obj)
        {
            // Register button can only be clicked if login subgrid is visible and buttons are enabled.
            return isRegisterViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: AlreadyHaveClickedCommand

        private void ExecuteAlreadyHaveClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowLoginView();
        }

        private bool CanExecuteAlreadyHaveClickedCommand(object? obj)
        {
            // Disallow click if main button is not enabled (means awaiting API response).
            return isRegisterViewVisible && isButtonInputEnabled;
        }

        #endregion

    }
}
