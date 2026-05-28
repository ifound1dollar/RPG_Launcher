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
    public class RecoveryCodeDisplayViewModel : ViewModelBase
    {
        private bool isViewVisible = false;
        private MainViewModel.MfaContext context;

        private string recoveryCode = string.Empty;

        private bool isButtonInputEnabled = true;

        public string RecoveryCode
        {
            get => recoveryCode;
            set
            {
                // Add spaces every 4 characters in the string.
                StringBuilder sb = new();
                for (int i = 0; i < value.Length; i++)
                {
                    if (i != 0 && i % 4 == 0) sb.Append(' ');   // Insert space before, except for first.
                    sb.Append(value[i]);
                }
                recoveryCode = sb.ToString();
                OnPropertyChanged(nameof(RecoveryCode));
            }
        }
        public bool IsButtonInputEnabled
        {
            get => isButtonInputEnabled;
            set { isButtonInputEnabled = value; OnPropertyChanged(nameof(IsButtonInputEnabled)); }
        }



        // COMMANDS
        public ICommand DoneButtonClickedCommand { get; }



        public RecoveryCodeDisplayViewModel()
        {
            DoneButtonClickedCommand = new ViewModelCommand(ExecuteDoneButtonClickedCommand, CanExecuteDoneButtonClickedCommand);
        }

        public void SetViewContext(MainViewModel.MfaContext context)
        {
            this.context = context;
        }

        public override void ShowView()
        {
            RecoveryCode = string.Empty;
            IsButtonInputEnabled = true;

            isViewVisible = true;
        }

        public override void HideView()
        {
            isViewVisible = false;
        }



        #region Private: ReturnButtonClicked

        private void ExecuteDoneButtonClickedCommand(object? obj)
        {
            // Disable return button before awaiting to prevent button spam.
            IsButtonInputEnabled = false;

            // Move onto home view if login/recover or initial setup, else account view for manual setup.
            if (context == MainViewModel.MfaContext.ManualSetup)
            {
                MainViewModel.Instance.ShowAccountView();
            }
            else
            {
                MainViewModel.Instance.ShowHomeView();
            }
        }

        private bool CanExecuteDoneButtonClickedCommand(object? obj)
        {
            return isViewVisible && isButtonInputEnabled;
        }



        #endregion

    }
}
