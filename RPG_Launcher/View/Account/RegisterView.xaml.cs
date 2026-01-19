using RPG_Launcher.ViewModel.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for RegisterView.xaml
    /// </summary>
    public partial class RegisterView : UserControl
    {
        private RegisterViewModel? vmRef;
        private RegisterViewModel RegisterVM
        {
            get
            {
                vmRef ??= (RegisterViewModel)DataContext;
                return vmRef;
            }
        }

        public RegisterView()
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
                ButtonRegister_Click(sender, e);
            }
        }





        // BELOW METHODS TECHNICALLY VIOLATE MVVM PATTERN, BUT THIS IS NECESSARY FOR SECURITY (CLEARING PASSWORDBOX).

        private void TextBoxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Update ViewModel directly when this password box is updated. This includes after Clear() is called.
            RegisterVM.SecurePassword = (TextBoxPassword.SecurePassword);
        }

        private void ButtonRegister_Click(object sender, RoutedEventArgs e)
        {
            // Before calling Command in ViewModel, verify that both PasswordBoxes match. We cannot easily compare
            //  SecurePasswords in the ViewModel, so we break the MVVM pattern here to compare.
            if (TextBoxPassword.Password != TextBoxConfirmPassword.Password)
            {
                // Directly update ErrorMessage in ViewModel, which is weird but necessary here.
                RegisterVM.ErrorMessage = "Both password fields must match.";
                ClearPasswords();
                return;
            }

            // Directly call ViewModel Command. Always clear passwords immedately on click, regardless of success.
            if (RegisterVM.RegisterClickedCommand.CanExecute(sender))
            {
                RegisterVM.RegisterClickedCommand.Execute(sender);
                ClearPasswords();
            }
        }

        private void AlreadyHaveTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (RegisterVM.AlreadyHaveClickedCommand.CanExecute(sender))
            {
                RegisterVM.AlreadyHaveClickedCommand.Execute(sender);

                // Always immediately clear password boxes and ViewModel property.
                ClearPasswords();
            }
        }



        private void ClearPasswords()
        {
            // Clear both password boxes and ViewModel property.
            TextBoxPassword.Clear();
            RegisterVM.SecurePassword.Clear();
            TextBoxConfirmPassword.Clear();
        }
    }
}
