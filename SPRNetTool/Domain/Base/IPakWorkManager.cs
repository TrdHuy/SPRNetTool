using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WizMachine.Data;
using WizMachine.Services.Utils.NativeEngine.Managed;

namespace ArtWiz.Domain.Base
{
    public interface IJobExecutor
    {
        void OnFinishJob();
    }
    public interface ILoadPakFileCallback : IJobExecutor
    {
        Dispatcher ViewDispatcher { get; }
        void OnSessionCreated(Bundle? bundle);
        void OnBlockLoaded(Bundle? bundle);
        void OnBlockLoadCompleted();
        void OnLoadCompleted(Bundle? bundle);
        void OnLoadFailed();
        void OnProgressChanged(int newProgress);
    }

    public interface IParseBlockDataCallback : IJobExecutor
    {
        public const string SPR_FILE_HEAD_EXTRA = "SPRFileHeadExtra";
        public const string BITMAP_SOURCE_EXTRA = "BitmapSourceExtra";
        public const string FRAME_DATA_EXTRA = "FrameDataExtra";
        public const string BLOCK_ID_EXTRA = "BlockIdExtra";
        Dispatcher ViewDispatcher { get; }
        void OnParseSprSuccessfully(string blockId, SprFileHead sprFileHead, FrameRGBA[] frameData, BitmapSource bitmapSource);
    }
    public interface IRemovePakFileCallback : IJobExecutor
    {
        void OnRemoveSuccess(object removedPakFile);
    }

    public interface IPakWorkManager : IObservableDomain, IDomainAdapter
    {
        protected WizMachine.Services.Base.IPakWorkManager PakWorkManagerService { get; }

        void LoadPakFileToWorkManagerAsync(string pakFilePath, ILoadPakFileCallback loadPakCallback);
        void CloseSessionAsync(string filePath, IRemovePakFileCallback removeFileCallback);

        bool IsFileAlreadyAdded(string filePath);

        bool IsBlockPathExist(string blockPath);

        CompressedFileInfo? GetBlockInfoByPath(string blockPath);

        bool ExtractPakBlockById(string blockId, string outputPath);

        void ParseSprBlockDataById(string blockId, IParseBlockDataCallback parseBlockDataCallback);
        byte[]? ReadBlockDataFromPakByBlockId(string blockId);
    }
}
