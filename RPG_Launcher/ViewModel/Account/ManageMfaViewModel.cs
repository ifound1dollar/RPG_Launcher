using RPG_Launcher.Model;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static QRCoder.PayloadGenerator.SwissQrCode;
using static RPG_Launcher.ViewModel.Account.ConfirmationCodeViewModel;

namespace RPG_Launcher.ViewModel.Account
{
    public class ManageMfaViewModel : ViewModelBase
    {
        public enum ManageMfaContext { None, ResetMfa, GenerateNewRecovery }

        private bool isViewVisible = false;
        private ManageMfaContext context;

        private string title = string.Empty;
        private string description = string.Empty;
        private string errorMessage = string.Empty;

        private bool isButtonInputEnabled = true;

        public string Title
        {
            get => title;
            set { title = value; OnPropertyChanged(nameof(Title)); }
        }
        public string Description
        {
            get => description;
            set { description = value; OnPropertyChanged(nameof(Description)); }
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



        // COMMANDS
        public ICommand ContinueButtonClickedCommand { get; }
        public ICommand CancelButtonClickedCommand { get; }



        public ManageMfaViewModel()
        {
            ContinueButtonClickedCommand = new ViewModelCommand(ExecuteContinueButtonClicked, CanExecuteContinueButtonClicked);
            CancelButtonClickedCommand = new ViewModelCommand(ExecuteCancelButtonClickedCommand, CanExecuteCancelButtonClickedCommand);
        }

        public void SetViewContext(ManageMfaContext context)
        {
            this.context = context;
            if (context == ManageMfaContext.ResetMfa)
            {
                Title = "Reset MFA configuration";
                Description = "Clicking continue will generate a new multi-factor authentication QR code for your account." +
                    " Note that this will not replace the current MFA setup until the new setup is verified in the next step.";
            }
            else if (context == ManageMfaContext.GenerateNewRecovery)
            {
                Title = "Generate new MFA recovery code";
                Description = "Clicking continue will generate a new MFA recovery code for the currently-active MFA setup." +
                    " Note that this will immediately replace the previously-generated MFA recovery code, rendering it obsolete.";
            }
            // Do nothing for none.
        }

        public override void ShowView()
        {
            ErrorMessage = string.Empty;
            Title = string.Empty;
            Description = string.Empty;
            IsButtonInputEnabled = true;

            isViewVisible = true;
        }
        
        public override void HideView()
        {
            isViewVisible = false;
        }



        #region Private: SubmitButtonClicked (async)

        private async Task ExecuteContinueButtonClicked(object? obj)
        {
            ErrorMessage = string.Empty;

            // Disable button input before awaiting to prevent button spam.
            IsButtonInputEnabled = false;

            // Make request to API and move onto correct view based on stored context.
            int statusCode; string response = string.Empty;
            switch (context)
            {
                case ManageMfaContext.ResetMfa:
                    {
                        // If resetting MFA, call setup MFA endpoint with our full-access token.
                        (statusCode, response) = await LoginApiService.BeginMfaSetup();
                        if (statusCode == 0)
                        {
                            // If status code is good, then we received a new QR code, so show setup MFA view.
                            MainViewModel.Instance.ShowMfaSetupView(MainViewModel.MfaContext.ManualSetup, response);
                            return;
                        }
                        break;
                    }
                case ManageMfaContext.GenerateNewRecovery:
                    {
                        // Call explicit regenerate method, which allows usage only with a full-access token.
                        (statusCode, response) = await LoginApiService.RegenerateMfaRecoveryCode();
                        if (statusCode == 0)
                        {
                            // Good status code will return the MFA recovery code in the response.
                            MainViewModel.Instance.ShowRecoveryCodeDisplayView(MainViewModel.MfaContext.ManualSetup, response);
                            return;
                        }
                        else if (statusCode >= 1 && statusCode < 100)
                        {
                            // If code 1, 10, 20, or 30 (success response code but bad account state), return to login view.
                            _ = LoginApiService.Logout();
                            MainViewModel.Instance.ShowReturnToLoginView(true,
                                "Could not regenerate recovery code, unexpected account state detected in response. Please log in again.");
                            return;
                        }
                        break;
                    }
                // Do nothing for None.
            }

            // Any non-success code means either unexpected error (exception) or legitimate HTTP status code error.
            ErrorMessage = response;
            IsButtonInputEnabled = true;
        }

        private bool CanExecuteContinueButtonClicked(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: CancelButtonClicked

        private void ExecuteCancelButtonClickedCommand(object? obj)
        {
            isButtonInputEnabled = false;

            // Simply return to account view.
            MainViewModel.Instance.ShowAccountView();
        }

        private bool CanExecuteCancelButtonClickedCommand(object? obj)
        {
            // Disallow click if main button is not enabled (means awaiting API response).
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

    }
}
