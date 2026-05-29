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

        private bool isButtonInputEnabled = true;

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
        public bool IsButtonInputEnabled
        {
            get => isButtonInputEnabled;
            set { isButtonInputEnabled = value; OnPropertyChanged(nameof(IsButtonInputEnabled)); }
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
            IsButtonInputEnabled = true;

            isReturnToLoginViewVisible = true;
        }

        public override void HideView()
        {
            isReturnToLoginViewVisible = false;
        }



        #region Private: ReturnButtonClicked

        private void ExecuteReturnButtonClickedCommand(object? obj)
        {
            IsButtonInputEnabled = false;
            MainViewModel.Instance.ShowLoginView();
        }

        private bool CanExecuteReturnButtonClickedCommand(object? obj)
        {
            return isReturnToLoginViewVisible && isButtonInputEnabled;
        }



        #endregion
    }
}
