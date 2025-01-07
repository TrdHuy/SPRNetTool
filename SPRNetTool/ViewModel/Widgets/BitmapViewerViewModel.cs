using ArtWiz.Domain.Base;
using ArtWiz.Domain;
using ArtWiz.ViewModel.Base;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ArtWiz.ViewModel.Widgets
{
    public class BitmapViewerViewModel : BaseSubViewModel, IBitmapViewerViewModel
    {
        private BitmapSource? _frameSource;
        public uint _globalWidth = 0;
        public uint _globalHeight = 0;
        public int _globalOffX = 0;
        public int _globalOffY = 0;
        public uint _frameHeight = 0;
        public uint _frameWidth = 0;
        public int _frameOffX = 0;
        public int _frameOffY = 0;
        public bool _isSpr;

        public BitmapSource? FrameSource
        {
            get => _frameSource;
            set
            {
                _frameSource = value;
                Invalidate();
            }
        }
        public uint GlobalWidth
        {
            get => _globalWidth; set
            {
                _globalWidth = value;
                Invalidate();
            }
        }
        public uint GlobalHeight
        {
            get => _globalHeight; set
            {
                _globalHeight = value;
                Invalidate();
            }
        }
        public int GlobalOffX
        {
            get => _globalOffX; set
            {
                _globalOffX = value;
                Invalidate();
            }
        }
        public int GlobalOffY
        {
            get => _globalOffY; set
            {
                _globalOffY = value;
                Invalidate();
            }
        }
        public uint FrameHeight
        {
            get => _frameHeight; set
            {
                _frameHeight = value;
                Invalidate();
            }
        }
        public uint FrameWidth
        {
            get => _frameWidth; set
            {
                _frameWidth = value;
                Invalidate();
            }
        }
        public int FrameOffX
        {
            get => _frameOffX; set
            {
                _frameOffX = value;
                Invalidate();
            }
        }
        public int FrameOffY
        {
            get => _frameOffY; set
            {
                _frameOffY = value;
                Invalidate();
            }
        }
        public bool IsSpr
        {
            get => _isSpr;
            set
            {
                _isSpr = value;
                Invalidate();
            }
        }

        public BitmapViewerViewModel(BaseParentsViewModel parents) : base(parents)
        {
        }
    }

    
}
