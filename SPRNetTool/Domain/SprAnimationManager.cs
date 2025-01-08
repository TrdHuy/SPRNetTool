using ArtWiz.Data;
using ArtWiz.Domain.Base;
using ArtWiz.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WizMachine.Data;
using static ArtWiz.Domain.BitmapDisplayMangerChangedArg;
using static ArtWiz.Domain.SprAnimationChangedArg.SprAnimationChangedEvent;

namespace ArtWiz.Domain
{
    public abstract class SprAnimationManager : BaseDomain
    {
        protected abstract SprFileHead FileHead { get; }
        protected abstract FrameRGBA? GetFrameData(uint frameIndex);
        protected abstract byte[]? GetDecodedBGRAData(uint index);
        protected class BitmapSourceCache
        {
            public bool IsPlaying { get; set; }
            public bool IsSprImage { get; set; }
            public BitmapSource? DisplayedBitmapSource { get; set; }
            public BitmapSource?[]? AnimationSourceCaching { get; set; }
            public uint? CurrentFrameIndex { get; set; }
            public CancellationTokenSource? AnimationTokenSource { get; set; }
        }

        protected BitmapSourceCache DisplayedBitmapSourceCache { get; } = new BitmapSourceCache();
        protected Task? CurrentAnimationTask { get; set; }

        protected async Task PlayAnimation()
        {
            DisplayedBitmapSourceCache.AnimationTokenSource = new CancellationTokenSource();
            CurrentAnimationTask = Task.Run(async () =>
            {
                Stopwatch stopwatch = new Stopwatch();

                uint frameIndex = DisplayedBitmapSourceCache.CurrentFrameIndex ?? 0;
                DisplayedBitmapSourceCache.CurrentFrameIndex = frameIndex;

                while (DisplayedBitmapSourceCache.IsPlaying && DisplayedBitmapSourceCache.AnimationSourceCaching != null)
                {
                    stopwatch.Restart();
                    DisplayedBitmapSourceCache.AnimationSourceCaching[frameIndex] =
                    DisplayedBitmapSourceCache.AnimationSourceCaching[frameIndex].IfNullThenLet(() =>
                        CreateBitmapSourceFromDecodedFrameData(frameIndex, out _));
                    DisplayedBitmapSourceCache.DisplayedBitmapSource = DisplayedBitmapSourceCache.AnimationSourceCaching[frameIndex];

                    NotifyChanged(new SprAnimationChangedArg(
                        changedEvent: IS_PLAYING_ANIMATION_CHANGED
                            | CURRENT_DISPLAYING_SOURCE_CHANGED
                            | CURRENT_DISPLAYING_FRAME_INDEX_CHANGED
                            | SPR_FRAME_DATA_CHANGED,
                        currentDisplayingSource: DisplayedBitmapSourceCache.DisplayedBitmapSource,
                        isPlayingAnimation: true,
                        currentDisplayFrameIndex: frameIndex,
                        animationInterval: FileHead.modifiedSprFileHeadCache.Interval,
                        sprFrameData: GetFrameData(frameIndex)));
                    DisplayedBitmapSourceCache.CurrentFrameIndex++;
                    frameIndex++;
                    if (frameIndex == FileHead.modifiedSprFileHeadCache.FrameCounts)
                    {
                        frameIndex = 0;
                        DisplayedBitmapSourceCache.CurrentFrameIndex = 0;
                    }
                    int delayTime = FileHead.modifiedSprFileHeadCache.Interval - (int)stopwatch.ElapsedMilliseconds;
                    if (delayTime > 0)
                    {
                        try
                        {
                            await Task.Delay(delayTime, DisplayedBitmapSourceCache.AnimationTokenSource.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            DisplayedBitmapSourceCache.IsPlaying = false;
                            break;
                        }
                    }
                }

                if (frameIndex > 0)
                {
                    DisplayedBitmapSourceCache.CurrentFrameIndex--;
                    frameIndex--;
                }
                else if (frameIndex == 0)
                {
                    frameIndex = (uint)(FileHead.modifiedSprFileHeadCache.FrameCounts - 1);
                    DisplayedBitmapSourceCache.CurrentFrameIndex = frameIndex;
                }

                DisplayedBitmapSourceCache.AnimationTokenSource = null;

                NotifyChanged(new SprAnimationChangedArg(
                        changedEvent: IS_PLAYING_ANIMATION_CHANGED
                            | CURRENT_DISPLAYING_SOURCE_CHANGED
                            | CURRENT_DISPLAYING_FRAME_INDEX_CHANGED
                            | SPR_FRAME_DATA_CHANGED,
                        currentDisplayingSource: DisplayedBitmapSourceCache.DisplayedBitmapSource,
                        isPlayingAnimation: false,
                        currentDisplayFrameIndex: frameIndex,
                        sprFrameData: GetFrameData(frameIndex)));
            });
            await CurrentAnimationTask;
        }

        protected void InitAnimationSourceCacheIfAsynchronous(bool force = false)
        {
            if (force ||
                DisplayedBitmapSourceCache.AnimationSourceCaching == null ||
                DisplayedBitmapSourceCache.AnimationSourceCaching.Length != FileHead.modifiedSprFileHeadCache.FrameCounts)
            {
                DisplayedBitmapSourceCache.AnimationSourceCaching =
                    new BitmapSource?[FileHead.modifiedSprFileHeadCache.FrameCounts];
            }
        }

        protected virtual BitmapSource? CreateBitmapSourceFromDecodedFrameData(uint frameIndex,
            out List<(Color, Color, int)> rgbColorChangedArgs)
        {
            var frameInfo = GetFrameData(frameIndex) ?? throw new Exception();
            rgbColorChangedArgs = new List<(Color, Color, int)>();
            return GetDecodedBGRAData(frameIndex)?
                .Let((it) => ArtWiz.Utils.BitmapUtil.GetBitmapFromRGBArray(it
                    , frameInfo.frameWidth
                    , frameInfo.frameHeight, PixelFormats.Bgra32))
                .Also((it) => it.Freeze());
        }
    }

