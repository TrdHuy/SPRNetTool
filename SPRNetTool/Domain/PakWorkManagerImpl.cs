using ArtWiz.Domain.Base;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Markup;
using WizMachine;

namespace ArtWiz.Domain
{
    internal class PakWorkManagerImpl : BaseDomain, IPakWorkManager
    {
        protected WizMachine.Services.Base.IPakWorkManager _pakWorkManagerService;
        WizMachine.Services.Base.IPakWorkManager IPakWorkManager.PakWorkManagerService => _pakWorkManagerService;

        private readonly ConcurrentDictionary<string, string> _filePathToTokenMap = new ConcurrentDictionary<string, string>();

        public PakWorkManagerImpl()
        {
            _pakWorkManagerService = EngineKeeper.GetPakWorkManagerService();
        }

        public async void CloseSessionAsync(string filePath, IRemovePakFileCallback removeFileCallback)
        {
            if (!IsFileAlreadyAdded(filePath))
            {
                return;
            }

            await Task.Run(() =>
            {
                var result = _pakWorkManagerService.RemovePakFileFromWorkManager(filePath);

                if (result)
                {
                    _filePathToTokenMap.TryRemove(filePath, out _);
                    NotifyRemovePakFileObserver(removeFileCallback, "REMOVE_SUCCESS");
                }
            });
            NotifyRemovePakFileObserver(removeFileCallback, "FINISH_JOB");
        }

        public async void LoadPakFileToWorkManagerAsync(string pakFilePath, ILoadPakFileCallback loadPakCallback)
        {
            if (IsFileAlreadyAdded(pakFilePath))
            {
                loadPakCallback.OnLoadFailed();
                return;
            }
            await Task.Run(() =>
            {
                var currentProgress = -1;
                var result = _pakWorkManagerService.LoadPakFileToWorkManager(pakFilePath, (progress, message) =>
                {
                    if (WizMachine.Services.Base.IPakWorkManager.ProcessCallbackMessage(message, out string eventType, out string data))
                    {
                        if (eventType == "TOKEN_CREATED")
                        {
                            _filePathToTokenMap[pakFilePath] = data;
                        }
                        NotifyLoadPakFileObserver(loadPakCallback, eventType, progress);
                    }

                    if (currentProgress != progress)
                    {
                        NotifyLoadPakFileObserver(loadPakCallback, "PROGRESS_CHANGED", progress);
                        currentProgress = progress;
                    }
                });

                if (!result)
                {
                    NotifyLoadPakFileObserver(loadPakCallback, "LOAD_FAILED", 0);
                    _filePathToTokenMap.TryRemove(pakFilePath, out _);
                }

                NotifyLoadPakFileObserver(loadPakCallback, "FINISH_JOB", 100);

            });

        }

        public bool IsFileAlreadyAdded(string filePath)
        {
            return _filePathToTokenMap.ContainsKey(filePath);
        }

        private void NotifyLoadPakFileObserver(ILoadPakFileCallback loadPakCallback,
            string eventType,
            int newProgress)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                switch (eventType)
                {
                    case "TOKEN_CREATED":
                        loadPakCallback.OnSessionCreated();
                        break;
                    case "BLOCK_LOADED":
                        loadPakCallback.OnBlockLoaded();
                        break;
                    case "LOAD_COMPLETED":
                        loadPakCallback.OnLoadCompleted();
                        break;
                    case "PROGRESS_CHANGED":
                        loadPakCallback.OnProgressChanged(newProgress);
                        break;
                    case "LOAD_FAILED":
                        loadPakCallback.OnLoadFailed();
                        break;
                    case "FINISH_JOB":
                        loadPakCallback.OnFinishJob();
                        break;
                }

            });
        }

        private void NotifyRemovePakFileObserver(IRemovePakFileCallback removePakCallback,
          string eventType)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                switch (eventType)
                {
                    case "REMOVE_SUCCESS":
                        removePakCallback.OnRemoveSuccess();
                        break;
                    case "FINISH_JOB":
                        removePakCallback.OnFinishJob();
                        break;
                }

            });
        }
    }
}
