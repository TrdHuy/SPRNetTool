using ArtWiz.Domain.Base;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using WizMachine;
using WizMachine.Data;
using WizMachine.Services.Utils.NativeEngine.Managed;
using static WizMachine.Services.Base.PakContract;
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

        public bool IsBlockPathExist(string blockPath)
        {
            return _pakWorkManagerService.IsBlockExistByPath(blockPath);
        }

        public CompressedFileInfo? GetBlockInfoByPath(string blockPath)
        {
            return _pakWorkManagerService.GetBlockInfoByPath(blockPath);
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
                    NotifyRemovePakFileObserver(removeFileCallback, "REMOVE_SUCCESS", filePath);
                }
            });
            NotifyRemovePakFileObserver(removeFileCallback, "FINISH_JOB", null);
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
                var result = _pakWorkManagerService.LoadPakFileToWorkManager(pakFilePath, (progress, message, bundle) =>
                {
                    if (WizMachine.Services.Base.IPakWorkManager.ProcessCallbackMessage(message, out string eventType, out string data))
                    {
                        if (eventType == EVENT_TOKEN_CREATED_MESSAGE)
                        {
                            _filePathToTokenMap[pakFilePath] = data;
                        }
                        NotifyLoadPakFileObserver(loadPakCallback, eventType, progress, bundle);
                    }

                    if (currentProgress != progress)
                    {
                        NotifyLoadPakFileObserver(loadPakCallback, EVENT_PROGRESS_CHANGED_MESSAGE, progress, bundle);
                        currentProgress = progress;
                    }
                });

                if (!result)
                {
                    NotifyLoadPakFileObserver(loadPakCallback, EVENT_LOAD_FAILED_MESSAGE, 0, null);
                    _filePathToTokenMap.TryRemove(pakFilePath, out _);
                }

                NotifyLoadPakFileObserver(loadPakCallback, "FINISH_JOB", 100, null);

            });

        }

        public bool IsFileAlreadyAdded(string filePath)
        {
            return _filePathToTokenMap.ContainsKey(filePath);
        }

        private void NotifyLoadPakFileObserver(ILoadPakFileCallback loadPakCallback,
            string eventType,
            int newProgress,
            Bundle? bundle)
        {
            loadPakCallback.ViewDispatcher.Invoke(() =>
            {
                switch (eventType)
                {
                    case EVENT_TOKEN_CREATED_MESSAGE:
                        loadPakCallback.OnSessionCreated(bundle);
                        break;
                    case EVENT_BLOCK_LOADED_MESSAGE: // one block loaded successfully
                        loadPakCallback.OnBlockLoaded(bundle);
                        break;
                    case EVENT_BLOCK_LOAD_COMPLETED_MESSAGE: // all blocks loaded successfully
                        loadPakCallback.OnBlockLoadCompleted();
                        break;
                    case EVENT_PROGRESS_CHANGED_MESSAGE:
                        loadPakCallback.OnProgressChanged(newProgress);
                        break;
                    case EVENT_LOAD_FAILED_MESSAGE:
                        loadPakCallback.OnLoadFailed();
                        break;
                    case EVENT_LOAD_COMPLETED_MESSAGE:
                        loadPakCallback.OnLoadCompleted(bundle);
                        break;
                    case "FINISH_JOB":
                        loadPakCallback.OnFinishJob();
                        break;
                }

            });
        }

        private void NotifyRemovePakFileObserver(IRemovePakFileCallback removePakCallback,
            string eventType,
            object callbackData)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                switch (eventType)
                {
                    case "REMOVE_SUCCESS":
                        removePakCallback.OnRemoveSuccess(callbackData);
                        break;
                    case "FINISH_JOB":
                        removePakCallback.OnFinishJob();
                        break;
                }

            });
        }
    }
}
