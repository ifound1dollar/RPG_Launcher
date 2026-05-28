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
    /// Interaction logic for RecoveryCodeDisplayView.xaml
    /// </summary>
    public partial class RecoveryCodeDisplayView : UserControl
    {
        private RecoveryCodeDisplayViewModel? vmRef;
        private RecoveryCodeDisplayViewModel RecoveryCodeDisplayVM
        {
            get
            {
                vmRef ??= (RecoveryCodeDisplayViewModel)DataContext;
                return vmRef;
            }
        }

        public RecoveryCodeDisplayView()
        {
            InitializeComponent();
        }



        private void ButtonDone_Click(object sender, RoutedEventArgs e)
        {
            if (RecoveryCodeDisplayVM.DoneButtonClickedCommand.CanExecute(sender))
            {
                RecoveryCodeDisplayVM.DoneButtonClickedCommand.Execute(sender);
            }
        }
    }
}
