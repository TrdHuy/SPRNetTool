﻿using SPRNetTool.Domain.Base;
using SPRNetTool.Utils;
using SPRNetTool.ViewModel.Base;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SPRNetTool.ViewModel
{
    public abstract class BaseViewModel : BaseNotifier, IDomainObserver, IDomainAccessors, IArtWizViewModel
    {
        #region Modules
        private IBitmapDisplayManager? bitmapDisplayManager;
        private ISprWorkManagerCore? sprWorkManager;

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

        protected ISprWorkManagerCore SprWorkManager
        {
            get
            {
                return sprWorkManager ??
                    IDomainAccessors
                    .DomainContext
                    .GetDomain<ISprWorkManagerCore>()
                    .Also(it => sprWorkManager = it);
            }
        }

        #endregion

        protected IArtWizViewModelOwner? ViewModelOwner { get; private set; }
        protected bool IsViewModelDestroyed { get; private set; } = false;

        protected void Invalidate([CallerMemberName] string caller = "")
        {
            OnPropertyChanged(caller);
        }

        protected void InvalidateAll()
        {
            Type type = GetType();
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                BindableAttribute? attribute = Attribute.GetCustomAttribute(property, typeof(BindableAttribute)) as BindableAttribute;

                if (attribute != null && attribute.Bindable)
                {
                    OnPropertyChanged(property.Name);
                }
            }
        }

        protected virtual void OnDomainChanged(IDomainChangedArgs args) { }


        void IDomainObserver.OnDomainChanged(IDomainChangedArgs args)
        {
            OnDomainChanged(args);
        }

        void IArtWizViewModel.OnCreate(IArtWizViewModelOwner owner)
        {
            ViewModelOwner = owner;
        }

        public virtual void OnDestroy()
        {
            IsViewModelDestroyed = true;
        }
    }
}
