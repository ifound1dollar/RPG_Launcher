using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
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

namespace RPG_Launcher.CustomControls
{
    /// <summary>
    /// Interaction logic for BindablePasswordBox.xaml
    /// </summary>
    public partial class BindablePasswordBox : UserControl
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(nameof(Password), typeof(SecureString), typeof(BindablePasswordBox),
                new PropertyMetadata(OnSourcePropertyChanged));

        // This property retrieves the password within the password box, accessed via static PasswordProperty.
        public SecureString Password
        {
            get => (SecureString)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }



        public BindablePasswordBox()
        {
            InitializeComponent();

            // Subscribe to the PasswordChanged event (in the built-in component) using a custom method.
            TextBoxPassword.PasswordChanged += OnPasswordChanged;
        }



        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            // Calling Password.Clear() in LoginViewModel winds up calling this method, which resets the
            //  just-cleared property back to what it was.
            Password = TextBoxPassword.SecurePassword;
        }

        private static void OnSourcePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
            {
                if (dependencyObject is BindablePasswordBox control)
                {
                    control.TextBoxPassword.Password = string.Empty;
                }
            }
        }
    }
}
