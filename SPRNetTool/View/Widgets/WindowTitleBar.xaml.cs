using ArtWiz.Utils;
using ArtWiz.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ArtWiz.View.Widgets
{
    /// <summary>
    /// Interaction logic for WindowTitleBar.xaml
    /// </summary>
    public partial class WindowTitleBar : UserControl
    {
        private MainWindowViewModel? mMainWindowViewModel;

        public WindowTitleBar()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            mMainWindowViewModel.IfNotNull(it => it.PropertyChanged -= OnViewModelPropertyChanged);
            mMainWindowViewModel = DataContext.IfIsThenAlso<MainWindowViewModel>(it => it);
            mMainWindowViewModel.IfNotNull(it =>
            {
                it.PropertyChanged += OnViewModelPropertyChanged;
                DeveloperModeMenu.Visibility = it.IsDebugMode ? Visibility.Visible : Visibility.Collapsed;
            }
            );

        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {

        }
    }
}
