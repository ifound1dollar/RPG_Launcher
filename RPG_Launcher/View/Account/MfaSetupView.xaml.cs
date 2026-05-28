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
    /// Interaction logic for MfaSetupView.xaml
    /// </summary>
    public partial class MfaSetupView : UserControl
    {
        private MfaSetupViewModel? vmRef;
        private MfaSetupViewModel MfaSetupVM
        {
            get
            {
                vmRef ??= (MfaSetupViewModel)DataContext;
                return vmRef;
            }
        }

        public MfaSetupView()
        {
            InitializeComponent();
        }



        private void ButtonContinue_Click(object sender, RoutedEventArgs e)
        {
            if (MfaSetupVM.ContinueButtonClickedCommand.CanExecute(sender))
            {
                MfaSetupVM.ContinueButtonClickedCommand.Execute(sender);
            }
        }

        private void CancelTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MfaSetupVM.CancelButtonClickedCommand.CanExecute(sender))
            {
                MfaSetupVM.CancelButtonClickedCommand.Execute(sender);
            }
        }
    }
}
