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
    /// Interaction logic for VerificationCodeView.xaml
    /// </summary>
    public partial class VerificationCodeView : UserControl
    {
        private VerificationCodeViewModel? vmRef;
        private VerificationCodeViewModel VerificationCodeVM
        {
            get
            {
                vmRef ??= (VerificationCodeViewModel)DataContext;
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



        private void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (VerificationCodeVM.SubmitButtonClicked.CanExecute(sender))
            {
                VerificationCodeVM.SubmitButtonClicked.Execute(sender);
            }
        }

        private void ResendCodeTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (VerificationCodeVM.ResendCodeButtonClicked.CanExecute(sender))
            {
                VerificationCodeVM.ResendCodeButtonClicked.Execute(sender);
            }
        }
    }
}
