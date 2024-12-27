using ArtWiz.ViewModel.Widgets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ArtWiz.View.Widgets
{
    /// <summary>
    /// Interaction logic for FramePreviewer.xaml
    /// </summary>
    public partial class FramePreviewer : UserControl
    {
        public static readonly DependencyProperty IsIntersectedProperty =
           DependencyProperty.Register(
               "IsIntersected",
               typeof(bool),
               typeof(FramePreviewer),
               new PropertyMetadata(false));


        public bool IsIntersected
        {
            get { return (bool)GetValue(IsIntersectedProperty); }
            private set { SetValue(IsIntersectedProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
           DependencyProperty.Register(
               "ViewModel",
               typeof(IFramePreviewerViewModel),
               typeof(FramePreviewer),
               new FrameworkPropertyMetadata(default(IFramePreviewerViewModel),
                   FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: OnViewModelChanged));

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public IFramePreviewerViewModel? ViewModel
        {
            get { return GetValue(ViewModelProperty) as IFramePreviewerViewModel; }
            set { SetValue(ViewModelProperty, value); }
        }

        public FrameLineEditorVirtualizingPanel? PanelOwner { get; set; }

        public FramePreviewer()
        {
            InitializeComponent();
        }


        private bool isMouseHolded = false;
        private bool isMousePressed = false;
        private FramePreviewer? tempFrameForDragging = null;

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isMousePressed = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isMousePressed && PanelOwner != null && tempFrameForDragging == null)
            {
                isMouseHolded = true;
                CreateTempEllipseWhenMouseDown(PanelOwner, e);
                isMousePressed = false;
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var shouldInvokeClickEvent = isMousePressed;
            isMousePressed = false;
            if (shouldInvokeClickEvent)
            {
                PanelOwner?.RaiseFrameClickedEvent(this, e);
            }
        }

        private void CreateTempEllipseWhenMouseDown(FrameLineEditorVirtualizingPanel referCanvasContainer, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(referCanvasContainer);
            var currentViewCacheIndex = referCanvasContainer.GetCurrentViewCacheIndex(this);
            FramePreviewer? intersectedFrame = null;
            var tempFrame = new FramePreviewer()
            {
                Background = Background,
                Height = ActualHeight,
                Width = ActualWidth,
                BorderBrush = BorderBrush,
                BorderThickness = BorderThickness,
                ViewModel = ViewModel,
                Opacity = 0.4
            };

            tempFrame.MouseLeftButtonUp += MouseLeftButtonUp;
            tempFrame.MouseMove += MouseMove;

            void MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            {
                if (intersectedFrame != null && currentViewCacheIndex != -1)
                {
                    //Logger.Raw.D($"Up on: {dragEnteredEllipse.initIndex}");
                    var intersectedFrameViewCacheIndex = referCanvasContainer.GetCurrentViewCacheIndex(intersectedFrame);
                    referCanvasContainer.RaisePreviewFrameSwitchEvent(currentViewCacheIndex, intersectedFrameViewCacheIndex);
                    //Definitions.Instance?[Definitions.ButtonAndIconColorLevel0].IfIs<Color>(it =>
                    //{
                    //    dragEnteredEllipse.BeginAnimation(it, 0);
                    //}, @else: () =>
                    //{
                    //    dragEnteredEllipse.BeginAnimation(Colors.Black, 0);
                    //});
                    intersectedFrame.IsIntersected = false;
                }

                tempFrame.Cursor = Cursors.Arrow;
                isMouseHolded = false;
                tempFrame.ReleaseMouseCapture();
                referCanvasContainer.Children.Remove(tempFrame);
                //ContainerCanvas_MouseLeave(referEllipse, e);
                //DraggedMouseEllipse = null;
                tempFrame.MouseLeftButtonUp -= MouseLeftButtonUp;
                tempFrame.MouseMove -= MouseMove;
                tempFrameForDragging = null;
                referCanvasContainer.EnableAutoHorizontalScroll(false, false);
                intersectedFrame = null;
            }

            void MouseMove(object sender, MouseEventArgs e)
            {
                if (isMouseHolded)
                {
                    Point newPosition = e.GetPosition(referCanvasContainer);
                    double deltaX = newPosition.X - mousePosition.X;
                    double deltaY = newPosition.Y - mousePosition.Y;

                    double newLeft = Canvas.GetLeft(tempFrame) + deltaX;
                    double newTop = Canvas.GetTop(tempFrame) + deltaY;

                    Canvas.SetLeft(tempFrame, newLeft);
                    Canvas.SetTop(tempFrame, newTop);

                    mousePosition = newPosition;
                    var newIntersectedFrame = referCanvasContainer.GetItemContainerBaseOnRelativePositionToPanel(
                         relativePanelPos: new Rect(newLeft, newTop, ActualWidth, ActualHeight),
                         excludedViewIndex: currentViewCacheIndex);
                    if (newIntersectedFrame != intersectedFrame)
                    {
                        if (intersectedFrame != null)
                        {
                            intersectedFrame.IsIntersected = false;
                        }
                        intersectedFrame = newIntersectedFrame;
                        if (intersectedFrame != null)
                        {
                            intersectedFrame.IsIntersected = true;
                        }
                    }

                    var autoScrollOption = referCanvasContainer.IsRectInAutoScrollArea(
                        new Rect(newPosition.X, newPosition.Y, 10, 10));
                    if (autoScrollOption == ScrollTypes.RIGHT)
                    {
                        referCanvasContainer.EnableAutoHorizontalScroll(true, false);
                    }
                    else if (autoScrollOption == ScrollTypes.LEFT)
                    {
                        referCanvasContainer.EnableAutoHorizontalScroll(true, true);
                    }
                    else
                    {
                        referCanvasContainer.EnableAutoHorizontalScroll(false, false);
                    }
                    //OnDraggingMouseEllipse?.Invoke(this, newLeft, newTop, e);
                }
            }

            referCanvasContainer.GetItemContainerPositionRelativeToPanel(this, out double left, out double top);
            referCanvasContainer.Children.Add(tempFrame);
            Canvas.SetLeft(tempFrame, left);
            Canvas.SetTop(tempFrame, top);
            tempFrame.CaptureMouse();
            tempFrame.Cursor = Cursors.Hand;
            tempFrameForDragging = tempFrame;
        }

        private void RemoveMenuItemClicked(object sender, RoutedEventArgs e)
        {
            var currentViewCacheIndex = PanelOwner?.GetCurrentViewCacheIndex(this) ?? 0;
            PanelOwner?.RaisePreviewFrameRemoveEvent(currentViewCacheIndex);
        }

        private void InsertMenuItemClicked(object sender, RoutedEventArgs e)
        {
            var currentViewCacheIndex = PanelOwner?.GetCurrentViewCacheIndex(this) ?? 0;
            PanelOwner?.RaisePreviewFrameInsertEvent(currentViewCacheIndex);
        }
    }
}
