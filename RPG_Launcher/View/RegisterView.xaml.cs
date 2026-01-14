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
    /// Interaction logic for RegisterView.xaml
    /// </summary>
    public partial class RegisterView : UserControl
    {
        private RegisterViewModel RegisterVM;

        public RegisterView()
        {
            InitializeComponent();

            RegisterVM = MainViewModel.Instance.RegisterVM;
        }

        private void Universal_KeyDown(object sender, KeyEventArgs e)
        {
            // On enter key pressed, fire the same command as the login button.
            if (e.Key == Key.Enter)
            {
                // Call Click method, which is necessary for PasswordBox behavior (calls ViewModel command in method).
                ButtonRegister_Click(sender, e);
            }
        }



        private void TextBoxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Update ViewModel directly when this password box is updated. This includes after Clear() is called.
            RegisterVM.SecurePassword = (TextBoxPassword.SecurePassword);
        }

        private void ButtonRegister_Click(object sender, RoutedEventArgs e)
        {
            if (RegisterVM.RegisterClickedCommand.CanExecute(sender))
            {
                RegisterVM.RegisterClickedCommand.Execute(sender);

                // Always immediately clear password box and ViewModel property.
                TextBoxPassword.Clear();
                RegisterVM.SecurePassword.Clear();
            }
        }

        private void AlreadyHaveTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (RegisterVM.AlreadyHaveClickedCommand.CanExecute(sender))
            {
                RegisterVM.AlreadyHaveClickedCommand.Execute(sender);

                // Always immediately clear password box and ViewModel property.
                TextBoxPassword.Clear();
                RegisterVM.SecurePassword.Clear();
            }
        }

    }
}
