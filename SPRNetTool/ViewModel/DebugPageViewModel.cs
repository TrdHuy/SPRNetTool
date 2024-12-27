using ArtWiz.Domain;
using ArtWiz.Domain.Base;
using ArtWiz.Domain.Utils;
using ArtWiz.Utils;
using ArtWiz.ViewModel.Base;
using ArtWiz.ViewModel.CommandVM;
using ArtWiz.ViewModel.Widgets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static ArtWiz.Domain.BitmapDisplayMangerChangedArg.ChangedEvent;
using static ArtWiz.Domain.SprFrameCollectionChangedArg.ChangedEvent;

namespace ArtWiz.ViewModel
{
    public class DebugPageViewModel : BaseParentsViewModel, IDebugPageCommand
    {
        private BitmapSource? _currentDisplayedBitmapSource;
        private BitmapSource? _currentDisplayedOptimizedBitmapSource;
        private ObservableCollection<ColorItemViewModel> _rawOriginalSource = new ObservableCollection<ColorItemViewModel>();
        private ObservableCollection<OptimizedColorItemViewModel>? _rawOptimizedSource = null;
        private ObservableCollection<ColorItemViewModel>? _rawResultRGBSource = null;

        private ObservableCollection<ColorItemViewModel> _orignalDisplayedColorSource = new ObservableCollection<ColorItemViewModel>();
        private ObservableCollection<OptimizedColorItemViewModel>? _optimizedDisplayedSource = null;
        private ObservableCollection<ColorItemViewModel>? _displayedRgbCompositeResultSource = null;
        private CustomObservableCollection<IFramePreviewerViewModel>? _framesSource;

        private bool _isPlayingAnimation = false;
        private int _pixelWidth = 0;
        private int _pixelHeight = 0;
        private bool _isSpr = false;
        private IFileHeadEditorViewModel fileHeadEditorVM;
        private IBitmapViewerViewModel bitmapViewerVM;
        private IPaletteEditorViewModel paletteEditorVM;

        [Bindable(true)]
        public IFileHeadEditorViewModel FileHeadEditorVM
        {
            get => fileHeadEditorVM;
        }

        [Bindable(true)]
        public IBitmapViewerViewModel BitmapViewerVM
        {
            get => bitmapViewerVM;
        }

        [Bindable(true)]
        public IPaletteEditorViewModel PaletteEditorVM
        {
            get => paletteEditorVM;
        }

        [Bindable(true)]
        public CustomObservableCollection<IFramePreviewerViewModel>? FramesSource
        {
            get
            {
                return _framesSource;
            }
            set
            {
                if (_framesSource != value)
                {
                    _framesSource = value;
                    Invalidate();
                }
            }
        }

        [Bindable(true)]
        public bool IsSpr
        {
            get
            {
                return _isSpr;
            }
            set
            {
                if (_isSpr != value)
                {
                    _isSpr = value;
                    Invalidate();
                }
            }
        }

        [Bindable(true)]
        public int PixelWidth
        {
            get
            {
                return _pixelWidth;
            }
            set
            {
                if (_pixelWidth != value)
                {
                    _pixelWidth = value;
                    Invalidate();
                }
            }
        }
        [Bindable(true)]
        public int PixelHeight
        {
            get
            {
                return _pixelHeight;
            }
            set
            {
                if (_pixelHeight != value)
                {
                    _pixelHeight = value;
                    Invalidate();
                }
            }
        }

        [Bindable(true)]
        public bool IsPlayingAnimation
        {
            get
            {
                return _isPlayingAnimation;
            }
            set
            {
                if (_isPlayingAnimation != value)
                {
                    _isPlayingAnimation = value;
                    Invalidate();
                }

            }
        }

