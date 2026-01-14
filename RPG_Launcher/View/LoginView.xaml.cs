using RPG_Launcher.ViewModel;
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
        private LoginViewModel LoginVM { get; }

        public LoginView()
        {
            InitializeComponent();

            LoginVM = MainViewModel.Instance.LoginVM;
        }

        private void Universal_KeyDown(object sender, KeyEventArgs e)
        {
            // On enter key pressed, fire the same command as the login button.
            if (e.Key == Key.Enter)
            {
                // Call Click method, which is necessary for PasswordBox behavior (calls ViewModel command in method).
                ButtonLogin_Click(sender, e);
            }
        }



        // BELOW METHODS TECHNICALLY VIOLATE MVVM PATTERN, BUT THIS IS NECESSARY FOR SECURITY (CLEARING PASSWORDBOX).

        private void TextBoxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Update ViewModel directly when this password box is updated. This includes after Clear() is called.
            LoginVM.SecurePassword = (TextBoxPassword.SecurePassword);
        }

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            // Will only allow execution if login subgrid is visible (checks within MainViewModel).
            if (LoginVM.LoginClickedCommand.CanExecute(sender))
            {
                // Execute command according to MVVM pattern, then also call Click method below to clear field.
                LoginVM.LoginClickedCommand.Execute(sender);

                // We should be immediately clearing the PasswordBox when we attempt login, successful or not.
                TextBoxPassword.Clear();
                LoginVM.SecurePassword.Clear();     // Also clear ViewModel property, in case it isnt automatically cleared.
            }

        }

        private void ForgotPasswordTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void NewUserTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (LoginVM.NewUserClickedCommand.CanExecute(sender))
            {
                LoginVM.NewUserClickedCommand.Execute(sender);

                // Always immediately clear password box and ViewModel property.
                TextBoxPassword.Clear();
                LoginVM.SecurePassword.Clear();     // Also clear ViewModel property, in case it isnt automatically cleared.
            }
        }
    }
}
