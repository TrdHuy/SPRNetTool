using ArtWiz.Utils;
using ArtWiz.ViewModel.Widgets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;

namespace ArtWiz.View.Widgets
{
    public enum ScrollTypes
    {
        NONE, RIGHT, LEFT
    }

    // Must set CanContentScroll = true of ScrollViewer owner
    // to overide IScrollInfo
    public class FrameLineEditorVirtualizingPanel : Canvas, IScrollInfo
    {
        public delegate void FameLineHandler(object sender, FrameLineEventArgs args);
        public delegate void FameLineMouseEventHandler(object sender, FameLineMouseEventArgs args);

        public event FameLineHandler? OnPreviewFrameIndexSwitched;
        public event FameLineHandler? OnPreviewFrameIndexRemoved;
        public event FameLineHandler? OnPreviewFrameIndexInserted;
        public event FameLineMouseEventHandler? OnFramePreviewerMouseClick;

        public static readonly DependencyProperty IsSpeedBoostProperty =
            DependencyProperty.Register(
                "IsSpeedBoost",
                typeof(bool),
                typeof(FrameLineEditorVirtualizingPanel),
            new FrameworkPropertyMetadata(defaultValue: default(bool),
                flags: FrameworkPropertyMetadataOptions.AffectsRender));


        public bool IsSpeedBoost
        {
            get { return (bool)GetValue(IsSpeedBoostProperty); }
            private set { SetValue(IsSpeedBoostProperty, value); }
        }

        public static readonly DependencyProperty AutoScrollOptProperty =
            DependencyProperty.Register(
                "AutoScrollOpt",
                typeof(ScrollTypes),
                typeof(FrameLineEditorVirtualizingPanel),
            new FrameworkPropertyMetadata(defaultValue: default(ScrollTypes),
                flags: FrameworkPropertyMetadataOptions.AffectsRender));


        public ScrollTypes AutoScrollOpt
        {
            get { return (ScrollTypes)GetValue(AutoScrollOptProperty); }
            private set { SetValue(AutoScrollOptProperty, value); }
        }
        #region ScrollInfo
        private ScrollViewer? _scrollOwner;
        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }
        public double ExtentHeight { get; set; }
        public double ExtentWidth { get; set; }

        public double HorizontalOffset { get; private set; }
        public ScrollViewer? ScrollOwner
        {
            get
            {
                return _scrollOwner;
            }
            set
            {
                _scrollOwner = value;
                ScrollBar? findHorizontalScrollBar(UIElement? ui)
                {
                    if (ui is Decorator cast)
                    {
                        return findHorizontalScrollBar(cast.Child);
                    }
                    else if (ui is Panel cast2)
                    {
                        foreach (UIElement child in cast2.Children)
                        {
                            var cache = findHorizontalScrollBar((UIElement)child);
                            if (cache != null)
                            {
                                return cache;
                            }
                        }
                    }
                    else if (ui is ContentControl cast3)
                    {
                        var child = cast3.Content as UIElement;
                        if (child == null)
                            return null;
                        return findHorizontalScrollBar(child);
                    }

                    if (ui is ScrollBar cast4 && cast4.Orientation == Orientation.Horizontal)
                    {
                        return cast4;
                    }
                    return null;
                }
                //var uiElement = _scrollOwner?.Template.LoadContent() as UIElement;
                //_horizontalScrollBar = findHorizontalScrollBar(uiElement);
            }
        }
        public double VerticalOffset { get; private set; }
        public double ViewportHeight { get; private set; }
        public double ViewportWidth { get; private set; }

