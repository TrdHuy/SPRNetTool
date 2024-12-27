namespace ArtWiz.ViewModel.Base
{
    public abstract class BaseSubViewModel : BaseParentsViewModel
    {
        protected BaseViewModel Parents;

        public BaseSubViewModel(BaseParentsViewModel parents)
        {
            Parents = parents;
            parents.RegisterSubViewModel(this);
        }


        public override void OnArtWizViewModelOwnerCreate(IArtWizViewModelOwner owner)
        {
            base.OnArtWizViewModelOwnerCreate(owner);
        }

        public override void OnArtWizViewModelDestroy()
        {
            base.OnArtWizViewModelDestroy();
        }
    }
}
