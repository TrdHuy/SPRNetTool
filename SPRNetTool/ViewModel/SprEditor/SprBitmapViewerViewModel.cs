using ArtWiz.Domain.Base;
using ArtWiz.Domain;
using ArtWiz.ViewModel.Base;
using ArtWiz.ViewModel.Widgets;
using System.Windows.Threading;
using static ArtWiz.Domain.BitmapDisplayMangerChangedArg.SprAnimationChangedEvent;
using static ArtWiz.Domain.BitmapDisplayMangerChangedArg.BitmapDisplayChangedEvent;
using ArtWiz.Utils;

namespace ArtWiz.ViewModel.SprEditor
{
    internal class SprBitmapViewerViewModel : BitmapViewerViewModel
    {

        public SprBitmapViewerViewModel(BaseParentsViewModel parents) : base(parents)
        {
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
                case SprAnimationChangedArg castArgs:
                    if (castArgs.Event.HasFlag(IS_PLAYING_ANIMATION_CHANGED) && IsSpr)
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
                    break;
            }
        }

        public override void OnArtWizViewModelOwnerCreate(IArtWizViewModelOwner owner)
        {
            base.OnArtWizViewModelOwnerCreate(owner);
            BitmapDisplayManager.RegisterObserver(this);
        }

        public override void OnArtWizViewModelOwnerDestroy()
        {
            base.OnArtWizViewModelOwnerDestroy();
            BitmapDisplayManager.UnregisterObserver(this);
        }
    }
}
