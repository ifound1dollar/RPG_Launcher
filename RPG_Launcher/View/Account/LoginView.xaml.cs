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
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        private LoginViewModel? vmRef;
        private LoginViewModel LoginVM
        {
            get
            {
                vmRef ??= (LoginViewModel)DataContext;
                return vmRef;
            }
        }

        public LoginView()
        {
            InitializeComponent();
        }

        private void Universal_KeyDown(object sender, KeyEventArgs e)
        {
            // On enter key pressed, fire the same command as the login button.
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();

                // Call Click method, which is necessary for PasswordBox behavior (calls ViewModel command in method).
                ButtonLogin_Click(sender, e);
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

        private void TextBoxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Update ViewModel directly when this password box is updated. This includes after Clear() is called.
            LoginVM.SecurePassword = (TextBoxPassword.SecurePassword);
        }

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            // Execute command according to MVVM pattern.
            if (LoginVM.LoginClickedCommand.CanExecute(sender))
            {
                LoginVM.LoginClickedCommand.Execute(sender);

                // We should be immediately clearing the PasswordBox when we attempt login, successful or not.
                ClearPasswords();
            }

        }

        private void ForgotPasswordTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (LoginVM.ForgotPasswordClickedCommand.CanExecute(sender))
            {
                LoginVM.ForgotPasswordClickedCommand.Execute(sender);

                ClearPasswords();
            }
        }

        private void NewUserTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (LoginVM.NewUserClickedCommand.CanExecute(sender))
            {
                LoginVM.NewUserClickedCommand.Execute(sender);

                // Always immediately clear password box and ViewModel property.
                ClearPasswords();
            }
        }



        private void ClearPasswords()
        {
            // Clear both password boxes and ViewModel property.
            TextBoxPassword.Clear();
            LoginVM.SecurePassword.Clear();
        }
    }
}
