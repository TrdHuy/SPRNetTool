using System.Collections.Generic;

namespace ArtWiz.ViewModel.Base
{
    public abstract class BaseParentsViewModel : BaseViewModel, IArtWizViewModel
    {
        private List<IArtWizViewModel> subViewModels = new List<IArtWizViewModel>();
        protected IArtWizViewModelOwner? ViewModelOwner { get; private set; }
        protected bool IsViewModelDestroyed { get; private set; } = false;
        public virtual void OnArtWizViewModelOwnerCreate(IArtWizViewModelOwner owner)
        {
            ViewModelOwner = owner;
            foreach (var vm in subViewModels)
            {
                (vm).OnArtWizViewModelOwnerCreate(owner);
            }
        }

        public virtual void OnArtWizViewModelDestroy()
        {
            IsViewModelDestroyed = true;
            foreach (var vm in subViewModels)
            {
                (vm).OnArtWizViewModelDestroy();
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
