using RPG_Launcher.ViewModel.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RPG_Launcher.View
{
    /// <summary>
    /// Interaction logic for VerificationCodeView.xaml
    /// </summary>
    public partial class VerificationCodeView : UserControl
    {
        private ConfirmationCodeViewModel? vmRef;
        private ConfirmationCodeViewModel VerificationCodeVM
        {
            get
            {
                vmRef ??= (ConfirmationCodeViewModel)DataContext;
                return vmRef;
            }
        }

        public VerificationCodeView()
        {
            InitializeComponent();
        }

        private void Universal_KeyDown(object sender, KeyEventArgs e)
        {
            // On enter key pressed, fire the same command as the submit button.
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();

                ButtonSubmit_Click(sender, e);
            }
        }

        private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // This will generically select all text in the box when it gets keyboard focus.
            ((TextBox)sender).Dispatcher.BeginInvoke(new Action(() => ((TextBox)sender).SelectAll()));
        }



        private void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (VerificationCodeVM.SubmitButtonClickedCommand.CanExecute(sender))
            {
                VerificationCodeVM.SubmitButtonClickedCommand.Execute(sender);
            }
        }

        private void ResendCodeTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (VerificationCodeVM.ResendCodeButtonClickedCommand.CanExecute(sender))
            {
                VerificationCodeVM.ResendCodeButtonClickedCommand.Execute(sender);
            }
        }

        private void CancelTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (VerificationCodeVM.CancelButtonClickedCommand.CanExecute(sender))
            {
                VerificationCodeVM.CancelButtonClickedCommand.Execute(sender);
            }
        }
    }
}
