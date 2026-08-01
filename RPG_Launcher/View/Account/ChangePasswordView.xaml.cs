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
    /// Interaction logic for ChangePasswordView.xaml
    /// </summary>
    public partial class ChangePasswordView : UserControl
    {
        private ChangePasswordViewModel? vmRef;
        private ChangePasswordViewModel ChangePasswordVM
        {
            get
            {
                vmRef ??= (ChangePasswordViewModel)DataContext;
                return vmRef;
            }
        }

        public ChangePasswordView()
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
                ButtonSubmit_Click(sender, e);
            }
        }

        private void PasswordBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((PasswordBox)sender).Dispatcher.BeginInvoke(new Action(() => ((PasswordBox)sender).SelectAll()));
        }



        // BELOW METHODS TECHNICALLY VIOLATE MVVM PATTERN, BUT THIS IS NECESSARY FOR SECURITY (CLEARING PASSWORDBOX).

        private void TextBoxCurrentPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Update ViewModel directly when this password box is updated. This includes after Clear() is called.
            ChangePasswordVM.CurrentPassword = (TextBoxCurrentPassword.SecurePassword);

            // Clear the error message whenever the text box updates UNLESS it has just been cleared. This must be
            //  done here instead of in the ViewModel because when we clear the password box, automatically clearing
            //  error message in the ViewModel will hide an error message before the user can read it.
            if (TextBoxCurrentPassword.Password.Length > 0)
            {
                ChangePasswordVM.ErrorMessage = string.Empty;
            }
        }
        private void TextBoxNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Update ViewModel directly when this password box is updated. This includes after Clear() is called.
            ChangePasswordVM.NewPassword = (TextBoxNewPassword.SecurePassword);

            // Clear the error message whenever the text box updates UNLESS it has just been cleared. This must be
            //  done here instead of in the ViewModel because when we clear the password box, automatically clearing
            //  error message in the ViewModel will hide an error message before the user can read it.
            if (TextBoxNewPassword.Password.Length > 0)
            {
                ChangePasswordVM.ErrorMessage = string.Empty;
            }
        }
        private void TextBoxConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Clear the error message whenever the confirm password box updates. Same reasoning as above.
            if (TextBoxConfirmPassword.Password.Length > 0)
            {
                ChangePasswordVM.ErrorMessage = string.Empty;
            }
        }



        private void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {
            // Before calling Command in ViewModel, verify that both PasswordBoxes match. We cannot easily compare
            //  SecurePasswords in the ViewModel, so we break the MVVM pattern here to compare.
            if (TextBoxNewPassword.Password != TextBoxConfirmPassword.Password)
            {
                // Directly update ErrorMessage in ViewModel, which is weird but necessary here.
                TextBoxConfirmPassword.Clear();
                ChangePasswordVM.ErrorMessage = "New password and confirm password must match.";
                return;
            }

            // Directly call ViewModel Command. Always clear passwords immedately on click, regardless of success.
            if (ChangePasswordVM.SubmitButtonClickedCommand.CanExecute(sender))
            {
                ChangePasswordVM.SubmitButtonClickedCommand.Execute(sender);

                ClearPasswords();
            }
        }

        private void CancelTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ChangePasswordVM.CancelButtonClickedCommand.CanExecute(sender))
            {
                ChangePasswordVM.CancelButtonClickedCommand.Execute(sender);

                ClearPasswords();
            }
        }



        private void ClearPasswords()
        {
            // Clear password boxes and ViewModel properties.
            TextBoxCurrentPassword.Clear();
            TextBoxNewPassword.Clear();
            TextBoxConfirmPassword.Clear();
            ChangePasswordVM.CurrentPassword.Clear();
            ChangePasswordVM.NewPassword.Clear();
        }
    }
}
