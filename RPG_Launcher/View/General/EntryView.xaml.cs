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
    /// Interaction logic for EntryView.xaml
    /// </summary>
    public partial class EntryView : UserControl
    {
        private EntryViewModel? vmRef;
        private EntryViewModel EntryVM
        {
            get
            {
                vmRef ??= (EntryViewModel)DataContext;
                return vmRef;
            }
        }

        public EntryView()
        {
            InitializeComponent();
        }



        private void ButtonRetry_Click(object sender, RoutedEventArgs e)
        {
            if (EntryVM.PingServerCommand.CanExecute(sender))
            {
                EntryVM.PingServerCommand.Execute(sender);
            }
        }
    }
}
