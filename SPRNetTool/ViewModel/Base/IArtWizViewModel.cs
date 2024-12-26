namespace ArtWiz.ViewModel.Base
{
    public interface IArtWizViewModel
    {
        void OnArtWizViewModelOwnerCreate(IArtWizViewModelOwner owner);

        void OnArtWizViewModelDestroy();

    }
}
