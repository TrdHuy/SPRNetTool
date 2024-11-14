using ArtWiz.Utils;
using ArtWiz.View.Base;
using ArtWiz.ViewModel;
using ArtWiz.ViewModel.Widgets;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ArtWiz.View.Widgets
{
    /// <summary>
    /// Interaction logic for WindowTitleBar.xaml
    /// </summary>
    public partial class WindowTitleBar : UserControl, IWindowTitleBar
    {
        public static readonly DependencyProperty CustomHeaderViewProperty =
          DependencyProperty.Register(
              "CustomHeaderView",
              typeof(object),
              typeof(WindowTitleBar),
               new FrameworkPropertyMetadata(default(object),
                   FrameworkPropertyMetadataOptions.AffectsRender, propertyChangedCallback: OnCustomHeaderChanged));

        private static void OnCustomHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.IfIs<WindowTitleBar>(it =>
            {
                if (e.NewValue != null)
                {
                    it.MainMenu.Visibility = Visibility.Collapsed;
                    it.CustomHeaderContainer.Visibility = Visibility.Visible;
                }
                else
                {
                    it.MainMenu.Visibility = Visibility.Visible;
                    it.CustomHeaderContainer.Visibility = Visibility.Collapsed;
                }
            });
        }

        public object CustomHeaderView
        {
            get { return GetValue(CustomHeaderViewProperty); }
            set { SetValue(CustomHeaderViewProperty, value); }
        }

        public static readonly DependencyProperty WindowBarHeightProperty =
            DependencyProperty.Register(
                "WindowBarHeight",
                typeof(double),
                typeof(WindowTitleBar),
                new FrameworkPropertyMetadata(
                    90d,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnTitleBarHeightChanged
                )
            );

        private static void OnTitleBarHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.IfIs<WindowTitleBar>(it =>
            {
                it.TitleBarHeightChanged?.Invoke(it, (double)e.OldValue, (double)e.NewValue);
            });
        }

        public double WindowBarHeight
        {
            get { return (double)GetValue(WindowBarHeightProperty); }
            set { SetValue(WindowBarHeightProperty, value); }
        }

        Button IWindowTitleBar.MinimizeButton => MinimizeButton;

        Button IWindowTitleBar.SmallmizeButton => SmallmizeButton;

        Button IWindowTitleBar.CloseButton => CloseButton;

        Button IWindowTitleBar.MaximizeButton => MaximizeButton;

        public double TitleBarHeight => WindowBarHeight;

        private MainWindowViewModel? mMainWindowViewModel;

        public event IWindowTitleBar.TitleBarHeightChangedHandler? TitleBarHeightChanged;

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
