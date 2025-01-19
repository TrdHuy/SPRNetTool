using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;
using static ArtWiz.View.Widgets.CodeBlock.ViewCacheManagement;

namespace ArtWiz.View.Widgets.CodeBlock
{
    public interface IVirtualizingPanel
    {
        FrameworkElement CreateItemView();
    }

    /// <summary>
    /// Interaction logic for CustomVerticalVirtualizingPanel.xaml
    /// </summary>
    public abstract class VerticalVirtualizingPanel : UserControl, IScrollInfo, IVirtualizingPanel
    {
        private const double DEFAULT_ITEM_SIZE_HEIGHT = 50d;
        #region Dependency Properties
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(
                "ItemTemplate",
                typeof(DataTemplate),
                typeof(VerticalVirtualizingPanel),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure)
            );

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public static readonly DependencyProperty ScrollOwnerProperty =
        DependencyProperty.Register("ScrollOwner", typeof(ScrollViewer),
            typeof(VerticalVirtualizingPanel), new PropertyMetadata(null));

        #endregion

        #region Fields
        private double _verticalOffset;
        private double _horizontalOffset;
        private double _extentHeight;
        private double _extentWidth;
        private double _viewportHeight;
        private double _viewportWidth;
        private Size _desiredItemSize = Size.Empty; // Default each item size
        private ViewCacheManagement _viewCacheManager;
        private System.Collections.IEnumerable? _itemsSource { get; set; } // Backing collection for the panel
        private System.Collections.IList? _itemsSourceAsList { get; set; } // Backing collection for the panel
        #endregion

        #region Initialization 
        public VerticalVirtualizingPanel()
        {
            _viewCacheManager = new ViewCacheManagement(this);
        }
        #endregion

        #region Virtualizing Panel Implementation
        public FrameworkElement CreateItemView()
        {
            ContentPresenter presenter = new()
            {
                ContentTemplate = ItemTemplate
            };
            return presenter;
        }

        public abstract Canvas PART_ContentCanvasContainer { get; }
        public abstract Canvas PART_MainCanvasContainer { get; }
        #endregion

        #region IScrollInfo Implementation
        public bool CanVerticallyScroll { get; set; } = true;
        public bool CanHorizontallyScroll { get; set; } = true;

        public ScrollViewer ScrollOwner
        {
            get { return (ScrollViewer)GetValue(ScrollOwnerProperty); }
            set { SetValue(ScrollOwnerProperty, value); }
        }

        public double VerticalOffset
        {
            get => _verticalOffset;
            private set
            {
                _verticalOffset = Math.Max(0, Math.Min(value, ExtentHeight - ViewportHeight));
                ScrollOwner?.InvalidateScrollInfo();
                InvalidateMeasure();
            }
        }

        public double HorizontalOffset
        {
            get => _horizontalOffset;
            private set
            {
                _horizontalOffset = Math.Max(0, Math.Min(value, ExtentWidth - ViewportWidth));
                ScrollOwner?.InvalidateScrollInfo();
                InvalidateMeasure();
            }
        }

        public double ExtentHeight => _extentHeight;
        public double ExtentWidth => _extentWidth;
        public double ViewportHeight => _viewportHeight;
        public double ViewportWidth => _viewportWidth;

        public void LineDown() => VerticalOffset += _desiredItemSize.Height;
        public void LineUp() => VerticalOffset -= _desiredItemSize.Height;
        public void PageDown() => VerticalOffset += ViewportHeight;
        public void PageUp() => VerticalOffset -= ViewportHeight;

        public void LineRight() => HorizontalOffset += 20;
        public void LineLeft() => HorizontalOffset -= 20;
        public void PageRight() => HorizontalOffset += ViewportWidth;
        public void PageLeft() => HorizontalOffset -= ViewportWidth;

        public void SetVerticalOffset(double offset) => VerticalOffset = offset;
        public void SetHorizontalOffset(double offset) => HorizontalOffset = offset;

