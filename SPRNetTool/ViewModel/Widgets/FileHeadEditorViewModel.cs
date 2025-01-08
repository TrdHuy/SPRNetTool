using ArtWiz.Domain;
using ArtWiz.Domain.Base;
using ArtWiz.Utils;
using ArtWiz.ViewModel.Base;
using System.ComponentModel;
using System.Windows.Threading;
using WizMachine.Data;

namespace ArtWiz.ViewModel.Widgets
{
    class FileHeadEditorViewModel : BaseSubViewModel, IFileHeadEditorViewModel
    {
        private SprFileHead _sprFileHead;
        private FrameRGBA _sprFrameData;
        private int _currentFrameIndex = 0;
        private int _pixelHeight = 0;
        private int _pixelWidth = 0;
        private bool _isSpr;
        private bool _isEditable;

        [Bindable(true)]
        public SprFileHead FileHead
        {
            get => _sprFileHead;
            set
            {
                _sprFileHead = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public int CurrentFrameIndex
        {
            get => _currentFrameIndex;
            set
            {
                _currentFrameIndex = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public FrameRGBA CurrentFrameData
        {
            get => _sprFrameData;
            set
            {
                _sprFrameData = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public int PixelHeight
        {
            get => _pixelHeight; set
            {
                _pixelHeight = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public int PixelWidth
        {
            get => _pixelWidth; set
            {
                _pixelWidth = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public bool IsSpr
        {
            get => _isSpr; set
            {
                _isSpr = value;
                Invalidate();
            }
        }

        [Bindable(true)]
        public bool IsEditable
        {
            get => _isEditable; set
            {
                _isEditable = value;
                Invalidate();
            }
        }

        public FileHeadEditorViewModel(BaseParentsViewModel parents) : base(parents)
        {
        }
    }
}
