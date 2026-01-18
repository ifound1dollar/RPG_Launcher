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

namespace RPG_Launcher.ViewModel
{
    public class VerificationCodeViewModel : ViewModelBase
    {
        public enum CodeContext { None, NewAccountConfirmation, ResetPassword }

        private readonly Brush infoBrush = Brushes.CornflowerBlue;
        private readonly Brush errorBrush = Brushes.IndianRed;

        private bool isVerificationCodeViewVisible = false;
        private CodeContext context;

        private string verificationCode = string.Empty;
        private string targetUser = string.Empty;
        private Brush messageBrush = Brushes.White;
        private string statusMessage = string.Empty;

        public string VerificationCode
        {
            get => verificationCode;
            set { verificationCode = value; OnPropertyChanged(nameof(VerificationCode)); }
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



        // Commands
        public ICommand SubmitButtonClicked { get; }
        public ICommand ResendCodeButtonClicked { get; }



        public VerificationCodeViewModel()
        {
            SubmitButtonClicked = new ViewModelCommand(ExecuteSubmitButtonClicked, CanExecuteSubmitButtonClicked);
            ResendCodeButtonClicked = new ViewModelCommand(ExecuteResendCodeButtonClicked, CanExecuteResendCodeButtonClicked);
        }

        public void SetViewContext(CodeContext context, string targetUser)
        {
            this.context = context;
            TargetUser = targetUser;
        }

        public override void ShowView()
        {
            VerificationCode = string.Empty;
            TargetUser = string.Empty;
            StatusMessage = string.Empty;

            isVerificationCodeViewVisible = true;
        }

        public override void HideView()
        {
            isVerificationCodeViewVisible = false;
        }



        #region Private: SubmitButtonClicked

        private void ExecuteSubmitButtonClicked(object? obj)
        {
            StatusMessage = string.Empty;

            // Validate input, enforcing specific-length code.
            if (VerificationCode.Length != 6)
            {
                StatusMessage = "Verification code must be length 6.";
                MessageBrush = errorBrush;
                VerificationCode = string.Empty;
                return;
            }

            // Switch on context - if reset password, validate with correct API endpoint and move on to
            //  password reset screen. If email verification, confirm account in database via API endpoint
            //  and move onto home screen with fully usable account.
            switch (context)
            {
                case CodeContext.NewAccountConfirmation:
                    {
                        int resultCode = LoginApiService.Instance.ConfirmAccountEmail(VerificationCode);
                        if (resultCode == 1)
                        {
                            StatusMessage = "Invalid input state, please try again.";
                            MessageBrush = errorBrush;
                            break;
                        }
                        else if (resultCode == -1)
                        {
                            StatusMessage = "Incorrect verification code.";
                            MessageBrush = errorBrush;
                            break;
                        }

                        // After account confirmation is successful, we must re-login using the saved refresh token.
                        if (LoginApiService.Instance.TryLoginFromRefreshToken() == 0)
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

        #region Private: ResendCodeButtonClicked

        private void ExecuteResendCodeButtonClicked(object? obj)
        {
            // Always clear all fields right as button Command is executed.
            VerificationCode = string.Empty;
            StatusMessage = string.Empty;

            int resultCode = LoginApiService.Instance.ResendEmailConfirmationCode();
            if (resultCode == 1)
            {
                StatusMessage = "Invalid input state, please try again.";
                MessageBrush = errorBrush;
                return;
            }
            else if (resultCode == -1)
            {
                StatusMessage = "Please wait at least 60 seconds before requesting a new code.";
                MessageBrush = errorBrush;
                return;
            }

            // Else successful, so update status message with success confirmation.
            StatusMessage = "Code successfuly re-sent.";
            MessageBrush = infoBrush;
        }

        private bool CanExecuteResendCodeButtonClicked(object? obj)
        {
            return isVerificationCodeViewVisible;
        }

        #endregion
    }
}
