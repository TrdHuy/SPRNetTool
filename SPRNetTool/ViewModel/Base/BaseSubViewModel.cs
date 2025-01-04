using System;

namespace ArtWiz.ViewModel.Base
{
    public abstract class BaseSubViewModel : BaseParentsViewModel
    {
        public BaseViewModel Parents { get; private set; }

        public BaseSubViewModel(BaseParentsViewModel parents)
        {
            Parents = parents;
            parents.RegisterSubViewModel(this);
            if (parents.IsOwnerCreated)
            {
                if (parents.ViewModelOwner == null)
                {
                    throw new Exception("Parents's owner is null. Should not be happened!");
                }
                OnArtWizViewModelOwnerCreate(parents.ViewModelOwner);
            }
        }

        public override void OnArtWizViewModelOwnerCreate(IArtWizViewModelOwner owner)
        {
            base.OnArtWizViewModelOwnerCreate(owner);
        }

        public override void OnArtWizViewModelOwnerDestroy()
        {
            base.OnArtWizViewModelOwnerDestroy();
        }
    }
}
