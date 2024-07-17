using ArtWiz.Domain.Base;
using ArtWiz.Utils;

namespace ArtWiz.ViewModel.Base
{
    public abstract class BaseViewModel : BaseNotifier, IDomainObserver, IDomainAccessors
    {
        #region Modules
        private IBitmapDisplayManager? bitmapDisplayManager;
        private ISprWorkManager? sprWorkManager;
        private IDeviceConfigManager? deviceConfigManager;

        protected IDeviceConfigManager DeviceConfigManager
        {
            get
            {
                return deviceConfigManager ?? IDomainAccessors
                    .DomainContext
                    .GetDomain<IDeviceConfigManager>()
                    .Also(it => deviceConfigManager = it);
            }
        }

        protected IBitmapDisplayManager BitmapDisplayManager
        {
            get
            {
                return bitmapDisplayManager ?? IDomainAccessors
                    .DomainContext
                    .GetDomain<IBitmapDisplayManager>()
                    .Also(it => bitmapDisplayManager = it);
            }
        }

        protected ISprWorkManager SprWorkManager
        {
            get
            {
                return sprWorkManager ??
                    IDomainAccessors
                    .DomainContext
                    .GetDomain<ISprWorkManager>()
                    .Also(it => sprWorkManager = it);
            }
        }
        #endregion
        void IDomainObserver.OnDomainChanged(IDomainChangedArgs args)
        {
            OnDomainChanged(args);
        }


        protected virtual void OnDomainChanged(IDomainChangedArgs args) { }



    }
}
