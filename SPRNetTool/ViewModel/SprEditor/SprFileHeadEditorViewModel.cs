using ArtWiz.Domain.Base;
using ArtWiz.Domain;
using System.Windows.Threading;
using WizMachine.Data;
using ArtWiz.ViewModel.Widgets;
using ArtWiz.ViewModel.Base;
using static ArtWiz.Domain.BitmapDisplayMangerChangedArg.BitmapDisplayChangedEvent;
using static ArtWiz.Domain.BitmapDisplayMangerChangedArg.SprAnimationChangedEvent;
using ArtWiz.Utils;

namespace ArtWiz.ViewModel.SprEditor
{
    internal class SprFileHeadEditorViewModel : FileHeadEditorViewModel
    {
        public SprFileHeadEditorViewModel(BaseParentsViewModel parents) : base(parents)
        {
            IsEditable = true;
        }

        public override void OnArtWizViewModelOwnerDestroy()
        {
            base.OnArtWizViewModelOwnerDestroy();
            BitmapDisplayManager.UnregisterObserver(this);
        }

        public override void OnArtWizViewModelOwnerCreate(IArtWizViewModelOwner owner)
        {
            base.OnArtWizViewModelOwnerCreate(owner);
            BitmapDisplayManager.RegisterObserver(this);
        }

        protected override void OnDomainChanged(IDomainChangedArgs args)
        {
            if (IsOwnerDestroyed) return;

            switch (args)
            {
                case BitmapDisplayMangerChangedArg castArgs:

                    if (castArgs.Event.HasFlag(SPR_WORKSPACE_RESET))
                    {
                        FileHead = new SprFileHead();
                        CurrentFrameData = new FrameRGBA();
                    }
                    else
                    {
                        if (castArgs.Event.HasFlag(SPR_FILE_HEAD_CHANGED))
                        {
                            IsSpr = castArgs.CurrentSprFileHead != null;
                            castArgs.CurrentSprFileHead?.Apply(it => FileHead = it.modifiedSprFileHeadCache.ToSprFileHead());
                        }

                        if (castArgs.Event.HasFlag(CURRENT_DISPLAYING_FRAME_INDEX_CHANGED))
                        {
                            CurrentFrameIndex = (int)castArgs.CurrentDisplayingFrameIndex;
                        }

                        if (castArgs.Event.HasFlag(SPR_FRAME_DATA_CHANGED))
                        {
                            CurrentFrameData = castArgs.SprFrameData?.modifiedFrameRGBACache?.toFrameRGBA() ?? new FrameRGBA();
                        }

                        if (castArgs.Event.HasFlag(CURRENT_DISPLAYING_SOURCE_CHANGED) && !IsSpr)
                        {
                            PixelHeight = castArgs.CurrentDisplayingSource?.PixelHeight ?? 0;
                            PixelWidth = castArgs.CurrentDisplayingSource?.PixelWidth ?? 0;
                        }
                    }
                    break;

                case SprAnimationChangedArg castArgs:
                    if (castArgs.Event.HasFlag(IS_PLAYING_ANIMATION_CHANGED))
                    {
                        if (castArgs.IsPlayingAnimation == true)
                        {
                            var dispatcherPriority = DispatcherPriority.Background;
                            if (FileHead.Interval > 20)
                            {
                                dispatcherPriority = DispatcherPriority.Render;
                            }

                            if (IsOwnerDestroyed) return;

                            ViewModelOwner?.ViewDispatcher.Invoke(() =>
                            {
                                CurrentFrameIndex = (int)castArgs.CurrentDisplayingFrameIndex;
                            }, dispatcherPriority);
                        }
                        else if (castArgs.IsPlayingAnimation == false)
                        {
                            CurrentFrameData = castArgs.SprFrameData ?? CurrentFrameData;
                            CurrentFrameIndex = (int)castArgs.CurrentDisplayingFrameIndex;
                        }
                    }
                    break;
            }
        }

    }
}