        [Bindable(true)]
        public ObservableCollection<ColorItemViewModel> OriginalColorSource
        {
            get { return _orignalDisplayedColorSource; }
            private set
            {
                _orignalDisplayedColorSource = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public ObservableCollection<ColorItemViewModel>? ResultRGBSource
        {
            get { return _displayedRgbCompositeResultSource; }
            private set
            {
                _displayedRgbCompositeResultSource = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public ObservableCollection<OptimizedColorItemViewModel>? OptimizedColorSource
        {
            get { return _optimizedDisplayedSource; }
            private set
            {
                _optimizedDisplayedSource = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public BitmapSource? CurrentlyDisplayedBitmapSource
        {

            get { return _currentDisplayedBitmapSource; }
            set
            {
                _currentDisplayedBitmapSource = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public BitmapSource? CurrentlyDisplayedOptimizedBitmapSource
        {

            get { return _currentDisplayedOptimizedBitmapSource; }
            set
            {
                _currentDisplayedOptimizedBitmapSource = value;
                Invalidate();
            }
        }



        public DebugPageViewModel()
        {
            paletteEditorVM = new PaletteEditorViewModel(this);
            bitmapViewerVM = new BitmapViewerViewModel(this);
            fileHeadEditorVM = new FileHeadEditorViewModel(this);
            BindingOperations.EnableCollectionSynchronization(_rawOriginalSource, new object());
            BitmapDisplayManager.RegisterObserver(this);
        }

        ~DebugPageViewModel()
        {
            BitmapDisplayManager.UnregisterObserver(this);
        }

        public void ResetViewModel()
        {
            _rawOriginalSource.Clear();
            _orignalDisplayedColorSource.Clear();
            _rawResultRGBSource = null;
            _displayedRgbCompositeResultSource = null;
            _rawOptimizedSource = null;
            _cachedOptimizedOrderByCombinedRGB = null;
            _cachedOptimizedOrderByRGB = null;
            _cachedOrderByCount = null;
            _cachedOrderByDescendingCount = null;
            _cachedOrderByRGB = null;
            _currentDisplayedBitmapSource = null;
            InvalidateAll();
        }

        #region OriginalSource
        public async Task SetColorSource(Dictionary<Color, long>? colorsSource)
        {
            _countableColorSource = colorsSource;
            _cachedOrderByCount = null;
            _cachedOrderByDescendingCount = null;
            _cachedOrderByRGB = null;
            _rawOriginalSource.Clear();

            if (colorsSource != null)
            {
                await Task.Run(() =>
                {
                    foreach (var color in colorsSource)
                    {
                        var newColor = color.Key;
                        _rawOriginalSource.Add<ColorItemViewModel>(new ColorItemViewModel { ItemColor = newColor, Count = color.Value });
                    }
                });
            }
            OriginalColorSource = _rawOriginalSource;
        }

        public void ResetOrder()
        {
            OriginalColorSource = _rawOriginalSource;
        }

        private ObservableCollection<ColorItemViewModel>? _cachedOrderByCount = null;
        private ObservableCollection<ColorItemViewModel>? _cachedOrderByDescendingCount = null;
        public void OrderByCount(bool isSetToDisplaySource = true)
        {
            if (_cachedOrderByCount == null)
                _cachedOrderByCount = _rawOriginalSource.OrderBy(x => x.Count).ToIndexableObservableCollection();

            if (isSetToDisplaySource)
                OriginalColorSource = _cachedOrderByCount;
        }
        public ObservableCollection<ColorItemViewModel> OrderByDescendingCount(bool isSetToDisplaySource = true)
        {
            if (_cachedOrderByDescendingCount == null)
                _cachedOrderByDescendingCount = _rawOriginalSource.OrderByDescending(x => x.Count).ToIndexableObservableCollection();

            if (isSetToDisplaySource)
                OriginalColorSource = _cachedOrderByDescendingCount;

            return _cachedOrderByDescendingCount;
        }

        private ObservableCollection<ColorItemViewModel>? _cachedOrderByRGB = null;
        public void OrderByRGB()
        {
            if (_cachedOrderByRGB == null)
                _cachedOrderByRGB = _rawOriginalSource.OrderBy(x => x.RGBValue).ToIndexableObservableCollection();
            OriginalColorSource = _cachedOrderByRGB;
        }
        #endregion

        #region OptimizedColorSource 
        public void SetOptimizedColorSource(ObservableCollection<OptimizedColorItemViewModel> optimizedSource)
        {
            _cachedOptimizedOrderByRGB = null;
            _cachedOptimizedOrderByCombinedRGB = null;
            _cachedOptimizedOrderByARGB = null;
            _rawOptimizedSource = optimizedSource;
            OptimizedColorSource = optimizedSource;
        }

        public void ResetOptimizedOrder()
        {
            _rawOptimizedSource?.ReIndexObservableCollection();
            OptimizedColorSource = _rawOptimizedSource;
        }
        private ObservableCollection<OptimizedColorItemViewModel>? _cachedOptimizedOrderByRGB = null;
        public void OptimizedOrderByRGB()
        {
            if (_cachedOptimizedOrderByRGB == null)
                _cachedOptimizedOrderByRGB = _rawOptimizedSource?.OrderBy(x => x.RGBValue).ToIndexableObservableCollection();
            _cachedOptimizedOrderByRGB?.ReIndexObservableCollection();
            OptimizedColorSource = _cachedOptimizedOrderByRGB;
        }

        private ObservableCollection<OptimizedColorItemViewModel>? _cachedOptimizedOrderByARGB = null;
        public void OptimizedOrderByARGB()
        {
            if (_cachedOptimizedOrderByARGB == null)
                _cachedOptimizedOrderByARGB = _rawOptimizedSource?.OrderBy(x => x.RGBAValue).ToIndexableObservableCollection();
            _cachedOptimizedOrderByARGB?.ReIndexObservableCollection();
            OptimizedColorSource = _cachedOptimizedOrderByARGB;
        }

        private ObservableCollection<OptimizedColorItemViewModel>? _cachedOptimizedOrderByCombinedRGB = null;
        public void OptimizedOrderByCombinedRGB()
        {
            if (_cachedOptimizedOrderByCombinedRGB == null)
                _cachedOptimizedOrderByCombinedRGB = _rawOptimizedSource?.OrderBy(x => x.CombinedRGBValue).ToIndexableObservableCollection();
            _cachedOptimizedOrderByCombinedRGB?.ReIndexObservableCollection();
            OptimizedColorSource = _cachedOptimizedOrderByCombinedRGB;
        }

        #endregion

        #region ResultRGBSource
        public void SetResultRGBColorSource(ObservableCollection<ColorItemViewModel> resultRGBColorSource)
        {
            _rawResultRGBSource = resultRGBColorSource;
            ResultRGBSource = resultRGBColorSource;
        }

        public void ResetResultRGBColorSourceOrder()
        {
            ResultRGBSource = _rawResultRGBSource;
        }
        #endregion

        protected override void OnDomainChanged(IDomainChangedArgs args)
        {
            if (IsViewModelDestroyed) return;

            switch (args)
            {
                case BitmapDisplayMangerChangedArg castArgs:

                    if (castArgs.Event.HasFlag(SPR_WORKSPACE_RESET))
                    {
                        FramesSource = null;
                        OriginalColorSource.Clear();
                    }
                    else if (castArgs.Event.HasFlag(IS_PLAYING_ANIMATION_CHANGED))
                    {
                        if (castArgs.IsPlayingAnimation == true)
                        {
                            var dispatcherPriority = DispatcherPriority.Background;
                            if (FileHeadEditorVM.FileHead.Interval > 20)
                            {
                                dispatcherPriority = DispatcherPriority.Render;
                            }

                            if (IsViewModelDestroyed) return;

                            ViewModelOwner?.ViewDispatcher.Invoke(() =>
                            {
                                IsPlayingAnimation = true;
                                CurrentlyDisplayedBitmapSource = castArgs.CurrentDisplayingSource;
                                if (IsSpr &&
                                    FramesSource != null &&
                                    FramesSource.Count > 0)
                                {
                                    FramesSource[(int)BitmapDisplayManager.GetCurrentDisplayFrameIndex()]
                                        .PreviewImageSource = castArgs.CurrentDisplayingSource;
                                }

                            }, dispatcherPriority);
                        }
                        else if (castArgs.IsPlayingAnimation == false)
                        {
                            IsPlayingAnimation = false;
                            CurrentlyDisplayedBitmapSource = castArgs.CurrentDisplayingSource;
                            if (IsSpr &&
                                FramesSource != null &&
                                FramesSource.Count > 0)
                            {
                                FramesSource[(int)BitmapDisplayManager.GetCurrentDisplayFrameIndex()]
                                    .PreviewImageSource = castArgs.CurrentDisplayingSource;
                            }
                        }
                    }
                    else
                    {
                        if (castArgs.Event.HasFlag(SPR_FILE_HEAD_CHANGED))
                        {
                            IsSpr = castArgs.CurrentSprFileHead != null;
                        }

                        if (castArgs.Event.HasFlag(CURRENT_DISPLAYING_SOURCE_CHANGED))
                        {
                            CurrentlyDisplayedBitmapSource = castArgs.CurrentDisplayingSource;
                            PixelWidth = castArgs.CurrentDisplayingSource?.PixelWidth ?? 0;
                            PixelHeight = castArgs.CurrentDisplayingSource?.PixelHeight ?? 0;
                            if (castArgs.Event.HasFlag(CURRENT_DISPLAYING_SOURCE_CHANGED) &&
                                IsSpr &&
                                FramesSource != null &&
                                FramesSource.Count > 0 &&
                                BitmapDisplayManager.GetCurrentDisplayFrameIndex() < FramesSource.Count)
                            {
                                FramesSource[(int)BitmapDisplayManager.GetCurrentDisplayFrameIndex()]
                                    .PreviewImageSource = castArgs.CurrentDisplayingSource;
                            }
                        }

                        if (castArgs.Event.HasFlag(SPR_FRAME_COLLECTION_CHANGED))
                        {
                            var collectionChangedArg = castArgs.SprFrameCollectionChangedArg;
                            if (collectionChangedArg == null)
                            {
                                break;
                            }

                            if (collectionChangedArg.Event.HasFlag(TOTAL_FRAME_COUNT_CHANGED) &&
                                castArgs.Event.HasFlag(SPR_FRAME_DATA_CHANGED) &&
                                IsSpr)
                            {
                                var newSrc = new CustomObservableCollection<IFramePreviewerViewModel>();
                                var head = castArgs.CurrentSprFileHead!;
                                for (int i = 0; i < collectionChangedArg.FrameCount; i++)
                                {
                                    newSrc.Add(new FrameViewModel(this)
                                    {
                                        GlobalHeight = castArgs.CurrentSprFileHead?.GlobalHeight ?? 0,
                                        GlobalWidth = castArgs.CurrentSprFileHead?.GlobalWidth ?? 0,
                                        GlobalOffsetX = castArgs.CurrentSprFileHead?.OffX ?? 0,
                                        GlobalOffsetY = castArgs.CurrentSprFileHead?.OffY ?? 0,
                                        FrameHeight = castArgs.SprFrameData?.frameHeight ?? 0,
                                        FrameWidth = castArgs.SprFrameData?.frameWidth ?? 0,
                                        FrameOffsetY = castArgs.SprFrameData?.frameOffY ?? 0,
                                        FrameOffsetX = castArgs.SprFrameData?.frameOffX ?? 0,
                                    }); ;
                                }
                                FramesSource = newSrc;
                                if (castArgs.Event.HasFlag(CURRENT_DISPLAYING_SOURCE_CHANGED))
                                {
                                    FramesSource[(int)BitmapDisplayManager.GetCurrentDisplayFrameIndex()]
                                        .PreviewImageSource = castArgs.CurrentDisplayingSource;
                                }
                            }

                            if (collectionChangedArg.Event.HasFlag(FRAME_SWITCHED) && FramesSource != null)
                            {
                                FramesSource.SwitchItem((int)collectionChangedArg.FrameSwitched1Index, (int)collectionChangedArg.FrameSwitched2Index);
                            }

                            if (collectionChangedArg.Event.HasFlag(FRAME_REMOVED) && FramesSource != null)
                            {
                                FramesSource[collectionChangedArg.OldFrameIndex].OnArtWizViewModelDestroy();
                                FramesSource.RemoveAt(collectionChangedArg.OldFrameIndex);
                            }

                            if (collectionChangedArg.Event.HasFlag(FRAME_INSERTED))
                            {
                                if (FramesSource != null)
                                {
                                    ViewModelOwner?.ViewDispatcher.Invoke(() =>
                                    {
                                        var newFrameVM = new FrameViewModel(this);
                                        if (castArgs.Event.HasAllFlagsOf(CURRENT_DISPLAYING_SOURCE_CHANGED,
                                            SPR_FILE_HEAD_CHANGED,
                                            SPR_FRAME_DATA_CHANGED))
                                        {
                                            newFrameVM.PreviewImageSource = castArgs.CurrentDisplayingSource;
                                            newFrameVM.GlobalHeight = castArgs.CurrentSprFileHead?.GlobalHeight ?? 0;
                                            newFrameVM.GlobalWidth = castArgs.CurrentSprFileHead?.GlobalWidth ?? 0;
                                            newFrameVM.GlobalOffsetX = castArgs.CurrentSprFileHead?.OffX ?? 0;
                                            newFrameVM.GlobalOffsetY = castArgs.CurrentSprFileHead?.OffY ?? 0;
                                            newFrameVM.FrameOffsetX = castArgs.SprFrameData?.frameOffX ?? 0;
                                            newFrameVM.FrameOffsetY = castArgs.SprFrameData?.frameOffY ?? 0;
                                            newFrameVM.FrameHeight = castArgs.SprFrameData?.frameHeight ?? 0;
                                            newFrameVM.FrameWidth = castArgs.SprFrameData?.frameWidth ?? 0;
                                        }
                                        FramesSource.Insert(collectionChangedArg.NewFrameIndex, newFrameVM);
                                    });
                                }
                                else
                                {
                                    FramesSource = new CustomObservableCollection<IFramePreviewerViewModel>();
                                    FramesSource.Insert(collectionChangedArg.NewFrameIndex, new FrameViewModel(this));
                                }
                            }

                        }
                    }
                    break;
            }
        }

        public Dictionary<Color, long>? _countableColorSource;

        public void OptimizeImageColor(int colorSize
            , int colorDifferenceDelta
            , bool isUsingAlpha
            , int colorDifferenceDeltaForCalculatingAlpha
            , Color backgroundForBlendColor)
        {
            (_countableColorSource!, CurrentlyDisplayedBitmapSource!).ApplyIfNotNull((colorSource, bitmapSource) =>
            {
                var scr = colorSource.Select(kp => (kp.Key, kp.Value)).ToList();
                var optimizedBitmap = BitmapDisplayManager.OptimzeImageColor(scr
                     , bitmapSource
                     , colorSize
                     , colorDifferenceDelta
                     , isUsingAlpha
                     , colorDifferenceDeltaForCalculatingAlpha
                     , backgroundForBlendColor
                     , out List<Color> selectedColors
                     , out List<Color> selectedAlphaColors
                     , out List<Color> expectedRGBColors);
                var selectedList = new ObservableCollection<OptimizedColorItemViewModel>();

                int i = 0;
                foreach (var color in selectedColors)
                {
                    if (i < colorSize)
                    {
                        selectedList.Add<OptimizedColorItemViewModel>(new OptimizedColorItemViewModel()
                        {
                            ItemColor = color
                        });
                    }
                    else
                    {
                        selectedList.Add<OptimizedColorItemViewModel>(new OptimizedColorItemViewModel()
                        {
                            ItemColor = selectedAlphaColors[i - colorSize],
                            ExpectedColor = expectedRGBColors[i - colorSize]
                        }); ;
                    }
                    i++;
                }
                SetOptimizedColorSource(selectedList);

                CurrentlyDisplayedOptimizedBitmapSource = optimizedBitmap?.Also(it =>
                {
                    BitmapDisplayManager.CountBitmapColors(it).Apply(it =>
                    {
                        var newCountedSrc = new ObservableCollection<ColorItemViewModel>();
                        foreach (var color in it)
                        {
                            var newColor = color.Key;
                            newCountedSrc.Add<ColorItemViewModel>(new ColorItemViewModel { ItemColor = newColor, Count = color.Value });
                        }
                        SetResultRGBColorSource(newCountedSrc);
                    });
                });
            });
        }


        #region Command region
        async Task IDebugPageCommand.OnOpenImageFromFileClickAsync(string filePath)
        {
            await Task.Run(() =>
            {
                BitmapDisplayManager.OpenBitmapFromFile(filePath);
            });

        }

        async Task IDebugPageCommand.OnReloadColorSourceClick()
        {
            if (bitmapViewerVM.FrameSource != null)
            {
                var colorSource = BitmapDisplayManager.CountColorsToDictionary(bitmapViewerVM.FrameSource);
                await SetColorSource(colorSource);
            }
            else
            {
                await SetColorSource(null);
            }
        }

        void IDebugPageCommand.OnResetSprWorkspaceClicked()
        {
            BitmapDisplayManager.ResetSprWorkSpace();
        }

        bool IDebugPageCommand.OnPlayPauseAnimationSprClicked()
        {
            if (!IsSpr) return false;

            if (!IsPlayingAnimation)
            {
                BitmapDisplayManager.StartSprAnimation();
            }
            else
            {
                BitmapDisplayManager.StopSprAnimation();
            }
            return FileHeadEditorVM.FileHead.FrameCounts > 1;
        }

        void IDebugPageCommand.OnSaveCurrentWorkManagerToFileSprClicked(string filePath)
        {
            if (!IsSpr) return;
            SprWorkManager.SaveCurrentWorkToSpr(filePath, true);
        }

        #region Change frame offset command
        private TaskPool ModifySizeAndOffsetTaskPool = new TaskPool(cores: 1);
        void IDebugPageCommand.OnIncreaseFrameWidthButtonClicked(uint delta)
        {
            if (!IsSpr) return;
            SetFrameSize((int)delta, 0);

        }

        void IDebugPageCommand.OnDecreaseFrameWidthButtonClicked(uint delta)
        {
            if (!IsSpr) return;
            SetFrameSize(-(int)delta, 0);
        }

        void IDebugPageCommand.OnIncreaseFrameHeightButtonClicked(uint delta)
        {
            if (!IsSpr) return;
            SetFrameSize(0, (int)delta);

        }

        void IDebugPageCommand.OnDecreaseFrameHeightButtonClicked(uint delta)
        {
            if (!IsSpr) return;
            SetFrameSize(0, -(int)delta);
        }

        void IDebugPageCommand.OnIncreaseFrameOffsetXButtonClicked(uint delta)
        {
            if (!IsSpr) return;
            SetFrameOffset((int)delta, 0);
        }

        void IDebugPageCommand.OnDecreaseFrameOffsetXButtonClicked(uint delta)
        {
            if (!IsSpr) return;
            SetFrameOffset(-(int)delta, 0);
        }
        void IDebugPageCommand.OnIncreaseFrameOffsetYButtonClicked(uint delta)
        {
            if (!IsSpr) return;

            SetFrameOffset(0, (int)delta);

        }

        void IDebugPageCommand.OnDecreaseFrameOffsetYButtonClicked(uint delta)
        {
            if (!IsSpr) return;

            SetFrameOffset(0, -(int)delta);
        }
        void IDebugPageCommand.OnIncreaseSprGlobalOffsetXButtonClicked(uint delta)
        {
            if (!IsSpr) return;

            SetGlobalOffset((int)delta, 0);
        }
        void IDebugPageCommand.OnDecreaseSprGlobalOffsetXButtonClicked(uint delta)
        {
            if (!IsSpr) return;

            SetGlobalOffset(-(int)delta, 0);
        }
        void IDebugPageCommand.OnIncreaseSprGlobalOffsetYButtonClicked(uint delta)
        {
            if (!IsSpr) return;

            SetGlobalOffset(0, (int)delta);
        }
        void IDebugPageCommand.OnDecreaseSprGlobalOffsetYButtonClicked(uint delta)
        {
            if (!IsSpr) return;

            SetGlobalOffset(0, -(int)delta);
        }
        void IDebugPageCommand.OnIncreaseSprGlobalWidthButtonClicked(uint delta)
        {
            if (!IsSpr) return;

            SetGlobalSize((int)delta, 0);
        }
        void IDebugPageCommand.OnDecreaseSprGlobalWidthButtonClicked(uint delta)
        {
            if (!IsSpr) return;

            SetGlobalSize(-(int)delta, 0);
        }
        void IDebugPageCommand.OnIncreaseSprGlobalHeightButtonClicked(uint delta)
        {
            if (!IsSpr) return;

            SetGlobalSize(0, (int)delta);
        }
        void IDebugPageCommand.OnDecreaseSprGlobalHeightButtonClicked(uint delta)
        {
            if (!IsSpr) return;

            SetGlobalSize(0, -(int)delta);
        }

        void IDebugPageCommand.SetSprGlobalSize(ushort? width, ushort? height)
        {
            if (!IsSpr) return;

            var nW = width ?? FileHeadEditorVM.FileHead.GlobalWidth;
            var nH = height ?? FileHeadEditorVM.FileHead.GlobalHeight;

            SetGlobalSize(nW - FileHeadEditorVM.FileHead.GlobalWidth, nH - FileHeadEditorVM.FileHead.GlobalHeight);
        }

        void IDebugPageCommand.SetSprGlobalOffset(short? offX, short? offY)
        {
            if (!IsSpr) return;

            var nOffX = offX ?? FileHeadEditorVM.FileHead.OffX;
            var nOffY = offY ?? FileHeadEditorVM.FileHead.OffY;

            SetGlobalOffset(nOffX - FileHeadEditorVM.FileHead.OffX, nOffY - FileHeadEditorVM.FileHead.OffY);
        }

        void IDebugPageCommand.SetSprFrameSize(ushort? width, ushort? height)
        {
            if (!IsSpr) return;

            var nW = width ?? FileHeadEditorVM.CurrentFrameData.frameWidth;
            var nH = height ?? FileHeadEditorVM.CurrentFrameData.frameHeight;

            SetFrameSize(nW - FileHeadEditorVM.CurrentFrameData.frameWidth, nH - FileHeadEditorVM.CurrentFrameData.frameHeight);
        }

        void IDebugPageCommand.SetSprFrameOffset(short? offX, short? offY)
        {
            if (!IsSpr) return;

            var nOffX = offX ?? FileHeadEditorVM.CurrentFrameData.frameOffX;
            var nOffY = offY ?? FileHeadEditorVM.CurrentFrameData.frameOffY;

            SetFrameOffset(nOffX - FileHeadEditorVM.CurrentFrameData.frameOffX, nOffY - FileHeadEditorVM.CurrentFrameData.frameOffY);
        }

        private void SetFrameOffset(int deltaX, int deltaY)
        {
            var newOffX = (short)(FileHeadEditorVM.CurrentFrameData.modifiedFrameRGBACache.frameOffX + deltaX);
            var newOffY = (short)(FileHeadEditorVM.CurrentFrameData.modifiedFrameRGBACache.frameOffY + deltaY);
            BitmapDisplayManager.SetCurrentlyDisplayedFrameOffset(newOffX, newOffY);
        }

        private void SetGlobalOffset(int deltaX, int deltaY)
        {
            var newOffX = (short)(FileHeadEditorVM.FileHead.modifiedSprFileHeadCache.offX + deltaX);
            var newOffY = (short)(FileHeadEditorVM.FileHead.modifiedSprFileHeadCache.offY + deltaY);
            BitmapDisplayManager.SetSprGlobalOffset(newOffX, newOffY);
        }

        private void SetGlobalSize(int deltaWidth, int deltaHeight)
        {
            var tempWidth = FileHeadEditorVM.FileHead.modifiedSprFileHeadCache.globalWidth + deltaWidth;
            var tempHeight = FileHeadEditorVM.FileHead.modifiedSprFileHeadCache.globalHeight + deltaHeight;
            var newWidth = (ushort)(tempWidth < 0 ? 0 : tempWidth);
            var newHeight = (ushort)(tempHeight < 0 ? 0 : tempHeight);
            BitmapDisplayManager.SetSprGlobalSize(newWidth, newHeight);
        }

        private void SetFrameSize(int deltaWidth, int deltaHeight)
        {
            var tempWidth = FileHeadEditorVM.CurrentFrameData.modifiedFrameRGBACache.frameWidth + deltaWidth;
            var tempHeight = FileHeadEditorVM.CurrentFrameData.modifiedFrameRGBACache.frameHeight + deltaHeight;
            var newWidth = (ushort)(tempWidth < 0 ? 0 : tempWidth);
            var newHeight = (ushort)(tempHeight < 0 ? 0 : tempHeight);
            BitmapDisplayManager.SetCurrentlyDisplayedFrameSize(newWidth, newHeight);
        }
        #endregion

        void IDebugPageCommand.OnIncreaseCurrentlyDisplayedSprFrameIndex()
        {
            if (!IsSpr) return;

            if (FileHeadEditorVM.CurrentFrameIndex < FileHeadEditorVM.FileHead.modifiedSprFileHeadCache.FrameCounts - 1)
            {
                BitmapDisplayManager.SetCurrentlyDisplayedSprFrameIndex((uint)FileHeadEditorVM.CurrentFrameIndex + 1);
            }
            else
            {
                BitmapDisplayManager.SetCurrentlyDisplayedSprFrameIndex(0);
            }
        }

        void IDebugPageCommand.OnDecreaseCurrentlyDisplayedSprFrameIndex()
        {
            if (!IsSpr) return;

            if (FileHeadEditorVM.CurrentFrameIndex >= 1)
            {
                BitmapDisplayManager.SetCurrentlyDisplayedSprFrameIndex((uint)FileHeadEditorVM.CurrentFrameIndex - 1);
            }
            else
            {
                BitmapDisplayManager.SetCurrentlyDisplayedSprFrameIndex((uint)FileHeadEditorVM.FileHead.FrameCounts - 1);
            }
        }

        void IDebugPageCommand.OnDecreaseIntervalButtonClicked()
        {
            if (!IsSpr) return;

            if (FileHeadEditorVM.FileHead.Interval > 0)
            {
                BitmapDisplayManager.SetSprInterval((ushort)(FileHeadEditorVM.FileHead.Interval - 1));
            }
        }

        void IDebugPageCommand.SetSprInterval(ushort interval)
        {
            if (!IsSpr) return;

            BitmapDisplayManager.SetSprInterval(interval);
        }

        void IDebugPageCommand.OnIncreaseIntervalButtonClicked()
        {
            if (!IsSpr) return;

            if (FileHeadEditorVM.FileHead.Interval < 1000)
            {
                BitmapDisplayManager.SetSprInterval((ushort)(FileHeadEditorVM.FileHead.Interval + 1));
            }
        }

        void IDebugPageCommand.OnSaveCurrentDisplayedBitmapSourceToSpr(string filePath)
        {
            if (IsSpr) return;
            CurrentlyDisplayedBitmapSource?.Apply(it =>
            {
                BitmapDisplayManager.CountColors(
                    it
                    , out long argbCount
                    , out long rgbCount
                    , out _
                    , out HashSet<Color> rgbSrc);
                if (argbCount > rgbCount && rgbCount <= 256)
                {
                    SprWorkManager.SaveBitmapSourceToSprFile(it, filePath);
                }
                else
                {
                    BitmapDisplayManager.OptimzeImageColorNA256(it)?.Apply(it =>
                    {
                        SprWorkManager.SaveBitmapSourceToSprFile(it, filePath);
                    });
                }

            });
        }

        void IDebugPageCommand.OnImportCurrentDisplaySourceToNextFrameOfSprWorkSpace()
        {
            if (CurrentlyDisplayedBitmapSource != null)
            {
                if (FileHeadEditorVM.FileHead.VersionInfo == null)
                {
                    BitmapDisplayManager.InsertFrame(0, CurrentlyDisplayedBitmapSource);
                }
                else
                {
                    BitmapDisplayManager.InsertFrame(FileHeadEditorVM.FileHead.modifiedSprFileHeadCache.FrameCounts, CurrentlyDisplayedBitmapSource);
                }
                BitmapDisplayManager.ChangeCurrentDisplayMode(isSpr: true);
            }
        }

        bool IDebugPageCommand.OnSwitchFrameClicked(uint frameIndex1, uint frameIndex2)
        {
            return BitmapDisplayManager.SwitchFrame(frameIndex1, frameIndex2);
        }

        bool IDebugPageCommand.OnRemoveFrameClicked(uint frameIndex)
        {
            return BitmapDisplayManager.DeleteFrame(frameIndex);
        }

        bool IDebugPageCommand.OnInsertFrameClicked(uint frameIndex, string[] filePaths)
        {
            var res = false;
            filePaths.FoEach(it =>
            {
                res = BitmapDisplayManager.InsertFrame(frameIndex++, it);
            });
            return res;
        }

        void IDebugPageCommand.OnFramePointerClick(uint frameIndex)
        {
            if (!IsSpr) return;

            if (FileHeadEditorVM.CurrentFrameIndex != frameIndex && frameIndex < FileHeadEditorVM.FileHead.FrameCounts)
            {
                BitmapDisplayManager.SetCurrentlyDisplayedSprFrameIndex(frameIndex);
            }
        }

        void IDebugPageCommand.OnPreviewColorPaletteChanged(uint colorIndex, Color newColor)
        {
            if (!IsSpr) return;

            BitmapDisplayManager.SetNewColorToPalette(colorIndex, newColor);
        }

        #endregion
    }

    public class FrameViewModel : BaseSubViewModel, IFramePreviewerViewModel
    {
        private ImageSource? _imgSrc;
        private int _globalWidth = 100;
        private int _globalHeight = 100;
        private int _height = 30;
        private int _width = 30;
        private int _frameOffsetX = 5;
        private int _frameOffsetY = 5;
        private int _globalOffsetX = 50;
        private int _globalOffsetY = 50;
        public FrameViewModel(BaseParentsViewModel parents) : base(parents)
        {
        }

        public ImageSource? PreviewImageSource
        {
            get => _imgSrc;
            set
            {
                _imgSrc = value;
                Invalidate();
            }
        }
        public int FrameHeight
        {
            get => _height;
            set
            {
                _height = value;
                Invalidate();
            }
        }
        public int FrameWidth
        {
            get => _width;
            set
            {
                _width = value;
                Invalidate();
            }
        }

        public int FrameOffsetX
        {
            get => _frameOffsetX;
            set
            {
                _frameOffsetX = value;
                Invalidate();
            }
        }

        public int FrameOffsetY
        {
            get => _frameOffsetY;
            set
            {
                _frameOffsetY = value;
                Invalidate();
            }
        }

        public int GlobalWidth
        {
            get => _globalWidth;
            set
            {
                _globalWidth = value;
                Invalidate();
            }
        }
        public int GlobalHeight
        {
            get => _globalHeight;
            set
            {
                _globalHeight = value;
                Invalidate();
            }
        }
        string IFramePreviewerViewModel.Index { get => "1"; set { } }

        public int GlobalOffsetX
        {
            get => _globalOffsetX;
            set
            {
                _globalOffsetX = value;
                Invalidate();
            }
        }
        public int GlobalOffsetY
        {
            get => _globalOffsetY;
            set
            {
                _globalOffsetY = value;
                Invalidate();
            }
        }
    }

    public class PaletteEditorColorItemViewModel : BaseViewModel, IPaletteEditorColorItemViewModel
    {
        private SolidColorBrush mBrush;
        public PaletteEditorColorItemViewModel(SolidColorBrush brush)
        {
            mBrush = brush;
        }

        public SolidColorBrush ColorBrush
        {
            get => mBrush;
            set
            {
                mBrush = value;
                Invalidate();
            }
        }
    }
}
