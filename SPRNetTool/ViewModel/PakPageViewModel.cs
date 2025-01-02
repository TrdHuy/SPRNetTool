using ArtWiz.Domain.Base;
using ArtWiz.LogUtil;
using ArtWiz.Utils;
using ArtWiz.View.Utils;
using ArtWiz.ViewModel.Base;
using ArtWiz.ViewModel.CommandVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using WizMachine.Services.Base;
using WizMachine.Services.Utils.NativeEngine.Managed;

namespace ArtWiz.ViewModel
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

        public PakViewModelManager()
        {
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
            string blockType,
            string blockId,
            long blockSize)
        {
            var blockItemViewModel = new PakBlockItemViewModel(pakFileItemViewModel,
                    blockName: blockName,
                    blockType: blockType,
                    blockId: blockId,
                    blockSize: blockSize
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

        Dispatcher ILoadPakFileCallback.ViewDispatcher => ViewModelOwner.ViewDispatcher;

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
                var isSpr = bundle.GetBoolean(PakContract.EXTRA_BLOCK_IS_SPR_KEY);
                var blockId = bundle.GetString(PakContract.EXTRA_BLOCK_ID_KEY) ?? "unknown";
                var blockSize = bundle.GetInt(PakContract.EXTRA_BLOCK_SIZE_KEY) ?? 0;
                var blockName = bundle.GetString(PakContract.EXTRA_BLOCK_FILE_NAME_KEY) ?? "unknown";
                var blockIndex = bundle.GetInt(PakContract.EXTRA_BLOCK_INDEX_KEY) ?? 0;


                var blockItemViewModel = _viewModelManager.CreatePakBlockViewModel(this, blockName: blockName,
                    blockType: isSpr == true ? "SPR" : "unknown",
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

    internal class PakBlockItemViewModel : PakItemViewModel
    {
        private string _blockName;
        private string _blockType;
        private string _blockId;

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
                return _blockType;
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

        public PakBlockItemViewModel(BaseParentsViewModel parents,
            string blockName,
            string blockType,
            string blockId,
            long blockSize) : base(parents)
        {
            _itemSizeInBytes = blockSize;
            _blockName = blockName;
            _blockType = blockType;
            _blockId = blockId;
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

            PakFiles.Add(_viewModelManager.CreatePakItemViewModel(this, filePath));
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
