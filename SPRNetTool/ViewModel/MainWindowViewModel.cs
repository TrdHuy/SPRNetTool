using ArtWiz.ViewModel.Base;
namespace ArtWiz.ViewModel
{
    internal class MainWindowViewModel : BaseParentsViewModel
    {

        public bool IsDebugMode
        {
            get
            {
                return DeviceConfigManager.IsDebugMode();
            }
        }
    }
}
