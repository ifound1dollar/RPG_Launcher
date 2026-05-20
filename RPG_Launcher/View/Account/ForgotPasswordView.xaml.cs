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
    /// Interaction logic for ForgotPasswordView.xaml
    /// </summary>
    public partial class ForgotPasswordView : UserControl
    {
        private ForgotPasswordViewModel? vmRef;
        private ForgotPasswordViewModel ForgotPasswordVM
        {
            get
            {
                vmRef ??= (ForgotPasswordViewModel)DataContext;
                return vmRef;
            }
        }

        public ForgotPasswordView()
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

        private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // This will generically select all text in the box when it gets keyboard focus.
            ((TextBox)sender).Dispatcher.BeginInvoke(new Action(() => ((TextBox)sender).SelectAll()));
        }



        private void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (ForgotPasswordVM.SubmitButtonClickedCommand.CanExecute(sender))
            {
                ForgotPasswordVM.SubmitButtonClickedCommand.Execute(sender);
            }
        }

        private void ReturnToLoginTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ForgotPasswordVM.ReturnToLoginClickedCommand.CanExecute(sender))
            {
                ForgotPasswordVM.ReturnToLoginClickedCommand.Execute(sender);
            }
        }
    }
}
