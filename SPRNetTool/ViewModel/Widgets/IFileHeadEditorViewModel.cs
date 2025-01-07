using ArtWiz.ViewModel.Base;
using WizMachine.Data;

namespace ArtWiz.ViewModel.Widgets
{
    public interface IFileHeadEditorViewModel : IArtWizViewModel
    {
        public bool IsSpr { get; set; }
        public bool IsEditable { get; set; }
        public SprFileHead FileHead { get; set; }
        public int CurrentFrameIndex { get; set; }
        public FrameRGBA CurrentFrameData { get; set; }
        public int PixelHeight { get; set; }
        public int PixelWidth { get; set; }
    }
}
