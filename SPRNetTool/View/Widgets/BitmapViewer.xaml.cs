using ArtWiz.Utils;
using ArtWiz.ViewModel.Widgets;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ArtWiz.View.Widgets
{
    /// <summary>
    /// Interaction logic for BitmapViewer.xaml
    /// </summary>
    public partial class BitmapViewer : UserControl
    {
        private ImageSource BlackBagroundImage { get; }
           = new BitmapImage(new Uri(@"/ArtWiz;component/resources/spr_global_background.png", UriKind.RelativeOrAbsolute));
        private ImageSource TransparentBagroundImage { get; }
           = new BitmapImage(new Uri(@"/ArtWiz;component/resources/spr_global_transparent_background.png", UriKind.RelativeOrAbsolute));

        public static readonly DependencyProperty FooterProperty =
           DependencyProperty.Register(
               "Footer",
               typeof(object),
               typeof(BitmapViewer),
               new PropertyMetadata(default(object)));

        public object Footer
        {
            get { return GetValue(FooterProperty); }
            set { SetValue(FooterProperty, value); }
        }

        public static readonly DependencyProperty ShowLoadingIconProperty =
           DependencyProperty.Register(
               "ShowLoadingIcon",
               typeof(bool),
               typeof(BitmapViewer),
               new PropertyMetadata(default(bool)));

        public bool ShowLoadingIcon
        {
            get { return (bool)GetValue(ShowLoadingIconProperty); }
            set { SetValue(ShowLoadingIconProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
           DependencyProperty.Register(
               "ViewModel",
               typeof(IBitmapViewerViewModel),
               typeof(BitmapViewer),
               new FrameworkPropertyMetadata(default(IBitmapViewerViewModel),
                   FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: OnViewModelChanged));

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public IBitmapViewerViewModel? ViewModel
        {
            get { return GetValue(ViewModelProperty) as IBitmapViewerViewModel; }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ToolBoxMarginProperty
            = DependencyProperty.Register("ToolBoxMargin", typeof(Thickness), typeof(BitmapViewer),
                                          new FrameworkPropertyMetadata(
                                                new Thickness(0, 0, 5, 0),
                                                FrameworkPropertyMetadataOptions.AffectsMeasure),
                                          new ValidateValueCallback(IsMarginValid));

        private static bool IsMarginValid(object value)
        {
            Thickness m = (Thickness)value;
            return m.IsValid(true, false, true, false);
        }

        public Thickness ToolBoxMargin
        {
            get { return (Thickness)GetValue(ToolBoxMarginProperty); }
            set { SetValue(ToolBoxMarginProperty, value); }
        }

        public static readonly DependencyProperty ToolBoxBackgroundProperty =
             DependencyProperty.Register(
                 "ToolBoxBackground",
                 typeof(Brush),
                 typeof(BitmapViewer),
                 new FrameworkPropertyMetadata(
                     DesignerProperties.GetIsInDesignMode(new DependencyObject())
                         ? Brushes.White
                         : Application.Current.FindResource(Definitions.PanelBackgroundLevel0_1) ?? Brushes.Transparent, // Giá trị khi runtime
                     FrameworkPropertyMetadataOptions.AffectsRender
                 )
             );
        public Brush ToolBoxBackground
        {
            get { return (Brush)GetValue(ToolBoxBackgroundProperty); }
            set { SetValue(ToolBoxBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ToolBoxBorderBrushProperty =
            DependencyProperty.Register(
                "ToolBoxBorderBrush",
                typeof(Brush),
                typeof(BitmapViewer),
                new FrameworkPropertyMetadata(
                    Application.Current.FindResource(Definitions.PanelBackgroundLevel0_4),
                    FrameworkPropertyMetadataOptions.AffectsRender
                )
            );

        public Brush ToolBoxBorderBrush
        {
            get { return (Brush)GetValue(ToolBoxBorderBrushProperty); }
            set { SetValue(ToolBoxBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty ToolBoxWidthProperty =
            DependencyProperty.Register(
                "ToolBoxWidth",
                typeof(double),
                typeof(BitmapViewer),
                new FrameworkPropertyMetadata(
                    40d,
                    FrameworkPropertyMetadataOptions.AffectsRender
                )
            );

        public double ToolBoxWidth
        {
            get { return (double)GetValue(ToolBoxWidthProperty); }
            set { SetValue(ToolBoxWidthProperty, value); }
        }

        public static readonly DependencyProperty ToolBoxIconSizeProperty =
            DependencyProperty.Register(
                "ToolBoxIconSize",
                typeof(double),
                typeof(BitmapViewer),
                new FrameworkPropertyMetadata(
                    26d,
                    FrameworkPropertyMetadataOptions.AffectsRender
                )
            );

        public double ToolBoxIconSize
        {
            get { return (double)GetValue(ToolBoxIconSizeProperty); }
            set { SetValue(ToolBoxIconSizeProperty, value); }
        }

        public static readonly DependencyProperty ToolBoxIconFillProperty =
            DependencyProperty.Register(
                "ToolBoxIconFill",
                typeof(Brush),
                typeof(BitmapViewer),
                new FrameworkPropertyMetadata(
                    Application.Current.FindResource(Definitions.PanelBackgroundLevel0_1),
                    FrameworkPropertyMetadataOptions.AffectsRender
                )
            );

        public Brush ToolBoxIconFill
        {
            get { return (Brush)GetValue(ToolBoxIconFillProperty); }
            set { SetValue(ToolBoxIconFillProperty, value); }
        }

        public static readonly DependencyProperty ToolBoxIconStrokeProperty =
            DependencyProperty.Register(
                "ToolBoxIconStroke",
                typeof(Brush),
                typeof(BitmapViewer),
                new FrameworkPropertyMetadata(
                    Application.Current.FindResource(Definitions.ButtonAndIconBrushLevel0),
                    FrameworkPropertyMetadataOptions.AffectsRender
                )
            );

        public Brush ToolBoxIconStroke
        {
            get { return (Brush)GetValue(ToolBoxIconStrokeProperty); }
            set { SetValue(ToolBoxIconStrokeProperty, value); }
        }

        // UseFitToScreenFunction
        public static readonly DependencyProperty UseFitToScreenFunctionProperty =
            DependencyProperty.Register("UseFitToScreenFunction", typeof(bool), typeof(BitmapViewer), new PropertyMetadata(true));

        public bool UseFitToScreenFunction
        {
            get { return (bool)GetValue(UseFitToScreenFunctionProperty); }
            set { SetValue(UseFitToScreenFunctionProperty, value); }
        }

        // UseTransparentBackgroundFunction
        public static readonly DependencyProperty UseTransparentBackgroundFunctionProperty =
            DependencyProperty.Register("UseTransparentBackgroundFunction", typeof(bool), typeof(BitmapViewer), new PropertyMetadata(true));

        public bool UseTransparentBackgroundFunction
        {
            get { return (bool)GetValue(UseTransparentBackgroundFunctionProperty); }
            set { SetValue(UseTransparentBackgroundFunctionProperty, value); }
        }

        // UseTransparenDecodedFrameBackgroundFunction
        public static readonly DependencyProperty UseTransparenDecodedFrameBackgroundFunctionProperty =
            DependencyProperty.Register("UseTransparenDecodedFrameBackgroundFunction", typeof(bool), typeof(BitmapViewer), new PropertyMetadata(true));

        public bool UseTransparenDecodedFrameBackgroundFunction
        {
            get { return (bool)GetValue(UseTransparenDecodedFrameBackgroundFunctionProperty); }
            set { SetValue(UseTransparenDecodedFrameBackgroundFunctionProperty, value); }
        }

        // UseLayoutBoundFunction
        public static readonly DependencyProperty UseLayoutBoundFunctionProperty =
            DependencyProperty.Register("UseLayoutBoundFunction", typeof(bool), typeof(BitmapViewer), new PropertyMetadata(true));

        public bool UseLayoutBoundFunction
        {
            get { return (bool)GetValue(UseLayoutBoundFunctionProperty); }
            set { SetValue(UseLayoutBoundFunctionProperty, value); }
        }

        // UseResetViewPortPositionFunction
        public static readonly DependencyProperty UseResetViewPortPositionFunctionProperty =
            DependencyProperty.Register("UseResetViewPortPositionFunction", typeof(bool), typeof(BitmapViewer), new PropertyMetadata(true));

        public bool UseResetViewPortPositionFunction
        {
            get { return (bool)GetValue(UseResetViewPortPositionFunctionProperty); }
            set { SetValue(UseResetViewPortPositionFunctionProperty, value); }
        }

        // UseZoomFunction
        public static readonly DependencyProperty UseZoomFunctionProperty =
            DependencyProperty.Register("UseZoomFunction", typeof(bool), typeof(BitmapViewer), new PropertyMetadata(true));

        public bool UseZoomFunction
        {
            get { return (bool)GetValue(UseZoomFunctionProperty); }
            set { SetValue(UseZoomFunctionProperty, value); }
        }

        private DraggableCanvasController draggableCanvasController;
        public BitmapViewer()
        {
            InitializeComponent();
            draggableCanvasController = new DraggableCanvasController(ContainerCanvas,
                DragableContainer,
                () => FitToScreenButton.IsChecked == false);
            Unloaded += OnUnloaded;
            Loaded += OnLoaded;
            BitmapViewerContainerInternal.GlobalBackgroundSource = BlackBagroundImage;
            BitmapViewerContainerInternal.FrameSourceChange += OnFrameSourceChange;
            if (TransparenDecodedFrameBackgroundButton.IsChecked == true)
            {
                DecodedFrameBackground.Fill = new SolidColorBrush(Colors.Transparent);
                DecodedFrameBackground2.Fill = new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                DecodedFrameBackground.Fill = new SolidColorBrush(Colors.White);
                DecodedFrameBackground2.Fill = new SolidColorBrush(Colors.White);
            }
        }

        private void OnWindowLocationChanged(object? sender, EventArgs e)
        {
            if (ZoomingPopup.IsOpen)
            {
                var offset = ZoomingPopup.HorizontalOffset;
                ZoomingPopup.HorizontalOffset = offset + 1;
                ZoomingPopup.HorizontalOffset = offset;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            draggableCanvasController.Setup();
            draggableCanvasController.Reset();

            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.LocationChanged -= OnWindowLocationChanged;
                window.LocationChanged += OnWindowLocationChanged;
            }
        }

        private void OnFrameSourceChange(ImageSource? oldSource, ImageSource? newSource)
        {
            draggableCanvasController.Reset();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            draggableCanvasController.Dispose();
        }

        private void FitToScreenButtonClick(object sender, RoutedEventArgs e)
        {
            if (FitToScreenButton.IsChecked == true)
            {
                StretchContainer.Visibility = Visibility.Visible;
                DragableContainer.Visibility = Visibility.Collapsed;
            }
            else
            {
                ZoomButton.IsChecked = false;
                StretchContainer.Visibility = Visibility.Collapsed;
                DragableContainer.Visibility = Visibility.Visible;
                LayoutBoundButton.IsChecked = false;
                StretchContainer.Margin = new Thickness(0);
                LayoutBoundRect.StrokeThickness = 0;
            }
        }

        private void TransparentBackgroundButtonClick(object sender, RoutedEventArgs e)
        {
            if (TransparentBackgroundButton.IsChecked == true)
            {
                BitmapViewerContainerInternal.GlobalBackgroundSource
                  = TransparentBagroundImage;
            }
            else
            {
                BitmapViewerContainerInternal.GlobalBackgroundSource
                    = BlackBagroundImage;
            }
        }

        private void ShowLayoutBoundButtonClick(object sender, RoutedEventArgs e)
        {
            if (LayoutBoundButton.IsChecked == true)
            {
                StretchContainer.Margin = new Thickness(2);
                LayoutBoundRect.StrokeThickness = 2;
            }
            else
            {
                StretchContainer.Margin = new Thickness(0);
                LayoutBoundRect.StrokeThickness = 0;
            }
        }

        private void TransparenDecodedFrameBackgroundButtonClick(object sender, RoutedEventArgs e)
        {
            if (TransparenDecodedFrameBackgroundButton.IsChecked == true)
            {
                DecodedFrameBackground.Fill = new SolidColorBrush(Colors.Transparent);
                DecodedFrameBackground2.Fill = new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                DecodedFrameBackground.Fill = new SolidColorBrush(Colors.White);
                DecodedFrameBackground2.Fill = new SolidColorBrush(Colors.White);
            }
        }

        private void ResetViewPortPositionButtonClick(object sender, RoutedEventArgs e)
        {
            draggableCanvasController.Reset();
            BitmapViewerContainerInternal.ViewBoxZoomDelta = 1;
        }
    }

    public class BitmapViewerInternal : UserControl
    {
        public FrameSourceChangeHandler? FrameSourceChange;
        public static readonly DependencyProperty GlobalOffXProperty =
           DependencyProperty.Register(
                "GlobalOffX",
                typeof(int),
                typeof(BitmapViewerInternal),
                new PropertyMetadata(0, OnSizePropertyChanged));

        public uint GlobalOffX
        {
            get { return Convert.ToUInt16(GetValue(GlobalOffXProperty)); }
            set { SetValue(GlobalOffXProperty, value); }
        }

        public static readonly DependencyProperty GlobalOffYProperty =
            DependencyProperty.Register(
                "GlobalOffY",
                typeof(int),
                typeof(BitmapViewerInternal),
                new PropertyMetadata(0, OnSizePropertyChanged));

        public uint GlobalOffY
        {
            get { return Convert.ToUInt16(GetValue(GlobalOffYProperty)); }
            set { SetValue(GlobalOffYProperty, value); }
        }

        public static readonly DependencyProperty GlobalWidthProperty =
            DependencyProperty.Register(
                "GlobalWidth",
                typeof(uint),
                typeof(BitmapViewerInternal),
                new PropertyMetadata(0u, OnSizePropertyChanged));

        public uint GlobalWidth
        {
            get { return Convert.ToUInt16(GetValue(GlobalWidthProperty)); }
            set { SetValue(GlobalWidthProperty, value); }
        }

        public static readonly DependencyProperty GlobalHeightProperty =
            DependencyProperty.Register(
                "GlobalHeight",
                typeof(uint),
                typeof(BitmapViewerInternal),
                new PropertyMetadata(0u, OnSizePropertyChanged));

        public uint GlobalHeight
        {
            get { return Convert.ToUInt16(GetValue(GlobalHeightProperty)); }
            set { SetValue(GlobalHeightProperty, value); }
        }

        public static readonly DependencyProperty FrameHeightProperty =
            DependencyProperty.Register(
                "FrameHeight",
                typeof(uint),
                typeof(BitmapViewerInternal),
                new PropertyMetadata(0u, OnSizePropertyChanged));

        public uint FrameHeight
        {
            get { return Convert.ToUInt16(GetValue(FrameHeightProperty)); }
            set { SetValue(FrameHeightProperty, value); }
        }

        public static readonly DependencyProperty FrameWidthProperty =
            DependencyProperty.Register(
                "FrameWidth",
                typeof(uint),
                typeof(BitmapViewerInternal),
                new PropertyMetadata(0u, OnSizePropertyChanged));

        public uint FrameWidth
        {
            get { return Convert.ToUInt16(GetValue(FrameWidthProperty)); }
            set { SetValue(FrameWidthProperty, value); }
        }

        public static readonly DependencyProperty FrameOffXProperty =
            DependencyProperty.Register(
                "FrameOffX",
                typeof(int),
                typeof(BitmapViewerInternal),
                new PropertyMetadata(0, OnSizePropertyChanged));

        public uint FrameOffX
        {
            get { return Convert.ToUInt16(GetValue(FrameOffXProperty)); }
            set { SetValue(FrameOffXProperty, value); }
        }

        public static readonly DependencyProperty FrameOffYProperty =
            DependencyProperty.Register(
                "FrameOffY",
                typeof(int),
                typeof(BitmapViewerInternal),
                new PropertyMetadata(0, OnSizePropertyChanged));

        public uint FrameOffY
        {
            get { return Convert.ToUInt16(GetValue(FrameOffYProperty)); }
            set { SetValue(FrameOffYProperty, value); }
        }

        private static void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public static readonly DependencyProperty FrameSourceProperty =
                DependencyProperty.Register(
                        "FrameSource",
                        typeof(ImageSource),
                        typeof(BitmapViewerInternal),
                        new FrameworkPropertyMetadata(
                                null,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnImageSourceChanged),
                                null),
                        null);

        public ImageSource FrameSource
        {
            get { return (ImageSource)GetValue(FrameSourceProperty); }
            set { SetValue(FrameSourceProperty, value); }
        }

        public static readonly DependencyProperty GlobalBackgroundSourceProperty =
                DependencyProperty.Register(
                        "GlobalBackgroundSource",
                        typeof(ImageSource),
                        typeof(BitmapViewerInternal),
                        new FrameworkPropertyMetadata(
                                new BitmapImage(new Uri(@"/Resources/spr_global_background.png", UriKind.Relative)),
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                null),
                        null);

        public ImageSource GlobalBackgroundSource
        {
            get { return (ImageSource)GetValue(GlobalBackgroundSourceProperty); }
            set { SetValue(GlobalBackgroundSourceProperty, value); }
        }

        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.IfIs<BitmapViewerInternal>(it =>
                it.FrameSourceChange?.Invoke(e.OldValue as ImageSource,
                    e.NewValue as ImageSource));
        }

        public static readonly DependencyProperty ViewBoxZoomDeltaProperty =
            DependencyProperty.Register(
                "ViewBoxZoomDelta",
                typeof(double),
                typeof(BitmapViewerInternal),
                new PropertyMetadata(1d));

        public double ViewBoxZoomDelta
        {
            get { return Convert.ToDouble(GetValue(ViewBoxZoomDeltaProperty)); }
            set { SetValue(ViewBoxZoomDeltaProperty, value); }
        }

        public delegate void FrameSourceChangeHandler(ImageSource? oldSource, ImageSource? newSource);
    }

    public class DraggableCanvasController : IDisposable
    {
        private Canvas _viewPortCanvas;
        private Canvas _draggableCanvas;
        private Func<bool>? _canContentBeDragged;
        private bool _isDragging = false;
        private Point _offset;

        public DraggableCanvasController(Canvas viewPortCanvas,
            Canvas draggableCanvas,
            Func<bool>? canContentBeDragged = null)
        {
            _canContentBeDragged = canContentBeDragged;
            _viewPortCanvas = viewPortCanvas;
            _draggableCanvas = draggableCanvas;
            Setup();
        }

        public void Setup()
        {
            _draggableCanvas.MouseLeftButtonDown -= ViewPortCanvasMouseLeftButtonDown;
            _draggableCanvas.MouseMove -= ViewPortCanvasMouseMove;
            _draggableCanvas.MouseLeftButtonUp -= ViewPortCanvasMouseLeftButtonUp;
            _draggableCanvas.MouseLeave -= ViewPortCanvasMouseLeave;
            _viewPortCanvas.MouseLeave -= ViewPortCanvasMouseLeave;

            _draggableCanvas.MouseLeftButtonDown += ViewPortCanvasMouseLeftButtonDown;
            _draggableCanvas.MouseMove += ViewPortCanvasMouseMove;
            _draggableCanvas.MouseLeftButtonUp += ViewPortCanvasMouseLeftButtonUp;
            _draggableCanvas.MouseLeave += ViewPortCanvasMouseLeave;
            _viewPortCanvas.MouseLeave += ViewPortCanvasMouseLeave;
        }

        public void Dispose()
        {
            _draggableCanvas.MouseLeftButtonDown -= ViewPortCanvasMouseLeftButtonDown;
            _draggableCanvas.MouseMove -= ViewPortCanvasMouseMove;
            _draggableCanvas.MouseLeftButtonUp -= ViewPortCanvasMouseLeftButtonUp;
            _draggableCanvas.MouseLeave -= ViewPortCanvasMouseLeave;
            _viewPortCanvas.MouseLeave -= ViewPortCanvasMouseLeave;
        }

        public void Reset()
        {
            _isDragging = false;
            _viewPortCanvas.Cursor = Cursors.Arrow;
            Canvas.SetLeft(_draggableCanvas, 0);
            Canvas.SetTop(_draggableCanvas, 0);
        }

        private void ViewPortCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _offset = e.GetPosition(_draggableCanvas);
            _viewPortCanvas.Cursor = Cursors.SizeAll;
        }

        private void ViewPortCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _viewPortCanvas.Cursor = Cursors.Arrow;
        }

        private void ViewPortCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && (_canContentBeDragged?.Invoke() ?? true))
            {
                Point mousePos = e.GetPosition(_viewPortCanvas);
                double newX = mousePos.X - _offset.X;
                double newY = mousePos.Y - _offset.Y;
                double globOffset = 10;
                // Giới hạn vị trí mới để Canvas B không rời khỏi biên của Canvas A
                var boundLeft = _viewPortCanvas.ActualWidth - _draggableCanvas.ActualWidth - globOffset;
                var boundBot = _viewPortCanvas.ActualHeight - _draggableCanvas.ActualHeight - globOffset;
                var boundRight = 0 + globOffset;
                var boundTop = 0 + globOffset;
                if (_viewPortCanvas.ActualWidth < _draggableCanvas.ActualWidth)
                {
                    newX = newX < boundLeft ? boundLeft : newX > boundRight ? boundRight : newX;
                }
                else
                {
                    newX = newX < 0 ? 0 : newX > globOffset ? globOffset : newX;
                }

                if (_viewPortCanvas.ActualHeight < _draggableCanvas.ActualHeight)
                {
                    newY = newY < boundBot ? boundBot : newY > boundTop ? boundTop : newY;
                }
                else
                {
                    newY = newY < 0 ? 0 : newY > globOffset ? globOffset : newY;
                }

                Canvas.SetLeft(_draggableCanvas, newX);
                Canvas.SetTop(_draggableCanvas, newY);
            }
        }

        private void ViewPortCanvasMouseLeave(object sender, MouseEventArgs e)
        {
            _isDragging = false;
            _viewPortCanvas.Cursor = Cursors.Arrow;
        }

    }
}
