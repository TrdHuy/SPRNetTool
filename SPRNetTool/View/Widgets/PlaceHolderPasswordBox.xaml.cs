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

namespace ArtWiz.View.Widgets
{
    /// <summary>
    /// Interaction logic for PlaceHolderPasswordBox.xaml
    /// </summary>
    public partial class PlaceHolderPasswordBox : UserControl
    {
        // Dependency Property for Placeholder
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(
                nameof(Placeholder),
                typeof(string),
                typeof(PlaceHolderPasswordBox),
                new PropertyMetadata(string.Empty));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        // Dependency Property for Password
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(PlaceHolderPasswordBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public static readonly DependencyProperty PlaceholderForegroundProperty =
            DependencyProperty.Register(
                nameof(PlaceholderForeground),
                typeof(Brush),
                typeof(PlaceHolderPasswordBox),
                new FrameworkPropertyMetadata(SystemColors.ControlTextBrush,
                            FrameworkPropertyMetadataOptions.Inherits));

        public Brush PlaceholderForeground
        {
            get => (Brush)GetValue(PlaceholderForegroundProperty);
            set => SetValue(PlaceholderForegroundProperty, value);
        }
        public PlaceHolderPasswordBox()
        {
            InitializeComponent();
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            Password = PasswordBoxInput.Password;
        }
    }
}
