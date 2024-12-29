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
        void OnSessionCreated();
        void OnBlockLoaded(Bundle? bundle);
        void OnBlockLoadCompleted();
        void OnLoadCompleted(Bundle? bundle);
        void OnLoadFailed();
        void OnProgressChanged(int newProgress);
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
    }
}