        public void LineDown()
        {
            throw new NotImplementedException();
        }

        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - 20);
        }

        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + 20);
        }

        public void LineUp()
        {
            throw new NotImplementedException();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            throw new NotImplementedException();
        }

        public void MouseWheelDown()
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                SetFrameDistance(-5);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                SetHorizontalOffset(HorizontalOffset - 50);
            }
            else
            {
                SetHorizontalOffset(HorizontalOffset - 5);
            }
        }

        public void MouseWheelLeft()
        {
            throw new NotImplementedException();
        }

        public void MouseWheelRight()
        {
            throw new NotImplementedException();
        }

        public void MouseWheelUp()
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                SetFrameDistance(5);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                SetHorizontalOffset(HorizontalOffset + 50);
            }
            else
            {
                SetHorizontalOffset(HorizontalOffset + 5);
            }
        }

        public void PageDown()
        {
            throw new NotImplementedException();
        }

        public void PageLeft()
        {
            var mP = Mouse.GetPosition(this);
            var ratX = mP.X / this.ActualWidth;
            var newOffset = ratX * (ExtentWidth - ViewportWidth);
            SetHorizontalOffset(newOffset);
        }

        public void PageRight()
        {
            var mP = Mouse.GetPosition(this);
            var ratX = mP.X / this.ActualWidth;
            var newOffset = ratX * (ExtentWidth - ViewportWidth);
            SetHorizontalOffset(newOffset);
        }

        public void PageUp()
        {
            throw new NotImplementedException();
        }

        public void SetHorizontalOffset(double offset)
        {
            if (offset < 0 || ViewportWidth >= ExtentWidth)
            {
                offset = 0;
            }
            else
            {
                if (offset + ViewportWidth >= ExtentWidth)
                {
                    offset = ExtentWidth - ViewportWidth;
                }
            }

            HorizontalOffset = offset;

            ScrollOwner?.InvalidateScrollInfo();

            InvalidateMeasure();
        }

        public void SetVerticalOffset(double offset)
        {
        }
        #endregion


        private int CACHE_SIZE { get; } = 2;
        private double autoScrollAreaWidth { get; set; } = 50;
        private double speedUpScrollAreaWidth { get; set; } = 50;
        private double frameDistance { get; set; } = 30;
        private Rect autoScrollRightRect { get; set; }
        private Rect speedUpScrollRightRect { get; set; }
        private Rect autoScrollLeftRect { get; set; }
        private Rect speedUpScrollLeftRect { get; set; }
        private bool IsItemContainerAnimationStarted { get; set; }

        private Collection<IFramePreviewerViewModel>? itemSourceCache;
        private SemaphoreSlim autoScrollSemaphore = new SemaphoreSlim(1, 1);
        private Canvas? contentContainerCanvas;
        private FrameItemController itemController;
        private FramePanelMenuContextController menuContextController;
        private FramePreviewer? cacheItemContainerForMeasure;
        private Size desiredItemContainerSize;

        public FrameLineEditorVirtualizingPanel()
        {
            itemController = new FrameItemController(this);
            menuContextController = new FramePanelMenuContextController(this);
        }

        #region public API
        public Size PanelConstraint { get; private set; }

        public int GetCurrentViewCacheIndex(FramePreviewer itemContainer)
        {
            return itemController[itemContainer]?.Index ?? -1;
        }

        public void RaiseFrameClickedEvent(FramePreviewer clickedFrame, MouseButtonEventArgs e)
        {
            var fameLineMouseEventHandler = new FameLineMouseEventArgs(GetCurrentViewCacheIndex(clickedFrame),
                e.MouseDevice, e.Timestamp, e.ChangedButton);
            itemController[clickedFrame]?.Apply(it => OnFramePreviewerMouseClick?.Invoke(it, fameLineMouseEventHandler));
        }

        public void RaisePreviewFrameInsertEvent(int newIndex)
        {
            var arg = FrameLineEventArgs.CreateAddingNewFrameEvent(newIndex);
            OnPreviewFrameIndexInserted?.Invoke(this, arg);

            if (arg.Handled)
            {
                return;
            }

            // TODO: may implement insert logic if not using observable collection
        }

        public void RaisePreviewFrameRemoveEvent(int oldIndex)
        {
            var arg = FrameLineEventArgs.CreateRemovingOldFrameEvent(oldIndex);
            OnPreviewFrameIndexRemoved?.Invoke(this, arg);

            if (arg.Handled)
            {
                return;
            }

            // TODO: may implement remove logic if not using observable collection
        }


        public void RaisePreviewFrameSwitchEvent(int oldIndex, int newIndex)
        {
            var arg = FrameLineEventArgs.CreateSwitchEvent(oldIndex, newIndex);
            OnPreviewFrameIndexSwitched?.Invoke(this, arg);

            if (arg.Handled)
            {
                return;
            }

            // TODO: may implement switch logic if not using observable collection
        }

        public void SetUpSource(Collection<IFramePreviewerViewModel>? itemSource)
        {
            itemSource?.IfIs<INotifyCollectionChanged>(it =>
            {
                it.CollectionChanged += OnItemCollectionChanged;
            });
            itemSourceCache?.IfIs<INotifyCollectionChanged>(it =>
            {
                it.CollectionChanged -= OnItemCollectionChanged;
            });
            itemSourceCache = itemSource;
            cacheItemContainerForMeasure = null;
            desiredItemContainerSize = Size.Empty;
            contentContainerCanvas?.Children.Clear();
            itemController.SetupItemSource(itemSource);
            InvalidateMeasure();
        }

        public async void EnableAutoHorizontalScroll(bool isEnable, bool isLeft)
        {
            if (!isEnable && autoScrollSemaphore.CurrentCount == 1)
            {
                return;
            }
            if (autoScrollSemaphore.CurrentCount == 0)
            {
                if (!isEnable)
                {
                    AutoScrollOpt = ScrollTypes.NONE;
                    autoScrollSemaphore.Release();
                }
                return;
            }
            await autoScrollSemaphore.WaitAsync();

            var defaultOffset = 6;
            var targetOffset = defaultOffset;
            if (isLeft)
            {
                AutoScrollOpt = ScrollTypes.LEFT;

                while (HorizontalOffset > 0
                   && autoScrollSemaphore.CurrentCount == 0)
                {
                    var mp = Mouse.GetPosition(this);
                    if (speedUpScrollLeftRect.Contains(mp))
                    {
                        targetOffset += 1;
                        IsSpeedBoost = true;
                    }
                    else
                    {
                        IsSpeedBoost = false;
                        targetOffset = defaultOffset;
                    }
                    SetHorizontalOffset(HorizontalOffset - targetOffset);
                    await Task.Delay(10);
                }
            }
            else
            {
                AutoScrollOpt = ScrollTypes.RIGHT;

                while (HorizontalOffset < ExtentWidth
                    && autoScrollSemaphore.CurrentCount == 0)
                {
                    var mp = Mouse.GetPosition(this);
                    if (speedUpScrollRightRect.Contains(mp))
                    {
                        targetOffset += 1;
                        IsSpeedBoost = true;
                    }
                    else
                    {
                        IsSpeedBoost = false;
                        targetOffset = defaultOffset;
                    }
                    SetHorizontalOffset(HorizontalOffset + targetOffset);
                    await Task.Delay(10);
                }

            }
            if (autoScrollSemaphore.CurrentCount == 0)
            {
                AutoScrollOpt = ScrollTypes.NONE;
                autoScrollSemaphore.Release();
            }
        }

        public void GetItemContainerPositionRelativeToPanel(FramePreviewer itemContainer, out double left, out double top)
        {
            left = 0; top = 0;
            var cacheItem = itemController[itemContainer];
            if (cacheItem == null) return;
            left = cacheItem.MainPanelPosition.Left;
            top = cacheItem.MainPanelPosition.Top;
        }

        public FramePreviewer? GetItemContainerBaseOnRelativePositionToPanel(Rect relativePanelPos, int excludedViewIndex)
        {
            return itemController.GetItemContainerBaseOnRelativePositionToPanel(relativePanelPos, excludedViewIndex)?.View;
        }

        public int GetItemContainerIndexBaseOnRelativePositionToPanel(double relativePanelPosX)
        {
            return itemController.GetItemContainerIndexBaseOnRelativePositionToPanel(relativePanelPosX, frameDistance);
        }

        public ScrollTypes IsRectInAutoScrollArea(Rect rect)
        {
            if (rect.IntersectsWith(autoScrollRightRect))
                return ScrollTypes.RIGHT;
            if (rect.IntersectsWith(autoScrollLeftRect))
                return ScrollTypes.LEFT;
            return ScrollTypes.NONE;
        }

        public void SetFrameDistance(double offset)
        {
            frameDistance += offset;
            if (frameDistance < 0)
            {
                frameDistance = 0;
            }

            // Calculate new Horizontal offset to remain display effect
            if (cacheItemContainerForMeasure != null
                && itemSourceCache != null
                && contentContainerCanvas != null
                && itemSourceCache.Count > 0
                && HorizontalOffset != 0)
            {
                var frameWidth = desiredItemContainerSize.Width;
                var slotSize = frameWidth + frameDistance;
                HorizontalOffset = (itemController.RealInitialVisibleItemIndex + CACHE_SIZE / 2) * slotSize;
            }
            InvalidateMeasure();
        }
        #endregion
        private void OnItemCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            //TODO: Implement collection changed
            switch (e)
            {
                case CustomNotifyCollectionChangedEventArgs cast:
                    if (cast.IsSwitchAction)
                    {
                        SwitchFrame(oldIndex: cast.Switched1stIndex,
                            newIndex: cast.Switched2ndIndex,
                            oldItem: itemSourceCache![cast.Switched2ndIndex],
                            newItem: itemSourceCache![cast.Switched1stIndex]);
                    }
                    return;
                case NotifyCollectionChangedEventArgs:
                    if (e.Action == NotifyCollectionChangedAction.Remove)
                    {
                        var sizeChanged = e.OldItems?.Count ?? 0;
                        for (int i = e.OldStartingIndex; i < e.OldStartingIndex + sizeChanged; i++)
                        {
                            RemoveFrame(i);
                        }
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        var sizeChanged = e.NewItems?.Count ?? 0;
                        for (int i = e.NewStartingIndex; i < e.NewStartingIndex + sizeChanged; i++)
                        {
                            if (e.NewItems != null)
                            {
                                var itemIndex = i - e.NewStartingIndex;
                                var vm = (IFramePreviewerViewModel)e.NewItems[itemIndex]!;
                                InsertFrame(i, vm);
                            }
                        }
                    }
                    return;
            }
        }


        protected override Size MeasureOverride(Size constraint)
        {
            var isShouldMeasureItemContainer = PanelConstraint != constraint;
            PanelConstraint = constraint;

            if (IsItemContainerAnimationStarted)
            {
                return base.MeasureOverride(constraint);
            }

            if (contentContainerCanvas != null
                && itemSourceCache != null
                && itemSourceCache.Count > 0)
            {
                // Each item container should have same width
                if (cacheItemContainerForMeasure == null)
                {
                    cacheItemContainerForMeasure = new FramePreviewer();
                    cacheItemContainerForMeasure.ViewModel = itemSourceCache[0];
                    isShouldMeasureItemContainer = true;
                }

                if (isShouldMeasureItemContainer)
                {
                    cacheItemContainerForMeasure.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    var desiredSize = cacheItemContainerForMeasure.DesiredSize;
                    var actualHeight = desiredSize.Height;
                    var actualWidth = desiredSize.Width;
                    if (actualHeight > PanelConstraint.Height / 2)
                    {
                        actualHeight = PanelConstraint.Height / 2;
                        var rat = desiredSize.Width / desiredSize.Height;
                        actualWidth = rat * actualHeight;
                    }
                    SetDesiredItemContainerSize(new Size(actualWidth, actualHeight));
                }

                ComputeExtentAndViewport(constraint, desiredItemContainerSize.Width);
                var slotSzie = desiredItemContainerSize.Width + frameDistance;

                // Case 3: Fit to the screen
                // |      |
                // |[][][]|
                // |      |
                //
                // [] : this is represented as an item
                // | : screen edge

                // Case 1:
                // |     |
                // |[ ][ |]
                // |     |
                //
                // [] : this is represented as an item
                // | : screen edge
                var realVisibleItemCount1 = Math.Ceiling(constraint.Width / slotSzie);

                // Case 2:
                // |     |
                //[|][][ |]
                // |     |
                //
                // [] : this is represented as an item
                // | : screen edge
                var realVisibleItemCount2 = constraint.Width % slotSzie > 1 ? realVisibleItemCount1 + 1 : realVisibleItemCount1;

                var newVisibleItemCount = (int)realVisibleItemCount2 + CACHE_SIZE;


                var newStartVisibleItemIndex = (int)(Math.Floor(HorizontalOffset / slotSzie) - CACHE_SIZE / 2);
                newStartVisibleItemIndex = newStartVisibleItemIndex >= 0 ? newStartVisibleItemIndex : 0;
                var newEndVisibleItemIndex = newStartVisibleItemIndex + newVisibleItemCount - 1;

                itemController.SetUpVisibleSection(newStartVisibleItemIndex, newEndVisibleItemIndex,
                    out List<FrameItemController.ViewCache> addedCaches,
                    out List<FrameItemController.ViewCache> removedCaches);
                foreach (var cache in addedCaches)
                {
                    contentContainerCanvas.Children.Add(cache.View);
                }
                foreach (var cache in removedCaches)
                {
                    contentContainerCanvas.Children.Remove(cache.View);
                }

                Debug.WriteLine("ExtentWidth=" + ExtentWidth);
                Debug.WriteLine("HorizontalOffset=" + HorizontalOffset);
                Debug.WriteLine("constraintWidth=" + constraint.Width);
                Debug.WriteLine("realVisibleItemCount=" + realVisibleItemCount2);
                Debug.WriteLine("newVisibleItemCount=" + newVisibleItemCount);
                Debug.WriteLine("newStartVisibleItemIndex=" + newStartVisibleItemIndex);
                Debug.WriteLine("newEndVisibleItemIndex=" + newEndVisibleItemIndex);
                Debug.WriteLine("==================");
            }
            else
            {
                SetDesiredItemContainerSize(new Size(0, 0));
            }
            return base.MeasureOverride(constraint);
        }

        public new void BeginStoryboard(Storyboard storyboard)
        {
            storyboard.Completed += (s, e) =>
            {
                IsItemContainerAnimationStarted = false;
                InvalidateMeasure();
            };
            IsItemContainerAnimationStarted = true;
            base.BeginStoryboard(storyboard);

        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (IsItemContainerAnimationStarted)
            {
                return base.ArrangeOverride(arrangeSize);
            }
            Debug.Assert(itemController.VisibleItemCount == contentContainerCanvas!.Children.Count);
            // Đảm bảo map và cache size luôn bằng nhau

            SetLeft(contentContainerCanvas, -HorizontalOffset);
            SetTop(contentContainerCanvas, (arrangeSize.Height - (desiredItemContainerSize.Height)) / 2);
            autoScrollRightRect = new Rect(arrangeSize.Width - autoScrollAreaWidth,
                0,
                autoScrollAreaWidth,
                arrangeSize.Height);
            autoScrollLeftRect = new Rect(0,
                0,
                autoScrollAreaWidth,
                arrangeSize.Height);

            speedUpScrollLeftRect = new Rect(0,
                arrangeSize.Height / 2 - speedUpScrollAreaWidth / 2,
                speedUpScrollAreaWidth,
                speedUpScrollAreaWidth);
            speedUpScrollRightRect = new Rect(arrangeSize.Width - speedUpScrollAreaWidth,
                arrangeSize.Height / 2 - speedUpScrollAreaWidth / 2,
                speedUpScrollAreaWidth,
                speedUpScrollAreaWidth);

            itemController.ArrangeViewCache(arrangeSize,
                desiredItemContainerSize, frameDistance, HorizontalOffset, contentContainerCanvas.Margin.Left);

            return base.ArrangeOverride(arrangeSize);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            contentContainerCanvas = new Canvas();
            contentContainerCanvas.Margin = new Thickness(15, 0, 15, 0);
            Children.Add(contentContainerCanvas);
        }

        protected void ComputeExtentAndViewport(Size pixelMeasuredViewportSize, double frameWidth)
        {
            ViewportHeight = pixelMeasuredViewportSize.Height;
            ExtentHeight = 0;
            var slotSize = frameWidth + frameDistance;
            if (contentContainerCanvas != null && itemSourceCache != null)
            {
                var actualWidth = itemSourceCache.Count * slotSize - frameDistance
                    + contentContainerCanvas.Margin.Left + contentContainerCanvas.Margin.Right;
                ViewportWidth = pixelMeasuredViewportSize.Width;
                ExtentWidth = actualWidth;
                ExtentWidth = ExtentWidth > 0 ? ExtentWidth : 0;
                if (HorizontalOffset + ViewportWidth > ExtentWidth)
                {
                    HorizontalOffset = ExtentWidth - ViewportWidth;
                    HorizontalOffset = HorizontalOffset >= 0 ? HorizontalOffset : 0;
                }
            }
            ScrollOwner?.InvalidateScrollInfo();
        }

        private void SwitchFrame(int oldIndex,
            int newIndex,
            IFramePreviewerViewModel oldItem,
            IFramePreviewerViewModel newItem)
        {
            var res = itemController.SwitchFrameWithAnimation(oldIndex,
                newIndex,
                oldItem,
                newItem,
                frameDistance,
                out DoubleAnimation?[]? anims,
                out FramePreviewer? oldItemContainer,
                out FramePreviewer? newItemContainer);

            if (res && oldItemContainer != null && newItemContainer != null)
            {
                if (contentContainerCanvas?.Children.Contains(newItemContainer) == false)
                {
                    contentContainerCanvas.Children.Add(newItemContainer);
                }

                if (contentContainerCanvas?.Children.Contains(oldItemContainer) == false)
                {
                    contentContainerCanvas.Children.Add(oldItemContainer);
                }

                var animationStoryboard = new Storyboard();
                animationStoryboard.FillBehavior = FillBehavior.Stop;
                anims?.FoEach(it => it?.Apply(it => animationStoryboard.Children.Add(it)));
                animationStoryboard.Completed += (s, e) =>
                {
                    if (itemController[oldItemContainer] == null)
                    {
                        contentContainerCanvas?.Children.Remove(oldItemContainer);
                    }
                    else if (itemController[newItemContainer] == null)
                    {
                        contentContainerCanvas?.Children.Remove(newItemContainer);
                    }
                };
                this?.BeginStoryboard(animationStoryboard);
            }
        }

        private void InsertFrame(int insertedIndex,
           IFramePreviewerViewModel newItem)
        {
            var res = itemController.InsertFrameWithAnimation(insertedIndex,
                newItem,
                frameDistance,
                out DoubleAnimation?[]? anims,
                out FramePreviewer? oldItemContainer,
                out FramePreviewer? newItemContainer);

            if (res && oldItemContainer != null && newItemContainer != null)
            {
                contentContainerCanvas?.Children.Add(newItemContainer);
                var animationStoryboard = new Storyboard();
                animationStoryboard.FillBehavior = FillBehavior.Stop;
                anims?.FoEach(it => it?.Apply(it => animationStoryboard.Children.Add(it)));

                animationStoryboard.Completed += (s, e) =>
                {
                    contentContainerCanvas?.Children.Remove(oldItemContainer);
#if DEBUG
                    itemController.AssertForDebug();
#endif
                };
                BeginStoryboard(animationStoryboard);
            }
            else if (res &&
                oldItemContainer == null &&
                newItemContainer == null &&
                anims != null)
            {
                var animationStoryboard = new Storyboard();
                animationStoryboard.FillBehavior = FillBehavior.Stop;
                anims?.FoEach(it => it?.Apply(it => animationStoryboard.Children.Add(it)));

                animationStoryboard.Completed += (s, e) =>
                {
#if DEBUG
                    itemController.AssertForDebug();
#endif
                };
                BeginStoryboard(animationStoryboard);
            }
            else if (res && newItemContainer != null && anims == null)
            {
                contentContainerCanvas?.Children.Add(newItemContainer);
                InvalidateMeasure();
            }

        }

        private void RemoveFrame(int removedIndex)
        {
            var res = itemController.RemoveFrameWithAnimation(removedIndex,
                frameDistance,
                out DoubleAnimation?[]? anims,
                out FramePreviewer? oldItemContainer,
                out FramePreviewer? newItemContainer);

            if (res && oldItemContainer != null)
            {
                contentContainerCanvas?.Children.Remove(oldItemContainer);

                if (newItemContainer != null)
                {
                    contentContainerCanvas?.Children.Add(newItemContainer);
                }
                if (anims!.Length == 0)
                {
                    InvalidateMeasure();
                }
                else
                {
                    var animationStoryboard = new Storyboard();
                    animationStoryboard.FillBehavior = FillBehavior.Stop;
                    anims?.FoEach(it => it?.Apply(it => animationStoryboard.Children.Add(it)));

                    animationStoryboard.Completed += (s, e) =>
                    {
#if DEBUG
                        itemController.AssertForDebug();
#endif
                    };
                    BeginStoryboard(animationStoryboard);
                }
            }
            else if (res && anims != null)
            {
                if (anims.Length > 0)
                {
                    var animationStoryboard = new Storyboard();
                    animationStoryboard.FillBehavior = FillBehavior.Stop;
                    anims?.FoEach(it => it?.Apply(it => animationStoryboard.Children.Add(it)));

                    animationStoryboard.Completed += (s, e) =>
                    {
#if DEBUG
                        itemController.AssertForDebug();
#endif
                    };
                    BeginStoryboard(animationStoryboard);
                }
                else
                {
                    InvalidateMeasure();
                }

            }

        }

        private void SetDesiredItemContainerSize(Size newSize)
        {
            if (desiredItemContainerSize != newSize)
            {
                itemController.SetDesiredItemContainerSize(newSize);
            }
            desiredItemContainerSize = newSize;
        }
    }

    internal class FrameItemController
    {
        public FrameItemController(FrameLineEditorVirtualizingPanel panelOwner)
        {
            this.panelOwner = panelOwner;
        }
        public int RealInitialVisibleItemIndex { get; private set; }

        public int VisibleItemCount
        {
            get
            {
                return viewCaches.Count;
            }
        }
        public int LastVisibleItemIndex
        {
            get
            {
                return viewCaches.Count == 0 ? -1 : viewCaches.Last().Index;
            }
        }
        public int FirstVisibleItemIndex
        {
            get
            {
                return viewCaches.Count == 0 ? -1 : viewCaches.First().Index;
            }
        }
        public class ViewCache
        {
            public FramePreviewer View { get; set; }

            // Vị trí hiển thị thực tế của view khi hiển thị trên màn hình
            public int Index { get; set; }
            public ViewCache(FramePreviewer view, int index)
            {
                View = view;
                Index = index;
            }

            public Rect ContentCanvasPosition { get; set; }
            public Rect MainPanelPosition { get; set; }
        }

        private FrameLineEditorVirtualizingPanel panelOwner;
        private Collection<IFramePreviewerViewModel>? itemSourceCache;
        private Collection<ViewCache> viewCaches = new Collection<ViewCache>();
        private Dictionary<FramePreviewer, ViewCache> viewCacheMap = new Dictionary<FramePreviewer, ViewCache>();
        private Dictionary<int, ViewCache> viewCacheIndexMap = new Dictionary<int, ViewCache>();
        private int realLastVisibleItemIndex;
        private int expectedLastVisibleItemIndex;
        private Size desiredItemContainerSize;

        public static ViewCache CreateViewCache(FramePreviewer itemContainer, int index)
        {
            return new ViewCache(itemContainer, index);
        }

        public void SetUpVisibleSection(int newStartVisibleItemIndex,
            int newEndVisibleItemIndex,
            out List<ViewCache> addedCaches,
            out List<ViewCache> removedCaches)
        {
            if (itemSourceCache == null) { throw new Exception(); }
            addedCaches = new List<ViewCache>();
            removedCaches = new List<ViewCache>();

            expectedLastVisibleItemIndex = newEndVisibleItemIndex;
            var visibleItemCount = VisibleItemCount;
            var newVisibleItemCount = newEndVisibleItemIndex - newStartVisibleItemIndex + 1;
            if (newEndVisibleItemIndex > itemSourceCache.Count - 1)
            {
                newEndVisibleItemIndex = itemSourceCache.Count - 1;
                newStartVisibleItemIndex = newEndVisibleItemIndex - newVisibleItemCount + 1;
                newStartVisibleItemIndex = newStartVisibleItemIndex >= 0 ? newStartVisibleItemIndex : 0;
            }

            if (newVisibleItemCount > itemSourceCache.Count)
            {
                newVisibleItemCount = itemSourceCache.Count;
            }
#if DEBUG
            Debug.Assert(newVisibleItemCount <= itemSourceCache.Count);
            Debug.Assert(newEndVisibleItemIndex < itemSourceCache.Count);
            Debug.Assert(newStartVisibleItemIndex >= 0);
#endif
            // Add or clean item container if visible section is changed
            if (newVisibleItemCount > visibleItemCount)
            {
                for (int i = 0; i < newVisibleItemCount - visibleItemCount; i++)
                {
                    var lastItemIndex = LastVisibleItemIndex;

                    if (itemSourceCache.Count > 0 && lastItemIndex >= itemSourceCache.Count - 1)
                    {
                        // Add item from the left
                        var firstItemIndex = FirstVisibleItemIndex;
                        FramePreviewer newFrame = GenerateItemContainer(firstItemIndex - 1);
                        var newViewCache = FrameItemController.CreateViewCache(newFrame, firstItemIndex - 1);
                        InsertNewCacheToFront(newViewCache);
                        addedCaches.Add(newViewCache);
                    }
                    else
                    {
                        // Add new item container
                        FramePreviewer newFrame = GenerateItemContainer(lastItemIndex + 1);
                        var newViewCache = FrameItemController.CreateViewCache(newFrame, lastItemIndex + 1);
                        AddNewCacheToBack(newViewCache);
                        addedCaches.Add(newViewCache);
                    }
                }

            }
            else if (newVisibleItemCount < visibleItemCount)
            {
                //  Clean old item container
                for (int i = 0; i < visibleItemCount - newVisibleItemCount; i++)
                {
                    var lastCache = RemoveLastCache();
                    if (lastCache != null)
                    {
                        removedCaches.Add(lastCache);
                    }

                }
            }
#if DEBUG
            AssertForDebug();
#endif

            //Switch container, and assign viewmodel to item container
            if (newStartVisibleItemIndex > RealInitialVisibleItemIndex)
            {
                var numberItemToBring = newStartVisibleItemIndex - RealInitialVisibleItemIndex;
                var lastItemIndex = LastVisibleItemIndex;

                // Trong trường hợp click thẳng trên thanh scroll thì lượng numberItemToBring có thể quá lớn dẫn tới
                // performance issue
                if (numberItemToBring > viewCaches.Count)
                {
                    viewCacheIndexMap.Clear();
                    for (int i = 0; i < newEndVisibleItemIndex - newStartVisibleItemIndex + 1; i++)
                    {
                        viewCaches[i].Index = newStartVisibleItemIndex + i;
                        viewCaches[i].View.ViewModel = itemSourceCache?[newStartVisibleItemIndex + i];
                        viewCacheIndexMap.Add(viewCaches[i].Index, viewCaches[i]);
                    }
                }
                else if (numberItemToBring <= viewCaches.Count)
                {
                    for (int i = 0; i < numberItemToBring; i++)
                    {
                        var oldElement = viewCaches[0];
                        viewCaches.RemoveAt(0);
                        viewCacheIndexMap.Remove(oldElement.Index);
                        oldElement.Index = lastItemIndex + 1 + i;
                        if (oldElement.Index < itemSourceCache?.Count)
                            oldElement.View.ViewModel = itemSourceCache[oldElement.Index];
                        viewCaches.Add(oldElement);
                        viewCacheIndexMap.Add(oldElement.Index, oldElement);
                    }
                }
            }
            else if (newEndVisibleItemIndex < realLastVisibleItemIndex)
            {
                var numberItemToBring = RealInitialVisibleItemIndex - newStartVisibleItemIndex;
                var firstItemIndex = FirstVisibleItemIndex;

                // Trong trường hợp click thẳng trên thanh scroll thì lượng numberItemToBring có thể quá lớn dẫn tới
                // performance issue
                if (numberItemToBring > viewCaches.Count)
                {
                    viewCacheIndexMap.Clear();
                    for (int i = 0; i < newEndVisibleItemIndex - newStartVisibleItemIndex + 1; i++)
                    {
                        viewCaches[i].Index = newStartVisibleItemIndex + i;
                        viewCaches[i].View.ViewModel = itemSourceCache?[newStartVisibleItemIndex + i];
                        viewCacheIndexMap.Add(viewCaches[i].Index, viewCaches[i]);
                    }
                }
                else if (numberItemToBring <= viewCaches.Count)
                {
                    for (int i = 0; i < numberItemToBring; i++)
                    {
                        var lastIndex = viewCaches.Count - 1;
                        var oldElement = viewCaches[lastIndex];
                        viewCaches.RemoveAt(lastIndex);
                        viewCacheIndexMap.Remove(oldElement.Index);

                        oldElement.Index = firstItemIndex - 1 - i;

                        oldElement.View.ViewModel = itemSourceCache?[oldElement.Index];
                        viewCaches.Insert(0, oldElement);
                        viewCacheIndexMap.Add(oldElement.Index, oldElement);
                    }

                }

            }

#if DEBUG
            AssertForDebug();
#endif

            //Apply to cache
            RealInitialVisibleItemIndex = newStartVisibleItemIndex;
            realLastVisibleItemIndex = newEndVisibleItemIndex;
        }

        public void SetupItemSource(Collection<IFramePreviewerViewModel>? itemSource)
        {
            itemSourceCache = itemSource;
            realLastVisibleItemIndex = 0;
            RealInitialVisibleItemIndex = 0;
            expectedLastVisibleItemIndex = 0;
            viewCaches.Clear();
            viewCacheIndexMap.Clear();
            viewCacheMap.Clear();
            desiredItemContainerSize = Size.Empty;
        }

        public void InsertNewCacheToFront(ViewCache newCache)
        {
            viewCaches.Insert(0, newCache);
            viewCacheMap.Add(newCache.View, newCache);
            viewCacheIndexMap.Add(newCache.Index, newCache);
        }

        public void AddNewCacheToBack(ViewCache newCache)
        {
            viewCaches.Add(newCache);
            viewCacheMap.Add(newCache.View, newCache);
            viewCacheIndexMap.Add(newCache.Index, newCache);
        }

        public ViewCache? RemoveLastCache()
        {
            var lastIndex = viewCaches.Count - 1;
            if (lastIndex >= 0)
            {
                var lastItem = viewCaches[lastIndex];
                viewCaches.RemoveAt(lastIndex);
                viewCacheMap.Remove(lastItem.View);
                viewCacheIndexMap.Remove(lastItem.Index);
                return lastItem;
            }
            return null;
        }

        public ViewCache? this[FramePreviewer frame]
        {
            get
            {
                if (!viewCacheMap.ContainsKey(frame))
                {
                    return null;
                }
                return viewCacheMap[frame];
            }
            set
            {
                if (value != null)
                    viewCacheMap[frame] = value;
                else
                    viewCacheMap.Remove(frame);
            }
        }

        public ViewCache? GetItemContainerBaseOnRelativePositionToPanel(Rect relativePanelPos, int excludedViewIndex)
        {
            foreach (var c in viewCaches)
            {
                if (excludedViewIndex != c.Index && relativePanelPos.IntersectsWith(c.MainPanelPosition))
                {
                    return c;
                }
            }
            return null;
        }

        public int GetItemContainerIndexBaseOnRelativePositionToPanel(double relativePanelPosX, double frameDistance)
        {
            if (viewCaches.Count == 0)
            {
                return 0;
            }
            var lastItem = viewCaches.Last();
            if (itemSourceCache != null && viewCaches.Last().Index == itemSourceCache.Count - 1
                && relativePanelPosX > lastItem.MainPanelPosition.Right)
            {
                return itemSourceCache.Count;
            }
            foreach (var c in viewCaches)
            {
                if (relativePanelPosX <= c.MainPanelPosition.Right + frameDistance &&
                    relativePanelPosX >= c.MainPanelPosition.Left)
                {
                    if (itemSourceCache != null &&
                        c.Index == itemSourceCache.Count - 1 &&
                        relativePanelPosX > c.MainPanelPosition.Right)
                    {
                        return itemSourceCache.Count;
                    }
                    return c.Index;
                }
            }
            return 0;
        }

        public void ArrangeViewCache(Size arrangeSize,
            Size desiredItemContainerSize,
            double frameDistance,
            double horizontalOffset,
            double containerMarginLeft)
        {
            for (int i = 0; i < viewCaches.Count; i++)
            {
                var frame = viewCaches[i].View;
#if DEBUG
                if (frame.Tag == "false")
                {
                    //if (viewCaches[i].Index % 3 == 0)
                    //{
                    //    frame.Background = new SolidColorBrush(Colors.Black);
                    //}
                    //else if (viewCaches[i].Index % 3 == 1)
                    //{
                    //    frame.Background = new SolidColorBrush(Colors.OliveDrab);
                    //}
                    //else if (viewCaches[i].Index % 3 == 2)
                    //{
                    //    frame.Background = new SolidColorBrush(Colors.DarkBlue);
                    //}

                    frame.Tag = viewCaches[i].Index;
                }
#endif

                var left = viewCaches[i].Index *
                    (desiredItemContainerSize.Width + frameDistance);
                Canvas.SetLeft(frame, left);

                viewCaches[i].ContentCanvasPosition = new Rect(left + containerMarginLeft,
                    0,
                    desiredItemContainerSize.Width,
                    desiredItemContainerSize.Height);
                viewCaches[i].MainPanelPosition = new Rect(left - horizontalOffset + containerMarginLeft,
                    (arrangeSize.Height - desiredItemContainerSize.Height) / 2,
                    desiredItemContainerSize.Width,
                    desiredItemContainerSize.Height);
            }
        }

        public void SetDesiredItemContainerSize(Size newSize)
        {
            desiredItemContainerSize = newSize;
            foreach (var v in viewCaches)
            {
                v.View.Height = newSize.Height;
                v.View.Width = newSize.Width;
            }
        }

        public void AssertForDebug()
        {
            if (itemSourceCache != null)
            {
                Debug.Assert(RealInitialVisibleItemIndex >= 0);
                Debug.Assert(realLastVisibleItemIndex < itemSourceCache.Count);
            }

            // Đảm bảo index trong view cache luôn the thứ tự tăng dần
            for (int i = 0; i < viewCaches.Count - 1; i++)
            {
                Debug.Assert(viewCaches[i + 1].Index - viewCaches[i].Index == 1);
            }
            foreach (var kp in viewCacheIndexMap)
            {
                Debug.Assert(kp.Key == kp.Value.Index);

                // đảm bảo index trong viewCache tương ứng với index của item trên item source
                Debug.Assert(kp.Value.View.ViewModel == itemSourceCache![kp.Value.Index]);
            }

            // Đảm bảo map và cache size luôn bằng nhau
            Debug.Assert(viewCacheMap.Count == viewCaches.Count);
        }

        private FramePreviewer GenerateItemContainer(int viewModelIdex)
        {
            var newFrame = GenerateItemContainer();
            if (itemSourceCache != null)
            {
                newFrame.ViewModel = itemSourceCache[viewModelIdex];
            }
            return newFrame;
        }

        private FramePreviewer GenerateItemContainer()
        {
            Debug.Assert(!desiredItemContainerSize.IsEmpty);
            var newFrame = new FramePreviewer();
#if DEBUG
            newFrame.Tag = "false";
#endif

            newFrame.PanelOwner = panelOwner;
            newFrame.Height = desiredItemContainerSize.Height;
            newFrame.Width = desiredItemContainerSize.Width;
            return newFrame;
        }

        public bool SwitchFrameWithAnimation(int oldIndex,
            int newIndex,
            IFramePreviewerViewModel oldItem,
            IFramePreviewerViewModel newItem,
            double frameDistance,
            out DoubleAnimation?[]? switchFrameAnimations,
            out FramePreviewer? oldItemContainer,
            out FramePreviewer? newItemContainer)
        {
            viewCacheIndexMap.TryGetValue(oldIndex, out ViewCache? oldItemViewCache);
            viewCacheIndexMap.TryGetValue(newIndex, out ViewCache? newItemViewCache);
            switchFrameAnimations = null;
            oldItemContainer = null;
            newItemContainer = null;
            // Nếu cả 2 đều bằng null, có nghĩa là vị trí switch nằm ngoài vị trí hiển thị
            // Nên case này không cần xử lý
            if (newItemViewCache == null && oldItemViewCache == null)
            {
                return false;
            }

            Debug.Assert(oldItemViewCache != null || newItemViewCache != null);

            if (oldItemViewCache == null)
            {
                oldItemViewCache = new ViewCache(GenerateItemContainer(), oldIndex);
                oldItemViewCache.ContentCanvasPosition = newItemViewCache!.ContentCanvasPosition;
                oldItemViewCache.MainPanelPosition = newItemViewCache!.MainPanelPosition;
                oldItemViewCache.View.ViewModel = oldItem;
                viewCacheMap.Add(oldItemViewCache.View, oldItemViewCache);
            }

            if (newItemViewCache == null)
            {
                newItemViewCache = new ViewCache(GenerateItemContainer(), newIndex);
                newItemViewCache.View.ViewModel = newItem;
                newItemViewCache.ContentCanvasPosition = oldItemViewCache!.ContentCanvasPosition;
                newItemViewCache.MainPanelPosition = oldItemViewCache!.MainPanelPosition;
                viewCacheMap.Add(newItemViewCache.View, newItemViewCache);
            }

            var oldViewCacheIndex = viewCaches.IndexOf(oldItemViewCache);
            var newViewCacheIndex = viewCaches.IndexOf(newItemViewCache);

            oldItemViewCache.Index = newIndex;
            newItemViewCache.Index = oldIndex;

            oldItemContainer = oldItemViewCache.View;
            newItemContainer = newItemViewCache.View;

            var anim1 = ReArrangeItemContainerWithAnimation(oldIndex, oldItemViewCache, frameDistance);
            var anim2 = ReArrangeItemContainerWithAnimation(newIndex, newItemViewCache, frameDistance);
            switchFrameAnimations = new DoubleAnimation?[] { anim1, anim2 };
            if (oldViewCacheIndex != -1)
            {
                viewCaches[oldViewCacheIndex] = newItemViewCache;
                viewCacheIndexMap.Remove(oldIndex);
                viewCacheIndexMap.Add(newItemViewCache.Index, newItemViewCache);
            }
            else
            {
                viewCacheMap.Remove(newItemViewCache.View);
            }

            if (newViewCacheIndex != -1)
            {
                viewCaches[newViewCacheIndex] = oldItemViewCache;
                viewCacheIndexMap.Remove(newIndex);
                viewCacheIndexMap.Add(oldItemViewCache.Index, oldItemViewCache);
            }
            else
            {
                viewCacheMap.Remove(oldItemViewCache.View);
            }

            return true;
        }

        public bool InsertFrameWithAnimation(int insertedIndex,
            IFramePreviewerViewModel newItem,
            double frameDistance,
            out DoubleAnimation?[]? insertFrameAnimations,
            out FramePreviewer? oldItemContainer,
            out FramePreviewer? newItemContainer)
        {
            viewCacheIndexMap.TryGetValue(insertedIndex, out ViewCache? oldViewCache);
            insertFrameAnimations = null;
            newItemContainer = null;
            oldItemContainer = null;
            // Nếu oldViewCache bằng null, có nghĩa là vị trí inserted nằm ngoài vị trí hiển thị
            // Nên case này không cần xử lý
            if (itemSourceCache == null || oldViewCache == null && itemSourceCache?.Count > 0
                && insertedIndex > realLastVisibleItemIndex && realLastVisibleItemIndex != itemSourceCache.Count - 2)
            {
                return false;
            }

            if (insertedIndex < RealInitialVisibleItemIndex)
            {
                insertFrameAnimations = new DoubleAnimation[viewCaches.Count];
                for (int i = 0; i < viewCaches.Count; i++)
                {
                    var viewCache = viewCaches[i];
                    viewCacheIndexMap.Remove(viewCache.Index);
                    viewCache.Index += 1;
                    insertFrameAnimations[i] =
                        ReArrangeItemContainerWithAnimation(viewCache.Index - 1, viewCache, frameDistance);
                }
                RealInitialVisibleItemIndex += 1;
                realLastVisibleItemIndex += 1;

                for (int i = 0; i < viewCaches.Count; i++)
                    viewCacheIndexMap.Add(viewCaches[i].Index, viewCaches[i]);
            }
            else if (insertedIndex >= RealInitialVisibleItemIndex && insertedIndex <= realLastVisibleItemIndex)
            {
                var lastItemContainerIndex = viewCaches.Last().Index;

                insertFrameAnimations = new DoubleAnimation[lastItemContainerIndex - insertedIndex + 1];
                for (int i = 0; i < lastItemContainerIndex - insertedIndex + 1; i++)
                {
                    var viewCache = viewCacheIndexMap[i + insertedIndex];
                    viewCacheIndexMap.Remove(i + insertedIndex);
                    viewCache.Index += 1;
                    insertFrameAnimations[i] =
                        ReArrangeItemContainerWithAnimation(i + insertedIndex, viewCache, frameDistance);

                }

                var newItemViewCache = new ViewCache(GenerateItemContainer(), insertedIndex);
                newItemViewCache.ContentCanvasPosition = oldViewCache?.ContentCanvasPosition ?? new Rect();
                newItemViewCache.MainPanelPosition = oldViewCache?.MainPanelPosition ?? new Rect();
                newItemViewCache.View.ViewModel = newItem;
                Canvas.SetLeft(newItemViewCache.View, oldViewCache != null ? Canvas.GetLeft(oldViewCache.View) : 0);

                var oldIndexOnViewCache = oldViewCache == null ? 0 : viewCaches.IndexOf(oldViewCache);
                var itemNeedToRemove = viewCaches[viewCaches.Count - 1];
                viewCaches.Insert(oldIndexOnViewCache, newItemViewCache);
                viewCaches.RemoveAt(viewCaches.Count - 1);
                viewCacheMap.Remove(itemNeedToRemove.View);
                viewCacheMap.Add(newItemViewCache.View, newItemViewCache);

                for (int i = 0; i < viewCaches.Count - oldIndexOnViewCache; i++)
                {
                    viewCacheIndexMap.Add(viewCaches[oldIndexOnViewCache + i].Index,
                        viewCaches[oldIndexOnViewCache + i]);
                }

                newItemContainer = newItemViewCache.View;
                oldItemContainer = itemNeedToRemove.View;
            }
            else if (insertedIndex > realLastVisibleItemIndex && insertedIndex <= expectedLastVisibleItemIndex)
            {
                var newItemViewCache = new ViewCache(GenerateItemContainer(), insertedIndex);
                newItemViewCache.View.ViewModel = newItem;
                ArrangeItemContainer(newItemViewCache, frameDistance);

                viewCaches.Add(newItemViewCache);
                viewCacheMap.Add(newItemViewCache.View, newItemViewCache);
                viewCacheIndexMap.Add(insertedIndex, newItemViewCache);
                newItemContainer = newItemViewCache.View;
            }

            return true;
        }

        public bool RemoveFrameWithAnimation(int removeIndex,
          double frameDistance,
          out DoubleAnimation?[]? removeFrameAnimations,
          out FramePreviewer? oldItemContainer,
          out FramePreviewer? newItemContainer)
        {
            viewCacheIndexMap.TryGetValue(removeIndex, out ViewCache? oldViewCache);
            removeFrameAnimations = null;
            newItemContainer = null;
            oldItemContainer = null;
            // Nếu oldViewCache bằng null, có nghĩa là vị trí inserted nằm ngoài vị trí hiển thị
            // Nên case này không cần xử lý
            if (itemSourceCache == null ||
                oldViewCache == null && itemSourceCache?.Count > 0 && removeIndex > realLastVisibleItemIndex ||
                oldViewCache == null && removeIndex >= itemSourceCache?.Count)
            {
                return false;
            }

            if (removeIndex < RealInitialVisibleItemIndex)
            {
                removeFrameAnimations = new DoubleAnimation[viewCaches.Count];
                for (int i = 0; i < viewCaches.Count; i++)
                {
                    var viewCache = viewCaches[i];
                    viewCacheIndexMap.Remove(viewCache.Index);
                    viewCache.Index -= 1;
                    removeFrameAnimations[i] =
                        ReArrangeItemContainerWithAnimation(viewCache.Index + 1, viewCache, frameDistance);
                }
                RealInitialVisibleItemIndex -= 1;
                realLastVisibleItemIndex -= 1;

                for (int i = 0; i < viewCaches.Count; i++)
                    viewCacheIndexMap.Add(viewCaches[i].Index, viewCaches[i]);
            }
            else
            {
                var lastItemContainerIndex = viewCaches.Last().Index;

                removeFrameAnimations = new DoubleAnimation[lastItemContainerIndex - removeIndex];

                var removedCache = viewCacheIndexMap[removeIndex];
                viewCaches.Remove(removedCache);
                viewCacheIndexMap.Remove(removeIndex);
                viewCacheMap.Remove(removedCache.View);

                List<ViewCache> tempList = new List<ViewCache>();
                for (int i = 1; i < lastItemContainerIndex - removeIndex + 1; i++)
                {
                    var viewCache = viewCacheIndexMap[i + removeIndex];
                    viewCacheIndexMap.Remove(i + removeIndex);
                    viewCache.Index -= 1;
                    removeFrameAnimations[i - 1] =
                        ReArrangeItemContainerWithAnimation(i + removeIndex, viewCache, frameDistance);
                    tempList.Add(viewCache);
                }
                foreach (var item in tempList)
                {
                    viewCacheIndexMap.Add(item.Index, item);
                }

                ViewCache newItemViewCache;

                if (realLastVisibleItemIndex >= itemSourceCache!.Count)
                {
                    RealInitialVisibleItemIndex -= 1;
                    realLastVisibleItemIndex -= 1;
                    if (RealInitialVisibleItemIndex >= 0)
                    {
                        newItemViewCache = new ViewCache(GenerateItemContainer(RealInitialVisibleItemIndex), RealInitialVisibleItemIndex);
                        viewCaches.Insert(0, newItemViewCache);
                        viewCacheMap.Add(newItemViewCache.View, newItemViewCache);
                        viewCacheIndexMap.Add(newItemViewCache.Index, newItemViewCache);
                        ArrangeItemContainer(newItemViewCache, frameDistance);
                        newItemContainer = newItemViewCache.View;
                    }
                    else
                    {
                        RealInitialVisibleItemIndex = 0;
                    }
                }
                else
                {
                    newItemViewCache = new ViewCache(GenerateItemContainer(realLastVisibleItemIndex), realLastVisibleItemIndex);
                    viewCaches.Add(newItemViewCache);
                    viewCacheMap.Add(newItemViewCache.View, newItemViewCache);
                    viewCacheIndexMap.Add(newItemViewCache.Index, newItemViewCache);
                    ArrangeItemContainer(newItemViewCache, frameDistance);
                    newItemContainer = newItemViewCache.View;
                }
                oldItemContainer = removedCache.View;
            }


            return true;
        }

        private DoubleAnimation? ReArrangeItemContainerWithAnimation(int oldIndex,
            ViewCache view,
            double frameDistance)
        {
            if (desiredItemContainerSize.IsEmpty == true) return null;

            var fromLeft = oldIndex *
                   (desiredItemContainerSize.Width + frameDistance);
            var toLeft = view.Index *
                   (desiredItemContainerSize.Width + frameDistance);
            Canvas.SetLeft(view.View, fromLeft);

            DoubleAnimation xAnim = new DoubleAnimation(fromLeft, toLeft, TimeSpan.FromSeconds(0.1));
            Storyboard.SetTarget(xAnim, view.View);
            Storyboard.SetTargetProperty(xAnim, new PropertyPath("(Canvas.Left)"));
            xAnim.Completed += (s, e) =>
            {
                Canvas.SetLeft(view.View, toLeft);
            };
            xAnim.FillBehavior = FillBehavior.Stop;
            return xAnim;
        }

        private void ArrangeItemContainer(ViewCache view,
            double frameDistance)
        {
            var toLeft = view.Index *
                   (desiredItemContainerSize.Width + frameDistance);
            Canvas.SetLeft(view.View, toLeft);
        }
    }

    internal class FramePanelMenuContextController
    {
        private double contextMenuPosX;
        private double contextMenuPosY;
        private FrameLineEditorVirtualizingPanel owner;
        public FramePanelMenuContextController(FrameLineEditorVirtualizingPanel panelOwner)
        {
            owner = panelOwner;
            var menu = new ContextMenu();
            panelOwner.ContextMenuOpening += OnMenuOpenning;
            MenuItem insertItem = new MenuItem();
            insertItem.Header = "Insert frame";
            insertItem.Click += OnInsertFrameClick;
            menu.Items.Add(insertItem);
            panelOwner.ContextMenu = menu;
        }

        private void OnMenuOpenning(object sender, ContextMenuEventArgs e)
        {
            contextMenuPosX = e.CursorLeft;
            contextMenuPosY = e.CursorTop;
        }

        private void OnInsertFrameClick(object sender, RoutedEventArgs e)
        {
            var newFrameIndex = owner.GetItemContainerIndexBaseOnRelativePositionToPanel(contextMenuPosX);
            owner.RaisePreviewFrameInsertEvent(newFrameIndex);
        }
    }

    public class FrameLineEventArgs
    {
        public static FrameLineEventArgs CreateSwitchEvent(int switchedFrame1Index, int switchedFrame2Index)
        {
            return new FrameLineEventArgs(switchedFrame1Index, switchedFrame2Index);
        }

        public static FrameLineEventArgs CreateAddingNewFrameEvent(int newFrameIndex)
        {
            var arg = new FrameLineEventArgs();
            arg.NewFrameIndex = newFrameIndex;
            return arg;
        }

        public static FrameLineEventArgs CreateRemovingOldFrameEvent(int oldFrameIndex)
        {
            var arg = new FrameLineEventArgs();
            arg.OldFrameIndex = oldFrameIndex;
            return arg;
        }


        private FrameLineEventArgs(int switchedFrame1Index, int switchedFrame2Index)
        {
            SwitchedFrame1Index = switchedFrame1Index;
            SwitchedFrame2Index = switchedFrame2Index;
        }

        private FrameLineEventArgs()
        {
        }

        public bool Handled { get; set; }
        public int SwitchedFrame1Index { get; private set; } = -1;
        public int SwitchedFrame2Index { get; private set; } = -1;
        public int NewFrameIndex { get; private set; } = -1;
        public int OldFrameIndex { get; private set; } = -1;

    }

    public class FameLineMouseEventArgs : MouseButtonEventArgs
    {
        public int FrameIndex { get; private set; }
        public FameLineMouseEventArgs(int frameIndex, MouseDevice mouse, int timestamp, MouseButton button)
            : base(mouse, timestamp, button)
        {
            FrameIndex = frameIndex;
        }
    }
}

