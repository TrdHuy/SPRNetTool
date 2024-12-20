using ArtWiz.Domain.Base;
using ArtWiz.LogUtil;
using ArtWiz.Utils;
using ArtWiz.ViewModel.Base;
using ArtWiz.ViewModel.CommandVM;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

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
    internal class PakFileItemViewModel : BaseSubViewModel, ILoadPakFileCallback, IRemovePakFileCallback
    {
        private string _filePath;
        private long _fileSizeInBytes;
        private int _loadingProgress;
        private PakItemLoadingStatus _loadingStatus = PakItemLoadingStatus.NONE;

        private PakItemLoadingStatus LoadingStatus
        {
            get => _loadingStatus;
            set
            {
                _loadingStatus = value;
                Invalidate(nameof(ReloadPakVisibility));
                Invalidate(nameof(RemoveFilePakVisibility));
                Invalidate(nameof(LoadingStatusToString));
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
                switch (_loadingStatus)
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
                return FormatFileSize(_fileSizeInBytes);
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
                Invalidate(); // Thông báo UI cập nhật giá trị
            }
        }

        public PakFileItemViewModel(BaseParentsViewModel parents, string filePath) : base(parents)
        {
            _filePath = filePath;

            if (File.Exists(_filePath))
            {
                _fileSizeInBytes = new FileInfo(_filePath).Length;
            }
            else
            {
                _fileSizeInBytes = 0;
            }

            LoadingStatus = PakItemLoadingStatus.PREPARING;
            PakWorkManager.LoadPakFileToWorkManagerAsync(filePath, this);
        }

        public void SetFilePath(string filePath)
        {
            _filePath = filePath;
            if (File.Exists(_filePath))
            {
                _fileSizeInBytes = new FileInfo(_filePath).Length;
            }
            else
            {
                _fileSizeInBytes = 0;
            }
            InvalidateAll();
        }

        public void ClosePakSession()
        {
            if (PakWorkManager.IsFileAlreadyAdded(_filePath))
            {
                PakWorkManager.CloseSessionAsync(_filePath, this);
            }
        }

        private string FormatFileSize(long bytes)
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

        public void OnSessionCreated()
        {
            LoadingStatus = PakItemLoadingStatus.LOADING;
        }

        public void OnBlockLoaded()
        {
        }

        public void OnLoadCompleted()
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

        public void OnRemoveSuccess()
        {
            Parents.IfIs<PakPageViewModel>(it =>
            {
                it.PakFiles.Remove(this);
            });
        }
    }

    internal class PakPageViewModel : BaseParentsViewModel, IPakPageCommand
    {
        private static Logger logger = new Logger(typeof(PakPageViewModel).Name);

        // ObservableCollection để quản lý danh sách PakFileItemViewModel
        private ObservableCollection<PakFileItemViewModel> _pakFiles;
        public ObservableCollection<PakFileItemViewModel> PakFiles
        {
            get => _pakFiles;
            private set
            {
                _pakFiles = value;
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

            var newFile = new PakFileItemViewModel(this, filePath);
            PakFiles.Add(newFile);
        }

        void IPakPageCommand.OnRemovePakFileClick(object pakFileViewModel)
        {
            pakFileViewModel.IfIs<PakFileItemViewModel>(it =>
            {
                it.ClosePakSession();
            });
        }
    }
}
