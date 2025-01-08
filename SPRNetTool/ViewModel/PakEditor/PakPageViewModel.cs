using ArtWiz.Domain;
using ArtWiz.Domain.Base;
using ArtWiz.Domain.Utils;
using ArtWiz.LogUtil;
using ArtWiz.Utils;
using ArtWiz.View.Utils;
using ArtWiz.View.Widgets;
using ArtWiz.ViewModel.Base;
using ArtWiz.ViewModel.CommandVM;
using ArtWiz.ViewModel.Widgets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WizMachine.Data;
using WizMachine.Services.Base;
using WizMachine.Services.Utils.NativeEngine.Managed;

namespace ArtWiz.ViewModel.PakEditor
{
    internal enum PakItemLoadingStatus
    {
        NONE,
        ERROR,
        PREPARING,
        LOADING,
        LOADED,
    }

    internal class PakViewModelManager
    {
        private Dictionary<string, PakBlockItemViewModel> _blockIdToPakBlockItemMap = new Dictionary<string, PakBlockItemViewModel>();
        private Dictionary<string, PakFileItemViewModel> _filePathToPakFileItemMap = new Dictionary<string, PakFileItemViewModel>();
        private Dictionary<PakBlockItemViewModel, PakFileItemViewModel> _blockItemToFileItemMap = new Dictionary<PakBlockItemViewModel, PakFileItemViewModel>();
        private Dictionary<PakFileItemViewModel, HashSet<PakBlockItemViewModel>> _fileItemToBlockItemMap = new Dictionary<PakFileItemViewModel, HashSet<PakBlockItemViewModel>>();
        private readonly Queue<PakBlockItemViewModel> _loadedDataBlockItemQueue;
        private int _blockDataSize;
        public PakViewModelManager(int blockDataSizeCache = 3)
        {
            _blockDataSize = blockDataSizeCache;
            _loadedDataBlockItemQueue = new Queue<PakBlockItemViewModel>();
        }

        public ObservableCollection<PakFileItemViewModel> GetPakFileItemViewModel()
        {
            foreach (var pair in _fileItemToBlockItemMap)
            {
                var pakBlockVMs = new ObservableCollection<PakBlockItemViewModel>(pair.Value);
                pair.Key.PakBlocks = pakBlockVMs;
            }
            return new ObservableCollection<PakFileItemViewModel>(_filePathToPakFileItemMap.Values);
        }

        public PakBlockItemViewModel CreatePakBlockViewModel(PakFileItemViewModel pakFileItemViewModel,
            string blockName,
            bool isSpr,
            string blockId,
            long blockSize)
        {
            var blockItemViewModel = new PakBlockItemViewModel(pakFileItemViewModel,
                    blockName: blockName,
                    isSpr: isSpr,
                    blockId: blockId,
                    blockSize: blockSize,
                    vmmanager: this
                    );
            _blockIdToPakBlockItemMap.Add(blockId, blockItemViewModel);
            _blockItemToFileItemMap.Add(blockItemViewModel, pakFileItemViewModel);
            if (!_fileItemToBlockItemMap.ContainsKey(pakFileItemViewModel))
            {
                var h = new HashSet<PakBlockItemViewModel>();
                h.Add(blockItemViewModel);
                _fileItemToBlockItemMap.Add(pakFileItemViewModel, h);
            }
            else
            {
                _fileItemToBlockItemMap[pakFileItemViewModel].Add(blockItemViewModel);
            }
            return blockItemViewModel;
        }

        public PakFileItemViewModel CreatePakItemViewModel(PakPageViewModel pageViewModel, string filePath)
        {
            var newFile = new PakFileItemViewModel(this, pageViewModel, filePath);
            _filePathToPakFileItemMap.Add(filePath, newFile);
            return newFile;
        }

        public void DeletePakFileItemViewModel(PakFileItemViewModel pakFileItem)
        {
            if (_fileItemToBlockItemMap.ContainsKey(pakFileItem))
            {
                foreach (var pakBlock in _fileItemToBlockItemMap[pakFileItem])
                {
                    _blockItemToFileItemMap.Remove(pakBlock);
                    _blockIdToPakBlockItemMap.Remove(pakBlock.BlockId);
                }
                _fileItemToBlockItemMap.Remove(pakFileItem);
            }
            _filePathToPakFileItemMap.Remove(pakFileItem.FilePath);
        }

