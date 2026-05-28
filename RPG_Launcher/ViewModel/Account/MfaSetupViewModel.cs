using QRCoder;
using QRCoder.Xaml;
using RPG_Launcher.Model;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using static RPG_Launcher.ViewModel.Account.SubmitMfaCodeViewModel;

namespace RPG_Launcher.ViewModel.Account
{
    public class MfaSetupViewModel : ViewModelBase
    {
        private bool isViewVisible = false;
        private MainViewModel.MfaContext context;

        private string otpAuthLink = string.Empty;

        private bool isButtonInputEnabled = true;

        public ImageSource QRCodeImage
        {
            get
            {
                QRCodeGenerator generator = new();
                QRCodeData data = generator.CreateQrCode(OtpAuthLink, QRCodeGenerator.ECCLevel.Q);
                XamlQRCode code = new(data);
                return code.GetGraphic(6);
            }
        }
        public string OtpAuthLink
        {
            get => otpAuthLink;
            set { otpAuthLink = value; OnPropertyChanged(nameof(OtpAuthLink)); }
        }
        public bool IsButtonInputEnabled
        {
            get => isButtonInputEnabled;
            set { isButtonInputEnabled = value; OnPropertyChanged(nameof(IsButtonInputEnabled)); }
        }



        // Commands
        public ICommand ContinueButtonClickedCommand { get; }
        public ICommand CancelButtonClickedCommand { get; }



        public MfaSetupViewModel()
        {
            ContinueButtonClickedCommand = new ViewModelCommand(ExecuteContinueButtonClickedCommand, CanExecuteContinueButtonClickedCommand);
            CancelButtonClickedCommand = new ViewModelCommand(ExecuteCancelButtonClickedCommand, CanExecuteCancelButtonClickedCommand);
        }

        public void SetViewContext(MainViewModel.MfaContext context)
        {
            this.context = context;
        }

        public override void ShowView()
        {
            OtpAuthLink = string.Empty;
            IsButtonInputEnabled = true;

            isViewVisible = true;
        }

        public override void HideView()
        {
            isViewVisible = false;
        }



        #region Private: ContinueButtonClicked

        private void ExecuteContinueButtonClickedCommand(object? obj)
        {
            // Clear OtpAuthLink immediately on continue.
            OtpAuthLink = string.Empty;

            // Disable send code button before awaiting to prevent button spam.
            IsButtonInputEnabled = false;

            // Move onto submit view, passing it our current context.
            MainViewModel.Instance.ShowSubmitMfaCodeView(context);
        }

        private bool CanExecuteContinueButtonClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: CancelButtonClicked

        private void ExecuteCancelButtonClickedCommand(object? obj)
        {
            // Determine what view to return to based on context.
            switch (context)
            {
                case MainViewModel.MfaContext.InitialSetup:
                case MainViewModel.MfaContext.RecoverySetup:
                case MainViewModel.MfaContext.MfaLogin:
                    {
                        // Cancelling on non-manual setup OR login means we do not have access so logout and return to login.
                        _ = LoginApiService.Logout();   // Do not await logout.

                        MainViewModel.Instance.ShowLoginView();
                        break;
                    }
                case MainViewModel.MfaContext.ManualSetup:
                    {
                        // Cancelling manual setup means we DO have access, so return to account view.
                        MainViewModel.Instance.ShowAccountView();
                        break;
                    }
            }
        }

        private bool CanExecuteCancelButtonClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion
    }
}
