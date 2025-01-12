using ArtWiz.ViewModel.Base;
using System.Windows.Media;

namespace ArtWiz.ViewModel.Widgets
{
    public interface IFramePreviewerViewModel : IArtWizViewModel
    {
        ImageSource? PreviewImageSource { get; set; }
        int FrameHeight { get; set; }
        int FrameWidth { get; set; }
        int GlobalWidth { get; set; }
        int GlobalHeight { get; set; }
        int FrameOffsetX { get; set; }
        int FrameOffsetY { get; set; }
        string Index { get; set; }
        int GlobalOffsetX { get; set; }
        int GlobalOffsetY { get; set; }
        bool IsHighlighted { get; set; }
    }
}
