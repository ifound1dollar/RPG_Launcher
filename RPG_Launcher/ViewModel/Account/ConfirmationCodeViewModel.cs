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
    public class ConfirmationCodeViewModel : ViewModelBase
    {
        public enum CodeContext { None, NewAccountConfirmation, ForgotPassword, ManualChangePassword, RequestEmailChange, VerifyNewEmail }

        private readonly Brush infoBrush = Brushes.CornflowerBlue;
        private readonly Brush errorBrush = Brushes.IndianRed;

        private bool isVerificationCodeViewVisible = false;
        private CodeContext context;
        private DateTime lastSent;

        private string confirmationCode = string.Empty;
        private string contextTitle = string.Empty;
        private string targetEmail = string.Empty;
        private Brush messageBrush = Brushes.White;
        private string statusMessage = string.Empty;

        private bool isButtonInputEnabled = true;

        public string ConfirmationCode
        {
            get => confirmationCode;
            set
            {
                confirmationCode = value;
                OnPropertyChanged(nameof(ConfirmationCode));
                StatusMessage = string.Empty;
            }
        }
        public string ContextTitle
        {
            get => contextTitle;
            set { contextTitle = value; OnPropertyChanged(nameof(ContextTitle)); }
        }
        public string TargetEmail
        {
            get => targetEmail;
            set { targetEmail = value; OnPropertyChanged(nameof(TargetEmail)); }
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
        public bool IsButtonInputEnabled
        {
            get => isButtonInputEnabled;
            set { isButtonInputEnabled = value; OnPropertyChanged(nameof(IsButtonInputEnabled)); }
        }



        // Commands
        public ICommand SubmitButtonClickedCommand { get; }
        public ICommand ResendCodeButtonClickedCommand { get; }
        public ICommand CancelButtonClickedCommand { get; }



        public ConfirmationCodeViewModel()
        {
            SubmitButtonClickedCommand = new ViewModelCommand(ExecuteSubmitButtonClicked, CanExecuteSubmitButtonClicked);
            ResendCodeButtonClickedCommand = new ViewModelCommand(ExecuteResendCodeButtonClicked, CanExecuteResendCodeButtonClicked);
            CancelButtonClickedCommand = new ViewModelCommand(ExecuteCancelButtonClickedCommand, CanExecuteCancelButtonClickedCommand);
        }

        public void SetViewContext(CodeContext context)
        {
            this.context = context;
            switch (context)
            {
                case CodeContext.NewAccountConfirmation:
                    {
                        ContextTitle = "Verify Account Email";
                        break;
                    }
                case CodeContext.ForgotPassword:
                case CodeContext.ManualChangePassword:
                    {
                        ContextTitle = "Reset Password";
                        break;
                    }
                case CodeContext.RequestEmailChange:
                    {
                        ContextTitle = "Change Email";
                        break;
                    }
                case CodeContext.VerifyNewEmail:
                    {
                        ContextTitle = "Verify New Email";
                        break;
                    }
            }
        }

        public override void ShowView()
        {
            ConfirmationCode = string.Empty;
            ContextTitle = string.Empty;
            TargetEmail = string.Empty;
            StatusMessage = string.Empty;
            IsButtonInputEnabled = true;

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
            if (ConfirmationCode.Length != 8)
            {
                ConfirmationCode = string.Empty;
                StatusMessage = "Confirmation code must be length 8.";
                MessageBrush = errorBrush;
                return;
            }

            // Disable submit button before awaiting to prevent button spam.
            IsButtonInputEnabled = false;

            // Switch on context - if reset password, validate with correct API endpoint and move on to
            //  password reset screen. If email verification, confirm account in database via API endpoint
            //  and move onto home screen with fully usable account.
            int StatusCode; string Message = string.Empty;
            switch (context)
            {
                case CodeContext.NewAccountConfirmation:
                    {
                        (StatusCode, Message) = await LoginApiService.Instance.VerifyEmail(ConfirmationCode, isForNewAccount: true);
                        if (StatusCode == 0)
                        {
                            // If status code is good, then email verification fully logged us in already, so move onto home view.
                            MainViewModel.Instance.ShowHomeView();
                            return;
                        }
                        break;
                    }
                case CodeContext.ForgotPassword:
                case CodeContext.ManualChangePassword:
                    {
                        // We pass the target user instead of refresh token because we might not have a valid refresh token here.
                        (StatusCode, Message) = await LoginApiService.Instance.InitiatePasswordReset(TargetEmail, ConfirmationCode);
                        if (StatusCode == 0)
                        {
                            // After request is successful (code 0), we move on to password reset screen.
                            MainViewModel.Instance.ShowResetPasswordView(isForgotPasswordContext: (context == CodeContext.ForgotPassword), TargetEmail);
                            return;
                        }
                        break;
                    }
                case CodeContext.RequestEmailChange:
                    {
                        (StatusCode, Message) = await LoginApiService.Instance.InitiateEmailChange(ConfirmationCode);
                        if (StatusCode == 0)
                        {
                            // If request is successful, move onto submit new email screen (we have our Email Change Token).
                            MainViewModel.Instance.ShowSubmitNewEmailView();
                            return;
                        }
                        break;
                    }
                case CodeContext.VerifyNewEmail:
                    {
                        (StatusCode, Message) = await LoginApiService.Instance.VerifyEmail(ConfirmationCode, isForNewAccount: false);
                        if (StatusCode == 0)
                        {
                            // If verification is successful, then email has been fully changed, so return to account view.
                            MainViewModel.Instance.ShowAccountView();
                            return;
                        }
                        break;
                    }
                // Do nothing for None.
            }

            // Any non-success code means either unexpected error (exception) or legitimate HTTP status code error.
            ConfirmationCode = string.Empty;
            StatusMessage = Message;
            MessageBrush = errorBrush;
            IsButtonInputEnabled = true;
        }

        private bool CanExecuteSubmitButtonClicked(object? obj)
        {
            return isVerificationCodeViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: ResendCodeButtonClicked (async)

        private async Task ExecuteResendCodeButtonClicked(object? obj)
        {
            // Always clear all fields right as button Command is executed.
            ConfirmationCode = string.Empty;
            StatusMessage = string.Empty;

            // Only allow one new code per minute.
            if ((DateTime.UtcNow - lastSent) < TimeSpan.FromMinutes(1))
            {
                StatusMessage = "Please wait at least 60 seconds before requesting a new code.";
                MessageBrush = errorBrush;
                return;
            }


            // Send new confirmation code to the correct endpoint based on context, and handle response accordingly.
            switch (context)
            {
                case CodeContext.NewAccountConfirmation:
                case CodeContext.VerifyNewEmail:
                    {
                        // Request a new email verification code, printing an error message if unsuccessful.
                        var (StatusCode, Message) = await LoginApiService.Instance.ResendEmailVerificationCode();
                        if (StatusCode != 0)
                        {
                            StatusMessage = Message;
                            MessageBrush = errorBrush;
                            return;
                        }
                        break;
                    }
                case CodeContext.ForgotPassword:
                case CodeContext.ManualChangePassword:
                    {
                        // Submit a forgot password request anonymously, printing an error message if unsuccessful.
                        int responseCode = await LoginApiService.Instance.ForgotPassword(targetEmail);
                        if (responseCode == -1)
                        {
                            StatusMessage = "Failed to perform API request, please try again.";
                            MessageBrush = errorBrush;
                            return;
                        }
                        break;
                    }
                case CodeContext.RequestEmailChange:
                    {
                        // Generate an entirely new email change request, printing an error message if unsuccessful.
                        var (StatusCode, Message) = await LoginApiService.Instance.RequestEmailChange();
                        if (StatusCode != 0)
                        {
                            StatusMessage = Message;
                            MessageBrush = errorBrush;
                            return;
                        }
                        break;
                    }
                    // Do nothing for None.
            }

            // Else successful (did not return within switch), so update status message with success confirmation.
            lastSent = DateTime.UtcNow;
            StatusMessage = "Code successfuly re-sent.";
            MessageBrush = infoBrush;
        }

        private bool CanExecuteResendCodeButtonClicked(object? obj)
        {
            // Disallow click if main button is not enabled (means awaiting API response).
            return isVerificationCodeViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: CancelButtonClicked

        private void ExecuteCancelButtonClickedCommand(object? obj)
        {
            isButtonInputEnabled = false;

            switch(context)
            {
                case CodeContext.NewAccountConfirmation:
                case CodeContext.ForgotPassword:
                    {
                        // Cancelling new account confirmation or forgot password should return to login.
                        // Upon returning to login, fully logout to clear access and refresh token, then show login view.
                        // NOTE: Do not await logout (fire-and-forget).
                        _ = LoginApiService.Instance.Logout();

                        MainViewModel.Instance.ShowLoginView();
                        break;
                    }
                case CodeContext.ManualChangePassword:
                case CodeContext.RequestEmailChange:
                case CodeContext.VerifyNewEmail:
                    {
                        // These states can only be from account view screen, so return to it.
                        MainViewModel.Instance.ShowAccountView();
                        break;
                    }
            }

        }

        private bool CanExecuteCancelButtonClickedCommand(object? obj)
        {
            // Disallow click if main button is not enabled (means awaiting API response).
            return isVerificationCodeViewVisible && isButtonInputEnabled;
        }

        #endregion
    }
}
