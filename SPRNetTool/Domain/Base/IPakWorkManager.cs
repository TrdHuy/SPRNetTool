using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtWiz.Domain.Base
{
    public interface IJobExecutor
    {
        void OnFinishJob();
    }
    public interface ILoadPakFileCallback : IJobExecutor
    {
        void OnSessionCreated();
        void OnBlockLoaded();
        void OnLoadCompleted();
        void OnLoadFailed();
        void OnProgressChanged(int newProgress);
    }

    public interface IRemovePakFileCallback : IJobExecutor
    {
        void OnRemoveSuccess();
    }

    public interface IPakWorkManager : IObservableDomain, IDomainAdapter
    {
        protected WizMachine.Services.Base.IPakWorkManager PakWorkManagerService { get; }

        void LoadPakFileToWorkManagerAsync(string pakFilePath, ILoadPakFileCallback loadPakCallback);
        void CloseSessionAsync(string filePath, IRemovePakFileCallback removeFileCallback);

        bool IsFileAlreadyAdded(string filePath);
    }
}
