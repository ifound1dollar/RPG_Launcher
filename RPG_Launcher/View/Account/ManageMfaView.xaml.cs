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
    /// Interaction logic for ManageMfaView.xaml
    /// </summary>
    public partial class ManageMfaView : UserControl
    {
        private ManageMfaViewModel? vmRef;
        private ManageMfaViewModel ManageMfaVM
        {
            get
            {
                vmRef ??= (ManageMfaViewModel)DataContext;
                return vmRef;
            }
        }

        public ManageMfaView()
        {
            InitializeComponent();
        }

        private void ButtonContinue_Click(object sender, RoutedEventArgs e)
        {
            if (ManageMfaVM.ContinueButtonClickedCommand.CanExecute(sender))
            {
                ManageMfaVM.ContinueButtonClickedCommand.Execute(sender);
            }
        }

        private void CancelTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ManageMfaVM.CancelButtonClickedCommand.CanExecute(sender))
            {
                ManageMfaVM.CancelButtonClickedCommand.Execute(sender);
            }
        }
    }
}
