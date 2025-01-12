using ArtWiz.Domain.Base;
using ArtWiz.Domain;
using ArtWiz.ViewModel.Base;
using ArtWiz.ViewModel.Widgets;
using System.Windows.Threading;
using System.ComponentModel;
using ArtWiz.Utils;
using static ArtWiz.Domain.BitmapDisplayMangerChangedArg.SprAnimationChangedEvent;

namespace ArtWiz.ViewModel.PakEditor
{
    public class BlockAnimationViewerViewModel : BitmapViewerViewModel, ISprAnimationCallback
    {
        private bool _isPlayingAnimation;

        [Bindable(true)]
        public bool IsPlayingAnimation
        {
            get => _isPlayingAnimation;
            set
            {
                SetNewAnimationState(value);
                Invalidate();
            }
        }

        public BlockAnimationViewerViewModel(BaseParentsViewModel parents) : base(parents)
        {
        }

        private void SetNewAnimationState(bool newState)
        {
            if (Parents is PakBlockItemViewModel vm)
            {
                if (_isPlayingAnimation != newState && vm.IsDataLoaded())
                {
                    _isPlayingAnimation = newState;
                    if (_isPlayingAnimation)
                    {
                        vm.GetSprData(out var sprFileHead, out var frameData);
                        BlockPreviewerAnimationManager.SetCurrentSprData(sprFileHead, frameData);
                        BlockPreviewerAnimationManager.StartSprAnimation(this);
                    }
                    else
                    {
                        BlockPreviewerAnimationManager.StopSprAnimation(this);
                    }
                }
            }

        }

        public void ChangeAnimationState()
        {
            if (IsSpr && _isPlayingAnimation)
            {
                IsPlayingAnimation = false;
                BlockPreviewerAnimationManager.StopSprAnimation(this);
            }
            else if (IsSpr && !_isPlayingAnimation)
            {
                IsPlayingAnimation = true;
                BlockPreviewerAnimationManager.StartSprAnimation(this);
            }
        }

        public void OnAnimationEventChange(object args)
        {
            if (IsOwnerDestroyed) return;

            switch (args)
            {
                case SprAnimationChangedArg castArgs:

                    if (castArgs.Event.HasFlag(SPR_FRAME_DATA_CHANGED))
                    {
                        castArgs.SprFrameData?.Apply(it =>
                        {
                            FrameHeight = it.modifiedFrameRGBACache.frameHeight;
                            FrameWidth = it.modifiedFrameRGBACache.frameWidth;
                            FrameOffX = it.modifiedFrameRGBACache.frameOffX;
                            FrameOffY = it.modifiedFrameRGBACache.frameOffY;
                        });
                    }

                    if (castArgs.Event.HasFlag(IS_PLAYING_ANIMATION_CHANGED) && IsSpr)
                    {
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
    }
}