        public void MouseWheelDown() => LineDown();
        public void MouseWheelUp() => LineUp();

        public void MouseWheelLeft() => LineLeft();
        public void MouseWheelRight() => LineRight();

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return new Rect(HorizontalOffset, VerticalOffset, ViewportWidth, ViewportHeight);
        }
        #endregion

        #region Item Management
        public void SetItems(IEnumerable<object> items)
        {
            if (items is INotifyCollectionChanged cast)
            {
                cast.CollectionChanged += OnItemCollectionChanged;
            }

            if (_itemsSource is INotifyCollectionChanged cast2)
            {
                cast2.CollectionChanged -= OnItemCollectionChanged;
            }
            _itemsSource = items;
            if (_itemsSource is System.Collections.IList)
            {
                _itemsSourceAsList = _itemsSource as System.Collections.IList;
            }
            else
            {
                throw new NotSupportedException();
            }
            _viewCacheManager.SetupItemSource(_itemsSourceAsList);
            PART_ContentCanvasContainer.Children.Clear();
            MeasureDesiredItemSize(forceMeasure: true);

            InvalidateMeasure();
            ScrollOwner?.InvalidateScrollInfo();
        }

        private void OnItemCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e)
            {
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
                                var vm = e.NewItems[itemIndex]!;
                                InsertFrame(i, vm);
                            }
                        }
                    }
                    return;
            }
        }

        private void InsertFrame(int insertedIndex,
          object newItem)
        {
            var res = _viewCacheManager.InsertFrame(insertedIndex,
                newItem,
                0,
                out FrameworkElement? oldItemContainer,
                out FrameworkElement? newItemContainer);

            if (res && oldItemContainer != null && newItemContainer != null)
            {
                PART_ContentCanvasContainer.Children.Add(newItemContainer);

                PART_ContentCanvasContainer.Children.Remove(oldItemContainer);
            }
            else if (res &&
                oldItemContainer == null &&
                newItemContainer == null)
            {

            }
            else if (res && newItemContainer != null)
            {
                PART_ContentCanvasContainer.Children.Add(newItemContainer);
            }

            InvalidateMeasure();
        }
        private void RemoveFrame(int removedIndex)
        {
            var res = _viewCacheManager.RemoveFrame(removedIndex,
                0,
                out var oldItemContainer,
                out var newItemContainer);

            if (res && oldItemContainer != null)
            {
                PART_ContentCanvasContainer.Children.Remove(oldItemContainer);

                if (newItemContainer != null)
                {
                    PART_ContentCanvasContainer.Children.Add(newItemContainer);
                }

                InvalidateMeasure();

            }
        }
        #endregion

        #region Measure and Arrange
        protected override Size MeasureOverride(Size constraint)
        {
            try
            {
                MeasureDesiredItemSize();
                _viewportHeight = constraint.Height;
                _viewportWidth = constraint.Width;
                _extentHeight = (_itemsSourceAsList?.Count ?? 0) * _desiredItemSize.Height;

                // Update ScrollViewer info
                ScrollOwner?.InvalidateScrollInfo();

                // Determine visible range
                var itemSizeHeight = _desiredItemSize.Height;
                if (_desiredItemSize == Size.Empty)
                {
                    itemSizeHeight = DEFAULT_ITEM_SIZE_HEIGHT;
                }
                int firstIndex = (int)(VerticalOffset / itemSizeHeight);
                int lastIndex = (_itemsSourceAsList == null || _itemsSourceAsList.Count == 0)
                    ? 0 : Math.Min(_itemsSourceAsList.Count - 1, (int)((VerticalOffset + ViewportHeight) / itemSizeHeight));
                _viewCacheManager.SetUpVisibleSection(firstIndex, lastIndex,
                    out List<ViewCache> addedList,
                    out List<ViewCache> removedList);

                foreach (var cache in addedList)
                {
                    PART_ContentCanvasContainer.Children.Add(cache.View);
                }
                foreach (var cache in removedList)
                {
                    PART_ContentCanvasContainer.Children.Remove(cache.View);
                }
            }
            catch (Exception ex)
            {
                if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                {
                    string errorDetails = $"XAML Designer Error: {ex.Message}\n" +
                         $"StackTrace: {ex.StackTrace}\n" +
                         (ex.InnerException != null ? $"InnerException: {ex.InnerException.Message}\nStackTrace: {ex.InnerException.StackTrace}" : "");
                    throw new Exception(errorDetails);
                }
            }

            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            try
            {
                Canvas.SetLeft(PART_ContentCanvasContainer, -HorizontalOffset);
                Canvas.SetTop(PART_ContentCanvasContainer, -VerticalOffset);

                _viewCacheManager.ArrangeViewCache(arrangeSize, _desiredItemSize, 0, HorizontalOffset, 0);
            }
            catch (Exception ex)
            {
                if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                {
                    string errorDetails = $"XAML Designer Error: {ex.Message}\n" +
                         $"StackTrace: {ex.StackTrace}\n" +
                         (ex.InnerException != null ? $"InnerException: {ex.InnerException.Message}\nStackTrace: {ex.InnerException.StackTrace}" : "");
                    throw new Exception(errorDetails);
                }
            }

            return base.ArrangeOverride(arrangeSize);
        }
        #endregion

        #region Logical and Visual Tree
        //protected override int VisualChildrenCount => _viewCache.Count;
        //protected override Visual GetVisualChild(int index) => _viewCache[index];
        #endregion

        protected void MeasureDesiredItemSize(bool forceMeasure = false)
        {
            // Khi trong item source không có item, thì không nên measure item size
            if (!forceMeasure && _desiredItemSize != Size.Empty
               || _itemsSourceAsList == null
               || _itemsSourceAsList.Count == 0)
            {
                return;
            }
            var itemForMeasure = CreateItemView();
            itemForMeasure.DataContext = _itemsSourceAsList[0];
            itemForMeasure.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _desiredItemSize = itemForMeasure.DesiredSize;
            _viewCacheManager.SetDesiredItemContainerSize(_desiredItemSize);
        }
    }

    internal class ViewCacheManagement
    {
        public ViewCacheManagement(IVirtualizingPanel panelOwner)
        {
            this.panelOwner = panelOwner;
        }

        public int RealInitialVisibleItemIndex { get; protected set; }

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
            public FrameworkElement View { get; set; }

            // Vị trí hiển thị thực tế của view khi hiển thị trên màn hình
            public int Index { get; set; }
            public ViewCache(FrameworkElement view, int index)
            {
                View = view;
                Index = index;
            }

            public Rect ContentCanvasPosition { get; set; }
            public Rect MainPanelPosition { get; set; }
        }

        protected IVirtualizingPanel panelOwner;
        protected System.Collections.IList? itemSourceCache;
        protected Collection<ViewCache> viewCaches = new Collection<ViewCache>();
        protected Dictionary<UIElement, ViewCache> viewCacheMap = new Dictionary<UIElement, ViewCache>();
        protected Dictionary<int, ViewCache> viewCacheIndexMap = new Dictionary<int, ViewCache>();
        protected int realLastVisibleItemIndex;
        protected int expectedLastVisibleItemIndex { get; set; }
        protected Size desiredItemContainerSize { get; set; } = Size.Empty;

        public void SetUpVisibleSection(int newStartVisibleItemIndex,
            int newEndVisibleItemIndex,
            out List<ViewCache> addedCaches,
            out List<ViewCache> removedCaches)
        {
            var logTraceForDebug = "SetUpVisibleSection: Trace:";
            addedCaches = new List<ViewCache>();
            removedCaches = new List<ViewCache>();
            if (itemSourceCache == null)
            {
                return;
            }
            expectedLastVisibleItemIndex = newEndVisibleItemIndex;
            var visibleItemCount = VisibleItemCount;
            var newVisibleItemCount = newEndVisibleItemIndex - newStartVisibleItemIndex + 1;
            if (newEndVisibleItemIndex > itemSourceCache.Count - 1)
            {
                logTraceForDebug += "1->";
                newEndVisibleItemIndex = itemSourceCache.Count - 1;
                newStartVisibleItemIndex = newEndVisibleItemIndex - newVisibleItemCount + 1;
                newStartVisibleItemIndex = newStartVisibleItemIndex >= 0 ? newStartVisibleItemIndex : 0;
            }

            if (newVisibleItemCount > itemSourceCache.Count)
            {
                logTraceForDebug += "2->";
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
                logTraceForDebug += "3->";

                for (int i = 0; i < newVisibleItemCount - visibleItemCount; i++)
                {
                    var lastItemIndex = LastVisibleItemIndex;

                    if (itemSourceCache.Count > 0
                        && newStartVisibleItemIndex < RealInitialVisibleItemIndex)
                    {
                        logTraceForDebug += "4->";
                        // Add item from the left
                        var firstItemIndex = FirstVisibleItemIndex;
                        var newFrame = GenerateItemContainer(firstItemIndex - 1);
                        var newViewCache = new ViewCache(newFrame, firstItemIndex - 1);
                        InsertNewCacheToFront(newViewCache);
                        addedCaches.Add(newViewCache);
                    }
                    else
                    {
                        logTraceForDebug += "5->";
                        // Add new item container
                        var newFrame = GenerateItemContainer(lastItemIndex + 1);
                        var newViewCache = new ViewCache(newFrame, lastItemIndex + 1);
                        AddNewCacheToBack(newViewCache);
                        addedCaches.Add(newViewCache);
                    }
                }
            }
            else if (newVisibleItemCount < visibleItemCount)
            {
                logTraceForDebug += "6->";
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
            //AssertForDebug();
#endif

            //Switch container, and assign viewmodel to item container
            if (newStartVisibleItemIndex > RealInitialVisibleItemIndex)
            {
                logTraceForDebug += "7->";

                var numberItemToBring = newStartVisibleItemIndex - RealInitialVisibleItemIndex;
                var lastItemIndex = LastVisibleItemIndex;

                // Trong trường hợp click thẳng trên thanh scroll thì lượng numberItemToBring có thể quá lớn dẫn tới
                // performance issue
                if (numberItemToBring > viewCaches.Count)
                {
                    logTraceForDebug += "8->";
                    viewCacheIndexMap.Clear();
                    for (int i = 0; i < newEndVisibleItemIndex - newStartVisibleItemIndex + 1; i++)
                    {
                        viewCaches[i].Index = newStartVisibleItemIndex + i;
                        viewCaches[i].View.DataContext = itemSourceCache?[newStartVisibleItemIndex + i];
                        if (viewCaches[i].View is ContentPresenter c)
                        {
                            c.Content = itemSourceCache?[newStartVisibleItemIndex + i];
                        }
                        viewCacheIndexMap.Add(viewCaches[i].Index, viewCaches[i]);
                    }
                }
                else if (numberItemToBring <= viewCaches.Count)
                {
                    logTraceForDebug += "9->";
                    for (int i = 0; i < numberItemToBring; i++)
                    {
                        var oldElement = viewCaches[0];
                        viewCaches.RemoveAt(0);
                        viewCacheIndexMap.Remove(oldElement.Index);
                        oldElement.Index = lastItemIndex + 1 + i;
                        if (oldElement.Index < itemSourceCache?.Count)
                        {
                            oldElement.View.DataContext = itemSourceCache[oldElement.Index];
                            if (oldElement.View is ContentPresenter c)
                            {
                                c.Content = itemSourceCache[oldElement.Index];
                            }
                        }
                        viewCaches.Add(oldElement);
                        viewCacheIndexMap.Add(oldElement.Index, oldElement);
                    }
                }
            }
            else if (newEndVisibleItemIndex < realLastVisibleItemIndex)
            {
                logTraceForDebug += "10->";
                var numberItemToBring = RealInitialVisibleItemIndex - newStartVisibleItemIndex;
                var firstItemIndex = FirstVisibleItemIndex;

                // Trong trường hợp click thẳng trên thanh scroll thì lượng numberItemToBring có thể quá lớn dẫn tới
                // performance issue
                if (numberItemToBring > viewCaches.Count)
                {
                    logTraceForDebug += "11->";
                    viewCacheIndexMap.Clear();
                    for (int i = 0; i < newEndVisibleItemIndex - newStartVisibleItemIndex + 1; i++)
                    {
                        viewCaches[i].Index = newStartVisibleItemIndex + i;
                        viewCaches[i].View.DataContext = itemSourceCache?[newStartVisibleItemIndex + i];
                        if (viewCaches[i].View is ContentPresenter c)
                        {
                            c.Content = itemSourceCache?[newStartVisibleItemIndex + i];
                        }
                        viewCacheIndexMap.Add(viewCaches[i].Index, viewCaches[i]);
                    }
                }
                else if (numberItemToBring <= viewCaches.Count)
                {
                    logTraceForDebug += "12->";

                    if (newVisibleItemCount > visibleItemCount)
                    {
                        // Vì trước khi vào đoạn if này
                        // 1 item đã được thêm mới vào phía trước [trong đoạn >> Debug.WriteLine("4.");]
                        // nên chỉ cần di chuyển số lượng item bằng
                        // newStartVisibleItemIndex - RealInitialVisibleItemIndex - 1
                        // Tức là không cần di chuyển item vừa thêm vào đó
                        numberItemToBring = numberItemToBring - 1;
                        logTraceForDebug += "13->";
                    }

                    for (int i = 0; i < numberItemToBring; i++)
                    {
                        var lastIndex = viewCaches.Count - 1;
                        var oldElement = viewCaches[lastIndex];
                        viewCaches.RemoveAt(lastIndex);
                        viewCacheIndexMap.Remove(oldElement.Index);

                        oldElement.Index = firstItemIndex - 1 - i;

                        oldElement.View.DataContext = itemSourceCache?[oldElement.Index];
                        if (oldElement.View is ContentPresenter c)
                        {
                            c.Content = itemSourceCache?[oldElement.Index];
                        }

                        viewCaches.Insert(0, oldElement);
                        viewCacheIndexMap.Add(oldElement.Index, oldElement);
                    }

                }

            }

#if DEBUG
            AssertForDebug(RealInitialVisibleItemIndex, realLastVisibleItemIndex, newStartVisibleItemIndex, newEndVisibleItemIndex);
            PrintDebugLog(logTraceForDebug, newStartVisibleItemIndex, newEndVisibleItemIndex);
#endif
            //Apply to cache
            RealInitialVisibleItemIndex = newStartVisibleItemIndex;
            realLastVisibleItemIndex = newEndVisibleItemIndex;
        }

        private void PrintDebugLog(string logTrace, int newStartVisibleItemIndex, int newEndVisibleItemIndex)
        {
            var log = "";
            foreach (var item in viewCaches)
            {
                log += $"{item.View.DataContext};";
            }
            log = $"[TRACE] {logTrace}; [DISPLAY] oldIndex={RealInitialVisibleItemIndex}-{realLastVisibleItemIndex};newIndex={newStartVisibleItemIndex}-{newEndVisibleItemIndex}; [ITEM-ARRAY] " + log;
            Debug.WriteLine(log);
        }

        public void SetupItemSource(System.Collections.IList? itemSource)
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

        public void ArrangeViewCache(Size arrangeSize,
            Size desiredItemContainerSize,
            double frameDistance,
            double horizontalOffset,
            double containerMarginLeft)
        {
            for (int i = 0; i < viewCaches.Count; i++)
            {
                var frame = viewCaches[i].View;
                var left = viewCaches[i].Index *
                    (desiredItemContainerSize.Width + frameDistance);
                var top = viewCaches[i].Index * (desiredItemContainerSize.Height + frameDistance);
                Canvas.SetLeft(frame, left);
                Canvas.SetTop(frame, top);

                viewCaches[i].ContentCanvasPosition = new Rect(left + containerMarginLeft,
                    top,
                    desiredItemContainerSize.Width,
                    desiredItemContainerSize.Height);
                viewCaches[i].MainPanelPosition = new Rect(left - horizontalOffset + containerMarginLeft,
                    (arrangeSize.Height - desiredItemContainerSize.Height) / 2,
                    desiredItemContainerSize.Width,
                    desiredItemContainerSize.Height);
            }
        }

        public ViewCache? this[UIElement frame]
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


        private void InsertNewCacheToFront(ViewCache newCache)
        {
            viewCaches.Insert(0, newCache);
            viewCacheMap.Add(newCache.View, newCache);
            viewCacheIndexMap.Add(newCache.Index, newCache);
        }

        private void AddNewCacheToBack(ViewCache newCache)
        {
            viewCaches.Add(newCache);
            viewCacheMap.Add(newCache.View, newCache);
            viewCacheIndexMap.Add(newCache.Index, newCache);
        }

        private ViewCache? RemoveLastCache()
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



        public void SetDesiredItemContainerSize(Size newSize)
        {
            desiredItemContainerSize = newSize;
            foreach (var v in viewCaches)
            {
                v.View.Height = newSize.Height;
                v.View.Width = newSize.Width;
            }
        }

        public void AssertForDebug(int oldStartIndex, int oldEndIndex, int newStartIndex, int newEndIndex)
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
                if (viewCaches[i].Index != newStartIndex + i)
                {
                    Debug.WriteLine("Assertion failed, but continuing execution.");
                }
                if (viewCaches[i + 1].Index != newStartIndex + i + 1)
                {
                    Debug.WriteLine("Assertion failed, but continuing execution.");
                }
            }
            foreach (var kp in viewCacheIndexMap)
            {
                Debug.Assert(kp.Key == kp.Value.Index);

                // đảm bảo index trong viewCache tương ứng với index của item trên item source
                Debug.Assert(kp.Value.View.DataContext == itemSourceCache![kp.Value.Index]);
            }

            // Đảm bảo map và cache size luôn bằng nhau
            Debug.Assert(viewCacheMap.Count == viewCaches.Count);
        }

        protected FrameworkElement GenerateItemContainer(int viewModelIdex)
        {
            var newItem = panelOwner.CreateItemView();
            if (itemSourceCache != null)
            {
                newItem.DataContext = itemSourceCache[viewModelIdex];
                if (newItem is ContentPresenter c)
                {
                    c.Content = itemSourceCache[viewModelIdex];
                }
            }
            return newItem;
        }

        protected FrameworkElement GenerateItemContainer(object dataContext)
        {
            Debug.Assert(!desiredItemContainerSize.IsEmpty);
            var newFrame = panelOwner.CreateItemView();
#if DEBUG
            newFrame.Tag = "false";
#endif
            newFrame.DataContext = dataContext;
            if (newFrame is ContentPresenter c)
            {
                c.Content = dataContext;
            }
            return newFrame;
        }


        public bool InsertFrame(int insertedIndex,
            object newItem,
            double frameDistance,
            out FrameworkElement? oldItemContainer,
            out FrameworkElement? newItemContainer)
        {
            viewCacheIndexMap.TryGetValue(insertedIndex, out ViewCache? oldViewCache);
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
                for (int i = 0; i < viewCaches.Count; i++)
                {
                    var viewCache = viewCaches[i];
                    viewCacheIndexMap.Remove(viewCache.Index);
                    viewCache.Index += 1;
                    ArrangeItemContainer(viewCache, frameDistance);
                }
                RealInitialVisibleItemIndex += 1;
                realLastVisibleItemIndex += 1;

                for (int i = 0; i < viewCaches.Count; i++)
                    viewCacheIndexMap.Add(viewCaches[i].Index, viewCaches[i]);
            }
            else if (insertedIndex >= RealInitialVisibleItemIndex && insertedIndex <= realLastVisibleItemIndex)
            {
                var lastItemContainerIndex = viewCaches.Last().Index;

                for (int i = 0; i < lastItemContainerIndex - insertedIndex + 1; i++)
                {
                    var viewCache = viewCacheIndexMap[i + insertedIndex];
                    viewCacheIndexMap.Remove(i + insertedIndex);
                    viewCache.Index += 1;
                    ArrangeItemContainer(viewCache, frameDistance);

                }

                var newItemViewCache = new ViewCache(GenerateItemContainer(newItem), insertedIndex);
                newItemViewCache.ContentCanvasPosition = oldViewCache?.ContentCanvasPosition ?? new Rect();
                newItemViewCache.MainPanelPosition = oldViewCache?.MainPanelPosition ?? new Rect();
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
            else if (insertedIndex > realLastVisibleItemIndex
                && (insertedIndex <= expectedLastVisibleItemIndex || insertedIndex == realLastVisibleItemIndex + 1))
            {
                var newItemViewCache = new ViewCache(GenerateItemContainer(newItem), insertedIndex);
                ArrangeItemContainer(newItemViewCache, frameDistance);

                viewCaches.Add(newItemViewCache);
                viewCacheMap.Add(newItemViewCache.View, newItemViewCache);
                viewCacheIndexMap.Add(insertedIndex, newItemViewCache);
                newItemContainer = newItemViewCache.View;
            }

            return true;
        }

        public bool RemoveFrame(int removeIndex,
          double frameDistance,
          out FrameworkElement? oldItemContainer,
          out FrameworkElement? newItemContainer)
        {
            viewCacheIndexMap.TryGetValue(removeIndex, out ViewCache? oldViewCache);
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
                for (int i = 0; i < viewCaches.Count; i++)
                {
                    var viewCache = viewCaches[i];
                    viewCacheIndexMap.Remove(viewCache.Index);
                    viewCache.Index -= 1;
                    ArrangeItemContainer(viewCache, frameDistance);
                }
                RealInitialVisibleItemIndex -= 1;
                realLastVisibleItemIndex -= 1;

                for (int i = 0; i < viewCaches.Count; i++)
                    viewCacheIndexMap.Add(viewCaches[i].Index, viewCaches[i]);
            }
            else
            {
                var lastItemContainerIndex = viewCaches.Last().Index;

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
                    ArrangeItemContainer(viewCache, frameDistance);
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

        protected void ArrangeItemContainer(ViewCache view,
            double frameDistance)
        {
            var toLeft = view.Index *
                   (desiredItemContainerSize.Width + frameDistance);
            var toTop = view.Index *
                 (desiredItemContainerSize.Height + frameDistance);
            Canvas.SetLeft(view.View, toLeft);
            Canvas.SetTop(view.View, toTop);
        }
    }

    internal class ViewCacheManagementWithAnimation : ViewCacheManagement
    {
        public ViewCacheManagementWithAnimation(IVirtualizingPanel panelOwner) : base(panelOwner)
        {
        }

        public bool InsertFrameWithAnimation(int insertedIndex,
            object newItem,
            double frameDistance,
            out DoubleAnimation?[]? insertFrameAnimations,
            out FrameworkElement? oldItemContainer,
            out FrameworkElement? newItemContainer)
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

                var newItemViewCache = new ViewCache(GenerateItemContainer(newItem), insertedIndex);
                newItemViewCache.ContentCanvasPosition = oldViewCache?.ContentCanvasPosition ?? new Rect();
                newItemViewCache.MainPanelPosition = oldViewCache?.MainPanelPosition ?? new Rect();
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
                var newItemViewCache = new ViewCache(GenerateItemContainer(newItem), insertedIndex);
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
          out FrameworkElement? oldItemContainer, out FrameworkElement? newItemContainer)
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
    }
}
