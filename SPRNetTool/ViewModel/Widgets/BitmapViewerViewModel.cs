using ArtWiz.Domain;
using ArtWiz.Domain.Base;
using ArtWiz.Utils;
using ArtWiz.ViewModel.Base;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static ArtWiz.Domain.BitmapDisplayMangerChangedArg.ChangedEvent;

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
            BitmapDisplayManager.RegisterObserver(this);
        }

        ~BitmapViewerViewModel()
        {
            BitmapDisplayManager.UnregisterObserver(this);
        }

        protected override void OnDomainChanged(IDomainChangedArgs args)
        {
            if (IsOwnerDestroyed) return;

            switch (args)
            {
                case BitmapDisplayMangerChangedArg castArgs:
                    if (castArgs.Event.HasFlag(SPR_WORKSPACE_RESET))
                    {
                        FrameSource = null;
                        FrameOffX = 0;
                        FrameOffY = 0;
                        FrameHeight = 0;
                        FrameWidth = 0;
                        GlobalHeight = 0;
                        GlobalWidth = 0;
                        GlobalOffY = 0;
                        GlobalOffX = 0;
                    }
                    else if (castArgs.Event.HasFlag(IS_PLAYING_ANIMATION_CHANGED) && IsSpr)
                    {
                        if (castArgs.Event.HasFlag(SPR_FRAME_OFFSET_CHANGED))
                        {
                            castArgs.SprFrameData?.Apply(it =>
                            {
                                FrameOffX = it.modifiedFrameRGBACache.frameOffX;
                                FrameOffY = it.modifiedFrameRGBACache.frameOffY;
                            });
                        }

                        if (castArgs.Event.HasFlag(SPR_FRAME_SIZE_CHANGED))
                        {
                            castArgs.SprFrameData?.Apply(it =>
                            {
                                FrameHeight = it.modifiedFrameRGBACache.frameHeight;
                                FrameWidth = it.modifiedFrameRGBACache.frameWidth;
                            });
                        }


                        if (castArgs.IsPlayingAnimation == true)
                        {
                            var dispatcherPriority = DispatcherPriority.Background;
                            if (castArgs.AnimationInterval > 20)
                            {
                                dispatcherPriority = DispatcherPriority.Render;
                            }
                            if (IsOwnerDestroyed) return;
                            ViewModelOwner?.ViewDispatcher.Invoke(() =>
                            {
                                FrameSource = castArgs.CurrentDisplayingSource;
                            }, dispatcherPriority);
                        }
                        else if (castArgs.IsPlayingAnimation == false)
                        {
                            FrameSource = castArgs.CurrentDisplayingSource;
                        }
                    }
                    else
                    {
                        if (castArgs.Event.HasFlag(SPR_FILE_PALETTE_CHANGED))
                        {
                            // TODO: Consider to notify to update image when palette changed
                        }

                        if (castArgs.Event.HasFlag(SPR_FILE_HEAD_CHANGED))
                        {
                            IsSpr = castArgs.CurrentSprFileHead != null;
                        }

                        if (castArgs.Event.HasFlag(SPR_GLOBAL_OFFSET_CHANGED))
                        {
                            castArgs.CurrentSprFileHead?.Apply(it =>
                            {
                                GlobalOffX = it.modifiedSprFileHeadCache.offX;
                                GlobalOffY = it.modifiedSprFileHeadCache.offY;
                            });
                        }

                        if (castArgs.Event.HasFlag(SPR_GLOBAL_SIZE_CHANGED))
                        {
                            castArgs.CurrentSprFileHead?.Apply(it =>
                            {
                                GlobalHeight = it.modifiedSprFileHeadCache.globalHeight;
                                GlobalWidth = it.modifiedSprFileHeadCache.globalWidth;
                            });
                        }

                        if (castArgs.Event.HasFlag(CURRENT_DISPLAYING_SOURCE_CHANGED))
                        {
                            FrameSource = castArgs.CurrentDisplayingSource;
                            if (!IsSpr)
                            {
                                GlobalOffX = 0;
                                GlobalOffY = 0;
                                FrameOffX = 0;
                                FrameOffY = 0;
                                GlobalHeight = (uint)(castArgs.CurrentDisplayingSource?.PixelHeight ?? 0);
                                GlobalWidth = (uint)(castArgs.CurrentDisplayingSource?.PixelWidth ?? 0);
                                FrameHeight = (uint)(castArgs.CurrentDisplayingSource?.PixelHeight ?? 0);
                                FrameWidth = (uint)(castArgs.CurrentDisplayingSource?.PixelWidth ?? 0);
                            }

                        }

                        if (castArgs.Event.HasFlag(SPR_FRAME_OFFSET_CHANGED))
                        {
                            castArgs.SprFrameData?.Apply(it =>
                            {
                                FrameOffX = it.modifiedFrameRGBACache.frameOffX;
                                FrameOffY = it.modifiedFrameRGBACache.frameOffY;
                            });
                        }

                        if (castArgs.Event.HasFlag(SPR_FRAME_SIZE_CHANGED))
                        {
                            castArgs.SprFrameData?.Apply(it =>
                            {
                                FrameHeight = it.modifiedFrameRGBACache.frameHeight;
                                FrameWidth = it.modifiedFrameRGBACache.frameWidth;
                            });
                        }
                    }
                    break;
            }
        }

        public override void OnArtWizViewModelOwnerDestroy()
        {
            base.OnArtWizViewModelOwnerDestroy();
            BitmapDisplayManager.UnregisterObserver(this);
        }
    }
}
