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
    /// Interaction logic for ChangeUsernameView.xaml
    /// </summary>
    public partial class ChangeUsernameView : UserControl
    {
        private ChangeUsernameViewModel? vmRef;
        private ChangeUsernameViewModel ChangeUsernameVM
        {
            get
            {
                vmRef ??= (ChangeUsernameViewModel)DataContext;
                return vmRef;
            }
        }

        public ChangeUsernameView()
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



        private void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {
            // Directly call ViewModel Command. Always clear passwords immedately on click, regardless of success.
            if (ChangeUsernameVM.SubmitButtonClickedCommand.CanExecute(sender))
            {
                ChangeUsernameVM.SubmitButtonClickedCommand.Execute(sender);
            }
        }

        private void CancelTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ChangeUsernameVM.CancelButtonClickedCommand.CanExecute(sender))
            {
                ChangeUsernameVM.CancelButtonClickedCommand.Execute(sender);
            }
        }
    }
}
