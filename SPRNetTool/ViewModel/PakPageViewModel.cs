using ArtWiz.LogUtil;
using ArtWiz.ViewModel.Base;
using ArtWiz.ViewModel.CommandVM;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using WizMachine;
using WizMachine.Services.Base;

namespace ArtWiz.ViewModel
{

    internal class PakFileItemViewModel : BaseSubViewModel
    {
        private string _filePath;
        private long _fileSizeInBytes;
        private int _loadingProgress;

        private IPakWorkManager workManager = EngineKeeper.GetPakWorkManagerService();

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

            _ = StartLoadingPakFileAsync(filePath); // Bắt đầu load file không blocking
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

        // Phương thức async để xử lý tải file
        private async Task StartLoadingPakFileAsync(string filePath)
        {
            await Task.Run(() =>
            {
                // Gọi hàm load file và cập nhật progress
                workManager.LoadPakFileToWorkManager(filePath, (progress, message) =>
                {
                    // Chuyển về UI thread để cập nhật progress
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        LoadingProgress = progress; // Cập nhật progress
                    });
                });
            });
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

            var newFile = new PakFileItemViewModel(this, filePath);
            PakFiles.Add(newFile);
        }
    }
}