    public class SprAnimationChangedArg : IDomainChangedArgs
    {

        public record SprAnimationChangedEvent : StateFlag<SprAnimationChangedEvent>
        {
            public static readonly SprAnimationChangedEvent CURRENT_DISPLAYING_SOURCE_CHANGED = new SprAnimationChangedEvent(0b1);
            public static readonly SprAnimationChangedEvent IS_PLAYING_ANIMATION_CHANGED = new SprAnimationChangedEvent(0b10);
            public static readonly SprAnimationChangedEvent SPR_FILE_HEAD_CHANGED = new SprAnimationChangedEvent(0b100);
            public static readonly SprAnimationChangedEvent CURRENT_DISPLAYING_FRAME_INDEX_CHANGED = new SprAnimationChangedEvent(0b1000);
            public static readonly SprAnimationChangedEvent SPR_FRAME_DATA_CHANGED = new SprAnimationChangedEvent(0b10000);

            public SprAnimationChangedEvent(int value) : base(value)
            {
            }
        }

        public SprAnimationChangedEvent Event { get; private set; }
        public BitmapSource? CurrentDisplayingSource { get; private set; }
        public bool? IsPlayingAnimation { get; private set; }
        public uint CurrentDisplayingFrameIndex { get; private set; }
        public FrameRGBA? SprFrameData { get; private set; }
        public uint SprFrameCount { get; private set; }
        public uint AnimationInterval { get; private set; }

        public SprAnimationChangedArg(SprAnimationChangedEvent changedEvent, BitmapSource? currentDisplayingSource = null,
            bool? isPlayingAnimation = null,
            uint currentDisplayFrameIndex = 0,
            FrameRGBA? sprFrameData = null,
            uint animationInterval = 0)
        {
            CurrentDisplayingSource = currentDisplayingSource;
            IsPlayingAnimation = isPlayingAnimation;
            CurrentDisplayingFrameIndex = currentDisplayFrameIndex;
            SprFrameData = sprFrameData;
            AnimationInterval = animationInterval;
            Event = changedEvent;
        }
    }

}
