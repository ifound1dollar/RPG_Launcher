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

namespace RPG_Launcher.ViewModel
{
    public class VerificationCodeViewModel : ViewModelBase
    {
        public enum CodeContext { None, NewAccountConfirmation, ResetPassword }

        private bool isVerificationCodeViewVisible = false;
        private CodeContext context;

        private string verificationCode = string.Empty;
        private string errorMessage = string.Empty;

        public string VerificationCode
        {
            get => verificationCode;
            set { verificationCode = value; OnPropertyChanged(nameof(VerificationCode)); }
        }
        public string ErrorMessage
        {
            get => errorMessage;
            set { errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }



        // Commands
        public ICommand SubmitButtonClicked { get; }
        public ICommand ResendCodeButtonClicked { get; }



        public VerificationCodeViewModel()
        {
            SubmitButtonClicked = new ViewModelCommand(ExecuteSubmitButtonClicked, CanExecuteSubmitButtonClicked);
            ResendCodeButtonClicked = new ViewModelCommand(ExecuteResendCodeButtonClicked, CanExecuteResendCodeButtonClicked);
        }

        public void SetCodeContext(CodeContext context)
        {
            this.context = context;
        }

        public override void ShowView()
        {
            VerificationCode = string.Empty;
            ErrorMessage = string.Empty;

            isVerificationCodeViewVisible = true;
        }

        public override void HideView()
        {
            isVerificationCodeViewVisible = false;
        }



        #region Private: SubmitButtonClicked

        private void ExecuteSubmitButtonClicked(object? obj)
        {
            // Validate input, enforcing specific-length code.
            if (VerificationCode.Length != 6)
            {
                ErrorMessage = "Verification code must be length 6.";
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
                            ErrorMessage = "Invalid input state, please try again.";
                            break;
                        }
                        else if (resultCode == -1)
                        {
                            ErrorMessage = "Incorrect verification code.";
                            break;
                        }

                        // After account confirmation is successful, we must re-login using the saved refresh token.
                        if (LoginApiService.Instance.TryLoginFromRefreshToken() == 0)
                        {
                            MainViewModel.Instance.ShowHomeViewCommand.Execute(obj);
                        }
                        else
                        {
                            // Return to main login screen if somehow unsuccessful login via refresh (should never happen).
                            MainViewModel.Instance.ShowLoginViewCommand.Execute(obj);
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
            Trace.WriteLine("verification code resend button pressed");

            // Always clear code field right as button Command is executed.
            VerificationCode = string.Empty;
        }

        private bool CanExecuteResendCodeButtonClicked(object? obj)
        {
            return isVerificationCodeViewVisible;
        }

        #endregion
    }
}
