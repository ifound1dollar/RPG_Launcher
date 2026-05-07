using RPG_Launcher.ViewModel.Account;
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

namespace RPG_Launcher.View.Account
{
    /// <summary>
    /// Interaction logic for AccountView.xaml
    /// </summary>
    public partial class AccountView : UserControl
    {
        private AccountViewModel? vmRef;
        private AccountViewModel AccountVM
        {
            get
            {
                vmRef ??= (AccountViewModel)DataContext;
                return vmRef;
            }
        }

        public AccountView()
        {
            InitializeComponent();
        }
    }
}
