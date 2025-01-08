
namespace ArtWiz.ViewModel.CommandVM
{
    internal interface IPakPageCommand
    {
        void OnAddedPakFileClick(string filePath);
        void OnRemovePakFileClick(object pakFileViewModel);
        void OnSearchPakBlockByPath(string blockPath);
        void OnResetSearchBox();
        void OnExtractCurrentSelectedBlock();
    }
}
