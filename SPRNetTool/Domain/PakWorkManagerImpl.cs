using ArtWiz.Domain.Base;
using ArtWiz.Domain.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        public bool ExtractPakBlockById(string blockId, string outputPath)
        {
            return _pakWorkManagerService.ExtractBlockById(blockId, outputPath);
        }

        public bool IsBlockPathExist(string blockPath)
        {
            return _pakWorkManagerService.IsBlockExistByPath(blockPath);
        }

        public CompressedFileInfo? GetBlockInfoByPath(string blockPath)
        {
            return _pakWorkManagerService.GetBlockInfoByPath(blockPath);
        }

        public byte[]? ReadBlockDataFromPakByBlockId(string blockId)
        {
            return _pakWorkManagerService.ReadBlockDataFromPakByBlockId(blockId);
        }

        public async void ParseSprBlockDataById(string blockId, IParseBlockDataCallback parseBlockDataCallback)
        {
            await Task.Run(() =>
            {
                var blockData = _pakWorkManagerService.ReadBlockDataFromPakByBlockId(blockId);
                if (blockData != null)
                {
                    SprUtil.ParseSprData(blockData,
                        out SprFileHead fileHead, out _,
                        out _,
                        out FrameRGBA[] frameData);
                    var frameIndex = 0;
                    var frameInfo = frameData[frameIndex];
                    var callbackData = new Dictionary<string, object>();
                    var source = ArtWiz.Utils.BitmapUtil.GetBitmapFromRGBArray(frameInfo.originDecodedBGRAData
                        , frameInfo.frameWidth
                        , frameInfo.frameHeight, PixelFormats.Bgra32);
                    source.Freeze();
                    callbackData.Add(IParseBlockDataCallback.FRAME_DATA_EXTRA, frameData);
                    callbackData.Add(IParseBlockDataCallback.BITMAP_SOURCE_EXTRA, source);
                    callbackData.Add(IParseBlockDataCallback.SPR_FILE_HEAD_EXTRA, fileHead);
                    callbackData.Add(IParseBlockDataCallback.BLOCK_ID_EXTRA, blockId);
                    NotifyParseBlockDataObserver(parseBlockDataCallback, "PARSE_BLOCK_SUCCESS", callbackData);
                }
            });

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

        private void NotifyParseBlockDataObserver(IParseBlockDataCallback parseBlockCallback,
            string eventType,
            Dictionary<string, object> callbackData)
        {
            parseBlockCallback.ViewDispatcher.Invoke(() =>
            {

                switch (eventType)
                {
                    case "PARSE_BLOCK_SUCCESS":
                        var fileHead = (SprFileHead)callbackData[IParseBlockDataCallback.SPR_FILE_HEAD_EXTRA];
                        var frameData = (FrameRGBA[])callbackData[IParseBlockDataCallback.FRAME_DATA_EXTRA];
                        var bitmapSource = (BitmapSource)callbackData[IParseBlockDataCallback.BITMAP_SOURCE_EXTRA];
                        var blockId = (string)callbackData[IParseBlockDataCallback.BLOCK_ID_EXTRA];
                        parseBlockCallback.OnParseSprSuccessfully(blockId, fileHead, frameData, bitmapSource);
                        break;
                    case "FINISH_JOB":
                        parseBlockCallback.OnFinishJob();
                        break;
                }
            });
        }
    }
}