        public PakFileItemViewModel? GetPakFileItemViewModel(string filePath)
        {
            if (!_filePathToPakFileItemMap.ContainsKey(filePath))
            {
                return null;
            }
            return _filePathToPakFileItemMap[filePath];
        }

        public (PakBlockItemViewModel, PakFileItemViewModel)? FindPakBlockById(string blockId)
        {
            if (_blockIdToPakBlockItemMap.ContainsKey(blockId))
            {
                var blockViewModel = _blockIdToPakBlockItemMap[blockId];
                return (blockViewModel, _blockItemToFileItemMap[blockViewModel]);
            }
            return null;
        }

        public void EnqueueLoadedSuccessfullyBlockData(PakBlockItemViewModel item)
        {
            if (_loadedDataBlockItemQueue.Count >= _blockDataSize)
            {
                // Remove the oldest item and dispose of it to free up memory
                var oldestItem = _loadedDataBlockItemQueue.Dequeue();
                oldestItem.Dispose();
            }

            _loadedDataBlockItemQueue.Enqueue(item);
        }
    }

    internal abstract class PakItemViewModel : BaseSubViewModel
    {
        private PakItemLoadingStatus _loadingStatus = PakItemLoadingStatus.NONE;
        protected long _itemSizeInBytes;

        public virtual PakItemLoadingStatus LoadingStatus
        {
            get => _loadingStatus;
            set
            {
                _loadingStatus = value;

            }
        }

        public PakItemViewModel(BaseParentsViewModel parents) : base(parents)
        {

        }

        protected string FormatFileSize(long bytes)
        {
            const int scale = 1024;
            string[] units = { "Bytes", "KB", "MB", "GB", "TB" };

            if (bytes < scale)
            {
                return $"{bytes} {units[0]}";
            }

            int unitIndex = (int)Math.Log(bytes, scale);
            double adjustedSize = bytes / Math.Pow(scale, unitIndex);

            return $"{adjustedSize:0.##} {units[unitIndex]}";
        }

    }

    internal class PakFileItemViewModel : PakItemViewModel, ILoadPakFileCallback
    {
        private string _crc = "Chưa xác định";
        private string _filePath;
        private string _mappingPath;
        private string _pakTime = "Chưa xác định";
        private string _pakTimeSave = "Chưa xác định";
        private int _blockCount = -1;
        protected int _loadingProgress;

        public override PakItemLoadingStatus LoadingStatus
        {
            get => base.LoadingStatus;
            set
            {
                base.LoadingStatus = value;
                Invalidate();
                Invalidate(nameof(ReloadPakVisibility));
                Invalidate(nameof(RemoveFilePakVisibility));
                Invalidate(nameof(LoadingStatusToString));
                Invalidate(nameof(LoadingProgressBarVisibility));
                Invalidate(nameof(ItemLoadingStatusVisibility));
                //Invalidate(nameof(StartLoadingButtonVisibility));
            }
        }

        private ObservableCollection<PakBlockItemViewModel> _pakBlocks = new ObservableCollection<PakBlockItemViewModel>();
        private PakViewModelManager _viewModelManager;
        private PakBlockItemViewModel? _currentSelectedPakBlock;
        public ObservableCollection<PakBlockItemViewModel> PakBlocks
        {
            get => _pakBlocks;
            set
            {
                _pakBlocks = value;
                Invalidate();
            }
        }
        public string FilePath { get => _filePath; }

        public PakBlockItemViewModel? CurrentSelectedPakBlock
        {
            get => _currentSelectedPakBlock;
            set
            {
                if (_currentSelectedPakBlock != null
                    && _currentSelectedPakBlock.BitmapViewerVM is BlockAnimationViewerViewModel vm)
                {
                    if (vm.IsPlayingAnimation)
                    {
                        vm.IsPlayingAnimation = false;
                    }
                }
                _currentSelectedPakBlock = value;
                Invalidate();
            }
        }

