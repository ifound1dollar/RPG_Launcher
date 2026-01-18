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

namespace RPG_Launcher.ViewModel
{
    public class RegisterViewModel : ViewModelBase
    {
        private bool isRegisterViewVisible = false;

        private string email = string.Empty;
        private string username = string.Empty;
        private SecureString securePassword = new();
        private string errorMessage = string.Empty;

        public string Email
        {
            get => email;
            set {  email = value; OnPropertyChanged(nameof(Email)); }
        }
        public string Username
        {
            get => username;
            set { username = value; OnPropertyChanged(nameof(Username)); }
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
            set { securePassword = value; OnPropertyChanged(nameof(SecurePassword)); }
        }
        public string ErrorMessage
        {
            get => errorMessage;
            set { errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
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

            isRegisterViewVisible = true;
        }
        public override void HideView()
        {
            isRegisterViewVisible = false;
        }



        #region Private: RegisterClickedCommand

        private void ExecuteRegisterClickedCommand(object? obj)
        {
            // NOTE: We have to compare Password and Confirm Password fields within the View, not here.
            //  It is not trivial to compare SecureStrings, so we compare the PasswordBoxes in RegisterView.

            NetworkCredential credential = new(Username, SecurePassword);

            // Clear error message, then validate input.
            ErrorMessage = string.Empty;
            if (Email.Length <= 0 || Username.Length <= 0 || SecurePassword.Length <= 0)
            {
                ErrorMessage = "All input fields must be set.";
                return;
            }

            // Verify that email is legitimate using simple regex.
            string pattern = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
            if (!Regex.IsMatch(Email, pattern))
            {
                ErrorMessage = "Please enter a valid email.";
                return;
            }
            // Verify username is valid.
            pattern = @"^[a-zA-Z0-9_]{5,20}$";
            if (!Regex.IsMatch(Username, pattern))
            {
                ErrorMessage = "Username must be length 5-20 and include only letters, numbers, and underscores.";
                return;
            }
            // Verify password is valid. Supported characters are in the final [] section.
            pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$";
            if (!Regex.IsMatch(credential.Password, pattern))
            {
                ErrorMessage = "Password must be minimum 8 characters and include at least one uppercase letter, lowercase letter, number, and symbol.";
                return;
            }

            // Call API service login method with Username and Password.
            int registerReturnCode = LoginApiService.Instance.Register(Email, credential);
            if (registerReturnCode == 0)
            {
                // If no errors, we move onto the email verification view.
                MainViewModel.Instance.ShowEmailConfirmationView(Email);
                return;
            }
            if (registerReturnCode == 1)
            {
                ErrorMessage = "Invalid input, please try again.";
                return;
            }
            else
            {
                // Code -1 means generic registration failure, which is typically unavailable email or username.
                ErrorMessage = "Unavailable email or username.";
                return;
            }

        }

        private bool CanExecuteRegisterClickedCommand(object? obj)
        {
            // Login button can only be clicked if login subgrid is visible.
            return isRegisterViewVisible;
        }

        #endregion

        #region Private: AlreadyHaveClickedCommand

        private void ExecuteAlreadyHaveClickedCommand(object? obj)
        {
            MainViewModel.Instance.ShowLoginView();
        }

        private bool CanExecuteAlreadyHaveClickedCommand(object? obj)
        {
            return isRegisterViewVisible;
        }

        #endregion

    }
}
