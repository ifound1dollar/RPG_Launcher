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

namespace RPG_Launcher.View.Account
{
    /// <summary>
    /// Interaction logic for SubmitNewEmailView.xaml
    /// </summary>
    public partial class SubmitNewEmailView : UserControl
    {
        private SubmitNewEmailViewModel? vmRef;
        private SubmitNewEmailViewModel SubmitNewEmailVM
        {
            get
            {
                vmRef ??= (SubmitNewEmailViewModel)DataContext;
                return vmRef;
            }
        }

        public SubmitNewEmailView()
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
        private void PasswordBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((PasswordBox)sender).Dispatcher.BeginInvoke(new Action(() => ((PasswordBox)sender).SelectAll()));
        }



        // BELOW METHODS TECHNICALLY VIOLATE MVVM PATTERN, BUT THIS IS NECESSARY FOR SECURITY (CLEARING PASSWORDBOX).

        private void TextBoxCurrentPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Update ViewModel directly when this password box is updated. This includes after Clear() is called.
            SubmitNewEmailVM.CurrentPassword = (TextBoxCurrentPassword.SecurePassword);
        }



        private void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (SubmitNewEmailVM.SubmitButtonClickedCommand.CanExecute(sender))
            {
                SubmitNewEmailVM.SubmitButtonClickedCommand.Execute(sender);

                ClearPasswords();
            }
        }

        private void CancelTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SubmitNewEmailVM.CancelButtonClickedCommand.CanExecute(sender))
            {
                SubmitNewEmailVM.CancelButtonClickedCommand.Execute(sender);

                ClearPasswords();
            }
        }



        private void ClearPasswords()
        {
            // Clear both password boxes and ViewModel property.
            TextBoxCurrentPassword.Clear();
            SubmitNewEmailVM.CurrentPassword.Clear();
        }
    }
}
