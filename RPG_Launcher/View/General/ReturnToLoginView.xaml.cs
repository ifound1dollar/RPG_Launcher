using RPG_Launcher.ViewModel.General;
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
    /// Interaction logic for ReturnToLoginView.xaml
    /// </summary>
    public partial class ReturnToLoginView : UserControl
    {
        private ReturnToLoginViewModel? vmRef;
        private ReturnToLoginViewModel ReturnToLoginVM
        {
            get
            {
                vmRef ??= (ReturnToLoginViewModel)DataContext;
                return vmRef;
            }
        }

        public ReturnToLoginView()
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
                ButtonReturn_Click(sender, e);
            }
        }



        private void ButtonReturn_Click(object sender, RoutedEventArgs e)
        {
            if (ReturnToLoginVM.ReturnButtonClickedCommand.CanExecute(sender))
            {
                ReturnToLoginVM.ReturnButtonClickedCommand.Execute(sender);
            }
        }
    }
}
