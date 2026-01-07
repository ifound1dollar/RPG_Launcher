using RPG_Launcher.ViewModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RPG_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel MainViewModel { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            // Set DataContext to a new ViewModel instance. Follows the MVVM pattern.
            MainViewModel = new MainViewModel();
            DataContext = MainViewModel;
        }



        private void Universal_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Whenever a click is handled by this event, it means that an individual component has not processed
            //  the input (ex. a text box or button will have its own mouse down event). Since we are not processing
            //  input for real, we should always clear focus (this allows users to click anywhere to de-select a
            //  text box).
            // The title bar movement below still works if we do this.
            Keyboard.ClearFocus();

            // If mouse down is in the title bar area, allow moving the window.
            // We can dynamically retrieve the title bar height if valid, else we default to 30px.
            double titleBarHeight = (TitleBarRow != null) ? TitleBarRow.Height.Value : 30.0d;
            if (e.LeftButton == MouseButtonState.Pressed && e.GetPosition(this).Y <= titleBarHeight)    // Top left is 0,0:x,y
            {
                DragMove();
            }
        }
        private void Universal_KeyDown(object sender, KeyEventArgs e)
        {
            // On enter key pressed, fire the same command as the login button.
            if (e.Key == Key.Enter)
            {
                // Call Click method, which is necessary for PasswordBox behavior (calls ViewModel command in method).
                ButtonLogin_Click(sender, e);
            }
        }




        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }



        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }



        private void ForgotPasswordTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }



        // BELOW METHODS TECHNICALLY VIOLATE MVVM PATTERN, BUT THIS IS NECESSARY FOR SECURITY (CLEARING PASSWORDBOX).

        private void TextBoxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Update MainViewModel directly when this password box is updated. This includes after Clear() is called.
            MainViewModel.LoginPassword = TextBoxPassword.SecurePassword;
        }

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            // Will only allow execution if login subgrid is visible (checks within MainViewModel).
            if (MainViewModel.LoginClickedCommand.CanExecute(sender))
            {
                // Execute command according to MVVM pattern, then also call Click method below to clear field.
                MainViewModel.LoginClickedCommand.Execute(sender);

                // We should be immediately clearing the PasswordBox when we attempt login, successful or not.
                TextBoxPassword.Clear();
            }

        }
    }
}