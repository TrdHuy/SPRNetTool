using System;
using System.Collections.Generic;

namespace ArtWiz.ViewModel.Base
{
    public abstract class BaseParentsViewModel : BaseViewModel, IArtWizViewModel
    {
        private List<IArtWizViewModel> subViewModels = new List<IArtWizViewModel>();
        private IArtWizViewModelOwner? _viewModelOwner;
        public IArtWizViewModelOwner ViewModelOwner
        {
            get
            {
                if (_viewModelOwner == null)
                {
                    throw new Exception("Owner must not be null");
                }
                return _viewModelOwner;
            }
            private set
            {
                _viewModelOwner = value;
            }
        }
        protected bool IsOwnerDestroyed { get; private set; } = false;
        public bool IsOwnerCreated { get; private set; } = false;
        public virtual void OnArtWizViewModelOwnerCreate(IArtWizViewModelOwner owner)
        {
            ViewModelOwner = owner;
            foreach (var vm in subViewModels)
            {
                (vm).OnArtWizViewModelOwnerCreate(owner);
            }
            IsOwnerCreated = true;
        }

        public virtual void OnArtWizViewModelOwnerDestroy()
        {
            IsOwnerDestroyed = true;
            foreach (var vm in subViewModels)
            {
                (vm).OnArtWizViewModelOwnerDestroy();
            }
            subViewModels.Clear();
        }

        public void RegisterSubViewModel(BaseSubViewModel subViewModel)
        {
            if (!subViewModels.Contains(subViewModel))
            {
                subViewModels.Add(subViewModel);
            }
        }
    }
}
