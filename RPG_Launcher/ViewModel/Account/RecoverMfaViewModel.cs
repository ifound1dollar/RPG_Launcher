using RPG_Launcher.Model;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RPG_Launcher.ViewModel.Account
{
    public class RecoverMfaViewModel : ViewModelBase
    {
        private bool isViewVisible = false;
        private MainViewModel.MfaContext context;

        private string recoveryCode = string.Empty;
        private string errorMessage = string.Empty;

        private bool isButtonInputEnabled = true;

        public string RecoveryCode
        {
            get => recoveryCode;
            set
            {
                recoveryCode = value;
                OnPropertyChanged(nameof(RecoveryCode));
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



        // COMMANDS
        public ICommand SubmitButtonClickedCommand { get; }
        public ICommand LostRecoveryCodeButtonClickedCommand { get; }
        public ICommand CancelButtonClickedCommand { get; }



        public RecoverMfaViewModel()
        {
            SubmitButtonClickedCommand = new ViewModelCommand(ExecuteSubmitButtonClickedCommand, CanExecuteSubmitButtonClickedCommand);
            LostRecoveryCodeButtonClickedCommand = new ViewModelCommand(ExecuteLostRecoveryCodeButtonClicked, CanExecuteLostRecoveryCodeButtonClicked);
            CancelButtonClickedCommand = new ViewModelCommand(ExecuteCancelButtonClickedCommand, CanExecuteCancelButtonClickedCommand);
        }

        public void SetViewContext(MainViewModel.MfaContext context)
        {
            this.context = context;
        }

        public override void ShowView()
        {
            RecoveryCode = string.Empty;
            ErrorMessage = string.Empty;
            IsButtonInputEnabled = true;

            isViewVisible = true;
        }

        public override void HideView()
        {
            isViewVisible = false;
        }



        #region Private: SubmitButtonClicked (async)

        private async Task ExecuteSubmitButtonClickedCommand(object? obj)
        {
            // Clear error message, then validate input.
            ErrorMessage = string.Empty;
            if (RecoveryCode.Length == 0)
            {
                ErrorMessage = "Recovery code cannot be empty.";
                return;
            }

            // Disable send code button before awaiting to prevent button spam.
            IsButtonInputEnabled = false;

            var (StatusCode, Response) = await LoginApiService.RecoverMfa(RecoveryCode);
            if (StatusCode != 0)
            {
                // If status code is bad, will just be an error message.
                ErrorMessage = Response;
                IsButtonInputEnabled = true;
                return;
            }

            // Else status code is good and we have a new QR code, so move onto SetupMfaView.
            MainViewModel.Instance.ShowMfaSetupView(MainViewModel.MfaContext.RecoverySetup, Response);
        }

        private bool CanExecuteSubmitButtonClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: LostRecoveryCodeButtonClicked

        private void ExecuteLostRecoveryCodeButtonClicked(object? obj)
        {
            MainViewModel.Instance.ShowManageMfaView(ManageMfaViewModel.ManageMfaContext.HardReset);
        }

        private bool CanExecuteLostRecoveryCodeButtonClicked(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

        #region Private: CancelButtonClicked

        private void ExecuteCancelButtonClickedCommand(object? obj)
        {
            // Recover view is only accessible when not logged in, so return to login view (fully log out first).
            // NOTE: Do not await logout (fire-and-forget).
            _ = LoginApiService.Logout();

            MainViewModel.Instance.ShowLoginView();
        }

        private bool CanExecuteCancelButtonClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }

        #endregion

    }
}
