using ArtWiz.Domain.Base;
using ArtWiz.LogUtil;
using ArtWiz.Utils;
using ArtWiz.ViewModel.Base;
using ArtWiz.ViewModel.CommandVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
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

        public ObservableCollection<PakFileItemViewModel> GetPakFileItemViewModel()
        {
            foreach(var pair in _fileItemToBlockItemMap)
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
            foreach (var pakBlock in _fileItemToBlockItemMap[pakFileItem])
            {
                _blockItemToFileItemMap.Remove(pakBlock);
                _blockIdToPakBlockItemMap.Remove(pakBlock.BlockId);
            }
            _fileItemToBlockItemMap.Remove(pakFileItem);
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

        protected virtual PakItemLoadingStatus LoadingStatus
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
        private string _filePath;
        protected int _loadingProgress;

        protected override PakItemLoadingStatus LoadingStatus
        {
            get => base.LoadingStatus;
            set
            {
                base.LoadingStatus = value;
                Invalidate(nameof(ReloadPakVisibility));
                Invalidate(nameof(RemoveFilePakVisibility));
                Invalidate(nameof(LoadingStatusToString));
                Invalidate(nameof(LoadingProgressBarVisibility));
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
        [Bindable(true)]
        public Visibility LoadingProgressBarVisibility
        {
            get
            {
                if (LoadingStatus == PakItemLoadingStatus.LOADED)
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
                        return "";
                    case PakItemLoadingStatus.NONE:
                        return "";
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
            LoadingStatus = PakItemLoadingStatus.PREPARING;
            PakWorkManager.LoadPakFileToWorkManagerAsync(filePath, this);
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

        public void OnSessionCreated()
        {
            LoadingStatus = PakItemLoadingStatus.LOADING;
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
        private PakViewModelManager _viewModelManager = new PakViewModelManager();

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
            PakFiles = _viewModelManager.GetPakFileItemViewModel();
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


    }
}
