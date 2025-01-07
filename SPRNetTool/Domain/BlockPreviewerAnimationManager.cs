using ArtWiz.Domain.Base;
using System;
using System.Threading;
using WizMachine.Data;

namespace ArtWiz.Domain
{
    internal class BlockPreviewerAnimationManager : SprAnimationManager, IBlockPreviewerAnimationManager
    {
        private SprFileHead mCurrentManagingFileHead;
        private FrameRGBA[]? mCurrentManagingFrameData;
        private SemaphoreSlim mAnimationSemaphore = new SemaphoreSlim(1, 1);
        private ISprAnimationCallback? mCurrentObjectRequestToPlayAnimation;

        protected override SprFileHead FileHead => mCurrentManagingFileHead;

        protected override byte[]? GetDecodedBGRAData(uint index)
        {
            if (mCurrentManagingFrameData == null) { return null; }
            if (mCurrentManagingFrameData.Length != mCurrentManagingFrameData.Length)
            {
                throw new InvalidOperationException("Should not be happened");
            }
            return mCurrentManagingFrameData[index].originDecodedBGRAData;
        }

        protected override FrameRGBA? GetFrameData(uint frameIndex)
        {
            if (mCurrentManagingFrameData == null) { return null; }
            if (mCurrentManagingFrameData.Length != mCurrentManagingFrameData.Length)
            {
                throw new InvalidOperationException("Should not be happened");
            }
            return mCurrentManagingFrameData[frameIndex];

        }

        public void SetCurrentSprData(SprFileHead fileHead, FrameRGBA[] frameData)
        {
            mCurrentManagingFileHead = fileHead;
            mCurrentManagingFrameData = frameData;
        }

        public async void StartSprAnimation(ISprAnimationCallback callback)
        {

            if (!DisplayedBitmapSourceCache.IsPlaying
                && FileHead.modifiedSprFileHeadCache.FrameCounts > 1
                && mCurrentObjectRequestToPlayAnimation == null)
            {
                await mAnimationSemaphore.WaitAsync();
                if (CurrentAnimationTask != null)
                {
                    await CurrentAnimationTask;
                }
                DisplayedBitmapSourceCache.IsPlaying = true;

                InitAnimationSourceCacheIfAsynchronous();
                mCurrentObjectRequestToPlayAnimation = callback;
                await PlayAnimation();
            }
        }

        public async void StopSprAnimation(ISprAnimationCallback callback)
        {
            if (DisplayedBitmapSourceCache.IsPlaying
                && FileHead.modifiedSprFileHeadCache.FrameCounts > 1
                && mCurrentObjectRequestToPlayAnimation != null)
            {
                if (callback != mCurrentObjectRequestToPlayAnimation)
                {
                    throw new InvalidOperationException("Should be never happened");
                }
                DisplayedBitmapSourceCache.IsPlaying = false;
                DisplayedBitmapSourceCache.AnimationTokenSource?.Cancel();

                if (CurrentAnimationTask != null)
                {
                    await CurrentAnimationTask;
                }
                mAnimationSemaphore.Release();
                mCurrentObjectRequestToPlayAnimation = null;
            }
        }

        protected override void NotifyChanged(IDomainChangedArgs args)
        {
            mCurrentObjectRequestToPlayAnimation?.OnAnimationEventChange(args);
        }

        public bool IsReadyToPlayAnimation()
        {
            return mCurrentManagingFrameData != null &&
                mCurrentManagingFrameData.Length == mCurrentManagingFileHead.FrameCounts;
        }
    }
}