        public string MappingPath
        {
            get => _mappingPath;
            set
            {
                _mappingPath = value;
                Invalidate();
                Invalidate(nameof(MappingStatusToString));
            }
        }

        //[Bindable(true)]
        //public Visibility StartLoadingButtonVisibility
        //{
        //    get
        //    {
        //        if (LoadingStatus == PakItemLoadingStatus.NONE)
        //        {
        //            return Visibility.Collapsed;
        //        }
        //        else
        //        {
        //            return Visibility.Visible;
        //        }
        //    }
        //}

        [Bindable(true)]
        public Visibility LoadingProgressBarVisibility
        {
            get
            {
                if (LoadingStatus == PakItemLoadingStatus.LOADED ||
                    LoadingStatus == PakItemLoadingStatus.NONE)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        [Bindable(true)]
        public Visibility ReloadPakVisibility
        {
            get
            {
                if (LoadingStatus == PakItemLoadingStatus.ERROR || LoadingStatus == PakItemLoadingStatus.LOADED)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }


        [Bindable(true)]
        public Visibility ItemLoadingStatusVisibility
        {
            get
            {
                if (LoadingStatus == PakItemLoadingStatus.NONE
                    || LoadingStatus == PakItemLoadingStatus.LOADED
                    || LoadingStatus == PakItemLoadingStatus.ERROR)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [Bindable(true)]
        public Visibility RemoveFilePakVisibility
        {
            get
            {
                if (LoadingStatus == PakItemLoadingStatus.LOADED ||
                    LoadingStatus == PakItemLoadingStatus.LOADING ||
                    LoadingStatus == PakItemLoadingStatus.ERROR)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [Bindable(true)]
        public string LoadingStatusToString
        {
            get
            {
                switch (LoadingStatus)
                {
                    case PakItemLoadingStatus.LOADING:
                        return "loading...";
                    case PakItemLoadingStatus.LOADED:
                        return "Thành công";
                    case PakItemLoadingStatus.NONE:
                        return "Chưa bắt đầu";
                    case PakItemLoadingStatus.PREPARING:
                        return "preparing...";
                }
                return "";
            }
        }

        [Bindable(true)]
        public string ItemName
        {
            get
            {
                return Path.GetFileName(_filePath);
            }
        }

        [Bindable(true)]
        public string FileSize
        {
            get
            {
                return FormatFileSize(_itemSizeInBytes);
            }
        }

        [Bindable(true)]
        public int LoadingProgress
        {
            get
            {
                return _loadingProgress;
            }
            set
            {
                _loadingProgress = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public string MappingStatusToString
        {
            get
            {
                if (string.IsNullOrEmpty(_mappingPath))
                {
                    return "Chưa map";
                }
                else
                {
                    return "Đã map";
                }
                // return "Đã xảy ra lỗi trong quá trình mapping";
            }
        }

        [Bindable(true)]
        public string BlockCount
        {
            get
            {
                if (_blockCount == -1)
                {
                    return "Chưa xác định";
                }
                else
                {
                    return $"{_blockCount} khối";
                }
            }
            set
            {
                _blockCount = int.Parse(value);
                Invalidate();
            }
        }

        [Bindable(true)]
        public string PakTime
        {
            get
            {
                return _pakTime;
            }
            set
            {
                _pakTime = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public string PakTimeSave
        {
            get
            {
                return _pakTimeSave;
            }
            set
            {
                _pakTimeSave = value;
                Invalidate();
            }
        }
        [Bindable(true)]
        public string CRC
        {
            get
            {
                return _crc;
            }
            set
            {
                _crc = value;
                Invalidate();
            }
        }

        public Dispatcher ViewDispatcher => ViewModelOwner.ViewDispatcher;

        public PakFileItemViewModel(PakViewModelManager viewModelManager, BaseParentsViewModel parents, string filePath) : base(parents)
        {
            _viewModelManager = viewModelManager;
            _filePath = filePath;

            if (File.Exists(_filePath))
            {
                _itemSizeInBytes = new FileInfo(_filePath).Length;
            }
            else
            {
                _itemSizeInBytes = 0;
            }

        }

        public void StartLoadPakFileToWorkManagerAsync()
        {
            if (File.Exists(_filePath) && LoadingStatus == PakItemLoadingStatus.NONE)
            {
                LoadingStatus = PakItemLoadingStatus.PREPARING;
                PakWorkManager.LoadPakFileToWorkManagerAsync(_filePath, this);
            }
            else
            {
                LoadingStatus = PakItemLoadingStatus.ERROR;
            }
        }

        public void SetFilePath(string filePath)
        {
            _filePath = filePath;
            if (File.Exists(_filePath))
            {
                _itemSizeInBytes = new FileInfo(_filePath).Length;
            }
            else
            {
                _itemSizeInBytes = 0;
            }
            InvalidateAll();
        }

        public void OnSessionCreated(Bundle? bundle)
        {
            LoadingStatus = PakItemLoadingStatus.LOADING;
            BlockCount = (bundle?.GetInt(PakContract.EXTRA_BLOCK_COUNT_KEY) ?? -1).ToString();
            PakTime = bundle?.GetString(PakContract.EXTRA_PAK_TIME_KEY) ?? "";
            PakTimeSave = bundle?.GetString(PakContract.EXTRA_PAK_TIME_SAVE_KEY) ?? "";
            CRC = bundle?.GetString(PakContract.EXTRA_PAK_CRC_KEY) ?? "";
        }

        public void OnBlockLoaded(Bundle? bundle)
        {
            if (bundle != null)
            {
                var isSpr = bundle.GetBoolean(PakContract.EXTRA_BLOCK_IS_SPR_KEY) ?? false;
                var blockId = bundle.GetString(PakContract.EXTRA_BLOCK_ID_KEY) ?? "unknown";
                var blockSize = bundle.GetInt(PakContract.EXTRA_BLOCK_SIZE_KEY) ?? 0;
                var blockName = bundle.GetString(PakContract.EXTRA_BLOCK_FILE_NAME_KEY) ?? "unknown";
                var blockIndex = bundle.GetInt(PakContract.EXTRA_BLOCK_INDEX_KEY) ?? 0;


                var blockItemViewModel = _viewModelManager.CreatePakBlockViewModel(this, blockName: blockName,
                    isSpr: isSpr,
                    blockId: blockId,
                    blockSize: blockSize);
                PakBlocks.Add(blockItemViewModel);
            }
        }

        public void OnBlockLoadCompleted()
        {
        }

        public void OnLoadFailed()
        {
            LoadingStatus = PakItemLoadingStatus.ERROR;
        }

        public void OnProgressChanged(int newProgress)
        {
            LoadingProgress = newProgress;
        }

        public void OnFinishJob()
        {
            LoadingStatus = PakItemLoadingStatus.LOADED;
        }

        public void OnLoadCompleted(Bundle? bundle)
        {
        }
    }

    internal class PakBlockItemViewModel : PakItemViewModel, IParseBlockDataCallback, IDisposable
    {
        private string _blockName;
        private string _blockId;
        private FrameRGBA[]? _frameData;
        private SprFileHead _sprFileHead;
        private BitmapSource[]? _frameSource;
        private string _stringBlockData;
        private bool _isLoadingBlock;
        private IBitmapViewerViewModel _bitmapViewerVM;
        private IFileHeadEditorViewModel _fileHeadEditorVM;
        private PakViewModelManager _viewModelManager;
        private Visibility _sprInfoPanelCollapseButtonVisibility = Visibility.Hidden;

        public bool IsSpr { get; private set; }

        [Bindable(true)]
        public Visibility SprInfoPanelCollapseButtonVisibility
        {
            get => _sprInfoPanelCollapseButtonVisibility;
            set
            {
                _sprInfoPanelCollapseButtonVisibility = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public IFileHeadEditorViewModel FileHeadEditorVM
        {
            get => _fileHeadEditorVM;
        }

        [Bindable(true)]
        public IBitmapViewerViewModel BitmapViewerVM
        {
            get => _bitmapViewerVM;
            set
            {
                _bitmapViewerVM = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public bool IsLoadingBlock
        {
            get
            {
                return _isLoadingBlock;
            }
            set
            {
                _isLoadingBlock = value;
                Invalidate();
            }
        }
        [Bindable(true)]
        public string BlockName
        {
            get
            {
                return _blockName;
            }
            set
            {
                _blockName = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public string BlockType
        {
            get
            {
                return IsSpr ? "SPR" : "unknown";
            }
        }

        [Bindable(true)]
        public string BlockId
        {
            get
            {
                return _blockId;
            }
        }

        [Bindable(true)]
        public string BlockSize
        {
            get
            {
                return FormatFileSize(_itemSizeInBytes);
            }
        }

        public Dispatcher ViewDispatcher => ViewModelOwner.ViewDispatcher;

        public PakBlockItemViewModel(BaseParentsViewModel parents,
            string blockName,
            bool isSpr,
            string blockId,
            long blockSize, PakViewModelManager vmmanager) : base(parents)
        {
            _fileHeadEditorVM = new FileHeadEditorViewModel(this);

            if (isSpr)
                _bitmapViewerVM = new BlockAnimationViewerViewModel(this);
            else
                _bitmapViewerVM = new BitmapViewerViewModel(this);
            _bitmapViewerVM.IsSpr = isSpr;
            _itemSizeInBytes = blockSize;
            _blockName = blockName;
            IsSpr = isSpr;
            _blockId = blockId;
            _viewModelManager = vmmanager;
        }

        public bool IsDataLoaded()
        {
            if (IsSpr)
            {
                return _frameData != null;
            }
            else
            {
                return !string.IsNullOrEmpty(_stringBlockData);
            }
        }

        public void StartLoadingBlockData()
        {
            if (IsDataLoaded())
            {
                return;
            }
            IsLoadingBlock = true;
            PakWorkManager.ParseSprBlockDataById(_blockId, this);
        }

        public void GetSprData(out SprFileHead sprFileHead, out FrameRGBA[] frameRGBAs)
        {
            sprFileHead = this._sprFileHead;
            frameRGBAs = this._frameData;
        }

        public void OnParseSprSuccessfully(string blockId, SprFileHead sprFileHead, FrameRGBA[] frameData, BitmapSource bitmapSource)
        {
            var s = new BitmapSource[sprFileHead.FrameCounts];
            s[0] = bitmapSource;
            _frameData = frameData;
            _sprFileHead = sprFileHead;
            _frameSource = s;
            IsLoadingBlock = false;
            var frameInfo = frameData[0];
            _bitmapViewerVM.FrameOffX = frameInfo.frameOffX;
            _bitmapViewerVM.FrameOffY = frameInfo.frameOffY;
            _bitmapViewerVM.FrameHeight = frameInfo.frameHeight;
            _bitmapViewerVM.FrameWidth = frameInfo.frameWidth;
            _bitmapViewerVM.GlobalWidth = sprFileHead.GlobalWidth;
            _bitmapViewerVM.GlobalHeight = sprFileHead.GlobalHeight;
            _bitmapViewerVM.GlobalOffX = sprFileHead.OffX;
            _bitmapViewerVM.GlobalOffY = sprFileHead.OffY;
            _bitmapViewerVM.FrameSource = bitmapSource;
            _viewModelManager.EnqueueLoadedSuccessfullyBlockData(this);
            SprInfoPanelCollapseButtonVisibility = Visibility.Visible;

            FileHeadEditorVM.CurrentFrameData = frameInfo;
            FileHeadEditorVM.CurrentFrameIndex = 0;
            FileHeadEditorVM.IsSpr = true;
            FileHeadEditorVM.IsEditable = false;
            FileHeadEditorVM.FileHead = sprFileHead;
        }

        public void OnFinishJob()
        {
        }

        public void Dispose()
        {
            _frameData = null;
            _frameSource = null;
            BitmapViewerVM = new BitmapViewerViewModel(this);
        }
    }

    internal class PakPageViewModel : BaseParentsViewModel, IPakPageCommand, IRemovePakFileCallback
    {
        private static Logger logger = new Logger(typeof(PakPageViewModel).Name);
        private PakFileItemViewModel? _currentSelectedPakFile;
        private ObservableCollection<PakFileItemViewModel> _pakFiles;
        private PakViewModelManager _viewModelManager;
        private Visibility _searchBoxVisibility = Visibility.Visible;
        private Visibility _initPanelVisibility = Visibility.Visible;
        private Visibility _detailPanelVisibility = Visibility.Visible;
        private string _blockFolderOutputPath = "";

        public string BlockFolderOutputPath
        {
            get
            {
                return _blockFolderOutputPath;
            }
            set
            {
                _blockFolderOutputPath = value;
                Invalidate();
            }
        }
        public Visibility DetailPanelVisibility
        {
            get
            {
                return _detailPanelVisibility;
            }
            set
            {
                _detailPanelVisibility = value;
                Invalidate();
            }
        }

        public Visibility InitPanelVisibility
        {
            get
            {
                return _initPanelVisibility;
            }
            set
            {
                _initPanelVisibility = value;
                Invalidate();
            }
        }
        public Visibility SearchBoxVisibility
        {
            get
            {
                return _searchBoxVisibility;
            }
            set
            {
                _searchBoxVisibility = value;
                Invalidate();
            }
        }

        public ObservableCollection<PakFileItemViewModel> PakFiles
        {
            get => _pakFiles;
            private set
            {
                _pakFiles = value;
                Invalidate();
            }
        }

        public PakFileItemViewModel? CurrentSelectedPakFile
        {
            get => _currentSelectedPakFile;
            set
            {
                _currentSelectedPakFile = value;
                Invalidate();
            }
        }

        public Dispatcher ViewDispatcher => ViewModelOwner.ViewDispatcher;

        public PakPageViewModel()
        {
            _pakFiles = new ObservableCollection<PakFileItemViewModel>();
            _pakFiles.CollectionChanged += OnPakItemViewModelCollectionChanged;
            _viewModelManager = new PakViewModelManager();
            UpdatePakEditorComponentVisibility();
        }

        private void OnPakItemViewModelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdatePakEditorComponentVisibility();
        }

        void IPakPageCommand.OnAddedPakFileClick(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                logger.E($"File path is invalid or does not exist: {filePath}");
                return;
            }

            if (PakWorkManager.IsFileAlreadyAdded(filePath))
            {
                logger.E($"File already added {filePath}");
                return;
            }

            var pakItemViewModel = _viewModelManager.CreatePakItemViewModel(this, filePath);
            pakItemViewModel.PropertyChanged += OnPakItemPropertyChanged;
            PakFiles.Add(pakItemViewModel);
        }

        private void OnPakItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is PakFileItemViewModel pIVM && e.PropertyName == nameof(PakFileItemViewModel.CurrentSelectedPakBlock))
            {
                if (pIVM.CurrentSelectedPakBlock != null && pIVM.CurrentSelectedPakBlock.IsDataLoaded() == false)
                {
                    if (pIVM.CurrentSelectedPakBlock.IsSpr)
                    {
                        pIVM.CurrentSelectedPakBlock.StartLoadingBlockData();
                    }
                    else
                    {

                    }
                }
                else
                {
                }
            }
        }

        void IPakPageCommand.OnResetSearchBox()
        {
            if (CurrentSelectedPakFile != null)
                CurrentSelectedPakFile.CurrentSelectedPakBlock = null;
            PakFiles.CollectionChanged -= OnPakItemViewModelCollectionChanged;
            PakFiles = _viewModelManager.GetPakFileItemViewModel();
            PakFiles.CollectionChanged += OnPakItemViewModelCollectionChanged;
            CurrentSelectedPakFile = null;
        }

        void IPakPageCommand.OnRemovePakFileClick(object pakFileViewModel)
        {
            pakFileViewModel.IfIs<PakFileItemViewModel>(it =>
            {
                if (PakWorkManager.IsFileAlreadyAdded(it.FilePath))
                {
                    PakWorkManager.CloseSessionAsync(it.FilePath, this);
                }
                else if (it.LoadingStatus == PakItemLoadingStatus.NONE)
                {
                    var vm = _viewModelManager.GetPakFileItemViewModel(it.FilePath);
                    if (vm != null)
                    {
                        vm.PropertyChanged -= OnPakItemPropertyChanged;
                        _viewModelManager.DeletePakFileItemViewModel(vm);
                        PakFiles.Remove(vm);
                    }
                }
            });
        }

        void IPakPageCommand.OnSearchPakBlockByPath(string blockPath)
        {
            var blockInfo = PakWorkManager.GetBlockInfoByPath(blockPath);
            if (blockInfo != null)
            {
                (PakBlockItemViewModel blockVM, PakFileItemViewModel pakFileVM)? result =
                        _viewModelManager.FindPakBlockById(blockInfo.Value.id);
                if (result.HasValue)
                {
                    var (blockVM, pakFileVM) = result.Value;
                    PakFiles.Clear();
                    PakFiles.Add(pakFileVM);
                    pakFileVM.PakBlocks.Clear();
                    pakFileVM.PakBlocks.Add(blockVM);
                    pakFileVM.CurrentSelectedPakBlock = blockVM;
                    CurrentSelectedPakFile = pakFileVM;
                }
                else
                {
                }
            }
        }

        void IPakPageCommand.OnExtractCurrentSelectedBlock()
        {
            if (CurrentSelectedPakFile != null && CurrentSelectedPakFile.CurrentSelectedPakBlock != null)
            {
                PakBlockItemViewModel pakBlock = CurrentSelectedPakFile!.CurrentSelectedPakBlock!;

                string? outputPath = _blockFolderOutputPath;
                if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
                {
                    using (var folderDialog = new FolderBrowserDialog())
                    {
                        folderDialog.Description = "Chọn thư mục để lưu block đã extract:";
                        folderDialog.ShowNewFolderButton = true;

                        if (folderDialog.ShowDialog() == DialogResult.OK)
                        {
                            outputPath = folderDialog.SelectedPath;
                            _blockFolderOutputPath = outputPath;
                        }
                        else
                        {
                            logger.I("Người dùng không chọn thư mục.");
                            return;
                        }
                    }
                }

                if (pakBlock.BlockType == "SPR")
                {
                    outputPath = Path.Combine(outputPath, pakBlock.BlockName + ".spr");
                }
                else
                {
                    outputPath = Path.Combine(outputPath, pakBlock.BlockName + ".txt");
                }

                bool success = PakWorkManager.ExtractPakBlockById(pakBlock.BlockId, outputPath!);
                if (success)
                {
                    logger.I($"Extract block '{pakBlock.BlockId}' thành công vào '{outputPath}'.");
                }
                else
                {
                    logger.I($"Extract block '{pakBlock.BlockId}' thất bại.");
                }
            }
            else
            {
                logger.E("Không có PakFile hoặc PakBlock nào được chọn.");
            }
        }

        public void OnRemoveSuccess(object removedPakFile)
        {
            removedPakFile.IfIs<string>(it =>
            {
                var viewModel = _viewModelManager.GetPakFileItemViewModel(it);
                if (viewModel != null)
                {
                    viewModel.PropertyChanged -= OnPakItemPropertyChanged;
                    _viewModelManager.DeletePakFileItemViewModel(viewModel);
                    PakFiles.Remove(viewModel);
                }
            });
        }

        public void OnFinishJob()
        {
        }

        private void UpdatePakEditorComponentVisibility()
        {
            if (PakFiles.Count == 0)
            {
                InitPanelVisibility = Visibility.Visible;
                DetailPanelVisibility = Visibility.Collapsed;
                SearchBoxVisibility = Visibility.Collapsed;
            }
            else
            {
                InitPanelVisibility = Visibility.Collapsed;
                DetailPanelVisibility = Visibility.Visible;
                SearchBoxVisibility = Visibility.Visible;
            }
        }

    }
}
