using RPG_Launcher.Model;
using RPG_Launcher.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace RPG_Launcher.ViewModel.General
{
    public class ReturnToLoginViewModel : ViewModelBase
    {
        private readonly Brush infoBrush = Brushes.CornflowerBlue;
        private readonly Brush errorBrush = Brushes.IndianRed;

        private bool isReturnToLoginViewVisible = false;

        private string statusMessage = string.Empty;
        private Brush messageBrush = Brushes.White;

        private bool isReturnButtonEnabled = true;

        public string StatusMessage
        {
            get => statusMessage;
            set { statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }
        public Brush MessageBrush
        {
            get => messageBrush;
            set { messageBrush = value; OnPropertyChanged(nameof(MessageBrush)); }
        }
        public bool IsReturnButtonEnabled
        {
            get => isReturnButtonEnabled;
            set { isReturnButtonEnabled = value; OnPropertyChanged(nameof(IsReturnButtonEnabled)); }
        }



        // Commands
        public ICommand ReturnButtonClickedCommand { get; }


        public ReturnToLoginViewModel()
        {
            ReturnButtonClickedCommand = new ViewModelCommand(ExecuteReturnButtonClickedCommand, CanExecuteReturnButtonClickedCommand);
        }

        public void SetMessageIsError(bool isError)
        {
            MessageBrush = (isError) ? errorBrush : infoBrush;
        }

        public override void ShowView()
        {
            StatusMessage = string.Empty;
            MessageBrush = Brushes.White;
            IsReturnButtonEnabled = true;

            isReturnToLoginViewVisible = true;
        }

        public override void HideView()
        {
            isReturnToLoginViewVisible = false;
        }



        #region Private: ReturnButtonClicked

        private void ExecuteReturnButtonClickedCommand(object? obj)
        {
            // Disable return button before awaiting to prevent button spam.
            IsReturnButtonEnabled = false;

            // We may have already logged out, but log out again just to be sure. 
            // NOTE: We do not await logout (fire-and-forget).
            _ = LoginApiService.Instance.Logout();

            MainViewModel.Instance.ShowLoginView();
        }

        private bool CanExecuteReturnButtonClickedCommand(object? obj)
        {
            return isReturnToLoginViewVisible;
        }



        #endregion
    }
}
