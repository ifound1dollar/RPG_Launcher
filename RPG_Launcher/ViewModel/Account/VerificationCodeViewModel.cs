using RPG_Launcher.Model;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace RPG_Launcher.ViewModel.Account
{
    public class VerificationCodeViewModel : ViewModelBase
    {
        public enum CodeContext { None, NewAccountConfirmation, ResetPassword }

        private readonly Brush infoBrush = Brushes.CornflowerBlue;
        private readonly Brush errorBrush = Brushes.IndianRed;

        private bool isVerificationCodeViewVisible = false;
        private CodeContext context;
        private DateTime lastSent;

        private string verificationCode = string.Empty;
        private string contextTitle = string.Empty;
        private string targetUser = string.Empty;
        private Brush messageBrush = Brushes.White;
        private string statusMessage = string.Empty;

        private bool isSubmitButtonEnabled = true;

        public string VerificationCode
        {
            get => verificationCode;
            set { verificationCode = value; OnPropertyChanged(nameof(VerificationCode)); }
        }
        public string ContextTitle
        {
            get => contextTitle;
            set { contextTitle = value; OnPropertyChanged(nameof(ContextTitle)); }
        }
        public string TargetUser
        {
            get => targetUser;
            set { targetUser = value; OnPropertyChanged(nameof(TargetUser)); }
        }
        public Brush MessageBrush
        {
            get => messageBrush;
            set { messageBrush = value; OnPropertyChanged(nameof(MessageBrush)); }
        }
        public string StatusMessage
        {
            get => statusMessage;
            set { statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }
        public bool IsSubmitButtonEnabled
        {
            get => isSubmitButtonEnabled;
            set { isSubmitButtonEnabled = value; OnPropertyChanged(nameof(IsSubmitButtonEnabled)); }
        }



        // Commands
        public ICommand SubmitButtonClickedCommand { get; }
        public ICommand ResendCodeButtonClickedCommand { get; }
        public ICommand ReturnToLoginClickedCommand { get; }



        public VerificationCodeViewModel()
        {
            SubmitButtonClickedCommand = new ViewModelCommand(ExecuteSubmitButtonClicked, CanExecuteSubmitButtonClicked);
            ResendCodeButtonClickedCommand = new ViewModelCommand(ExecuteResendCodeButtonClicked, CanExecuteResendCodeButtonClicked);
            ReturnToLoginClickedCommand = new ViewModelCommand(ExecuteReturnToLoginClickedCommand, CanExecuteReturnToLoginClickedCommand);
        }

        public void SetViewContext(CodeContext context)
        {
            this.context = context;
            ContextTitle = (context == CodeContext.NewAccountConfirmation) ? "Confirm Email" : "Reset Password";
        }

        public override void ShowView()
        {
            VerificationCode = string.Empty;
            ContextTitle = string.Empty;
            TargetUser = string.Empty;
            StatusMessage = string.Empty;
            IsSubmitButtonEnabled = true;

            // Set lastSent to now, as we should be showing immediately after sending.
            lastSent = DateTime.UtcNow;

            isVerificationCodeViewVisible = true;
        }

        public override void HideView()
        {
            isVerificationCodeViewVisible = false;
        }



        #region Private: SubmitButtonClicked (async)

        private async Task ExecuteSubmitButtonClicked(object? obj)
        {
            StatusMessage = string.Empty;

            // Validate input, enforcing specific-length code.
            if (VerificationCode.Length != 8)
            {
                StatusMessage = "Verification code must be length 8.";
                MessageBrush = errorBrush;
                VerificationCode = string.Empty;
                return;
            }

            // Disable submit button before awaiting to prevent button spam.
            IsSubmitButtonEnabled = false;

            // Switch on context - if reset password, validate with correct API endpoint and move on to
            //  password reset screen. If email verification, confirm account in database via API endpoint
            //  and move onto home screen with fully usable account.
            switch (context)
            {
                case CodeContext.NewAccountConfirmation:
                    {
                        int resultCode = await LoginApiService.Instance.ConfirmAccountEmail(VerificationCode);
                        if (resultCode == 1)
                        {
                            StatusMessage = "Invalid input state, please try again.";
                            MessageBrush = errorBrush;
                            IsSubmitButtonEnabled = true;
                            break;
                        }
                        else if (resultCode == -1)
                        {
                            StatusMessage = "Incorrect confirmation code.";
                            MessageBrush = errorBrush;
                            IsSubmitButtonEnabled = true;
                            break;
                        }

                        // After account confirmation is successful, we must re-login using the saved refresh token.
                        int loginAttemptResult = await LoginApiService.Instance.TryLoginFromRefreshToken();
                        if (loginAttemptResult == 0)
                        {
                            MainViewModel.Instance.ShowHomeView();
                        }
                        else
                        {
                            // Return to main login screen if somehow unsuccessful login via refresh (should never happen).
                            MainViewModel.Instance.ShowLoginView();
                        }

                        break;
                    }
                case CodeContext.ResetPassword:
                    {
                        // We pass the target user instead of refresh token because we might not have a valid refresh token here.
                        int resultCode = await LoginApiService.Instance.RequestPasswordResetTokenFromCode(TargetUser, VerificationCode);
                        if (resultCode == 1)
                        {
                            StatusMessage = "Invalid input state, please try again.";
                            MessageBrush = errorBrush;
                            IsSubmitButtonEnabled = true;
                            break;
                        }
                        else if (resultCode == -1)
                        {
                            StatusMessage = "Incorrect confirmation code.";
                            MessageBrush = errorBrush;
                            IsSubmitButtonEnabled = true;
                            break;
                        }

                        // After request is successful (code 0), we move on to password reset screen.
                        MainViewModel.Instance.ShowResetPasswordView(TargetUser);

                        break;
                    }
                // Do nothing for None.
            }

            // Before returning, clear verification code field.
            VerificationCode = string.Empty;
        }

        private bool CanExecuteSubmitButtonClicked(object? obj)
        {
            return isVerificationCodeViewVisible;
        }

        #endregion

        #region Private: ResendCodeButtonClicked (async)

        private async Task ExecuteResendCodeButtonClicked(object? obj)
        {
            // Always clear all fields right as button Command is executed.
            VerificationCode = string.Empty;
            StatusMessage = string.Empty;

            // Only allow one new code per minute.
            if ((DateTime.UtcNow - lastSent) < TimeSpan.FromMinutes(1))
            {
                StatusMessage = "Please wait at least 60 seconds before requesting a new code.";
                MessageBrush = errorBrush;
                return;
            }

            // Send new confirmation code, not getting any response for security reasons.
            await LoginApiService.Instance.SendEmailConfirmationCode(targetUser);
            lastSent = DateTime.UtcNow;

            // Else successful, so update status message with success confirmation.
            StatusMessage = "Code successfuly re-sent.";
            MessageBrush = infoBrush;
        }

        private bool CanExecuteResendCodeButtonClicked(object? obj)
        {
            return isVerificationCodeViewVisible;
        }

        #endregion

        #region Private: ReturnToLoginClicked

        private void ExecuteReturnToLoginClickedCommand(object? obj)
        {
            // Simply navigates back to login view, ignoring any pending code that exists on the server.
            MainViewModel.Instance.ShowLoginView();
        }

        private bool CanExecuteReturnToLoginClickedCommand(object? obj)
        {
            return isVerificationCodeViewVisible;
        }

        #endregion
    }
}
