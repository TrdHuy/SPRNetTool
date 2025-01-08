using ArtWiz.Data;
using ArtWiz.Domain.Base;
using ArtWiz.Domain.Utils;
using ArtWiz.LogUtil;
using ArtWiz.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WizMachine.Data;
using static ArtWiz.Domain.BitmapDisplayMangerChangedArg.SprAnimationChangedEvent;
using static ArtWiz.Domain.BitmapDisplayMangerChangedArg.BitmapDisplayChangedEvent;
using static ArtWiz.Domain.SprFrameCollectionChangedArg.ChangedEvent;
using static ArtWiz.Domain.SprPaletteChangedArg.ChangedEvent;

namespace ArtWiz.Domain
{
    // This domain work only for Spr editor page
    public class SprEditorBitmapDisplayManager : SprAnimationManager, ISprEditorBitmapDisplayManager
    {
        private static Logger logger = new Logger("BitmapDisplayManager");
        private ISprWorkManager? sprWorkManager;
        protected ISprWorkManager SprWorkManager
        {
            get
            {
                return sprWorkManager ??
                    IDomainAccessors
                    .DomainContext
                    .GetDomain<ISprWorkManager>()
                    .Also(it => sprWorkManager = it);
            }
        }

        protected override SprFileHead FileHead => SprWorkManager.FileHead;


        #region public interface
        uint ISprEditorBitmapDisplayManager.GetCurrentDisplayFrameIndex()
        {
            return DisplayedBitmapSourceCache.CurrentFrameIndex ?? 0;
        }

        void ISprEditorBitmapDisplayManager.ResetSprWorkSpace()
        {
            SprWorkManager.ResetWorkSpace();
            if (DisplayedBitmapSourceCache.IsSprImage)
            {
                DisplayedBitmapSourceCache.IsPlaying = false;
                DisplayedBitmapSourceCache.CurrentFrameIndex = 0;
                DisplayedBitmapSourceCache.AnimationTokenSource = null;
                DisplayedBitmapSourceCache.AnimationSourceCaching = null;
                NotifyChanged(new BitmapDisplayMangerChangedArg(
                        changedEvent: SPR_WORKSPACE_RESET));
            }
        }

        void ISprEditorBitmapDisplayManager.SetNewColorToPalette(uint paletteIndex, Color newColor)
        {
            if (!SprWorkManager.IsWorkSpaceEmpty)
            {
                var currentFrameIndex = DisplayedBitmapSourceCache.CurrentFrameIndex ?? 0;
                if (SprWorkManager.GetFrameData(currentFrameIndex)?.isInsertedFrame ?? false)
                {
                    SprWorkManager.SetNewColorToInsertedFramePalette((int)currentFrameIndex,
                        paletteIndex,
                        newColor.R,
                        newColor.G,
                        newColor.B);
                }
                else
                {
                    SprWorkManager.SetNewColorToGlobalPalette(paletteIndex, newColor.R, newColor.G, newColor.B);
                }

                if (InvalidateDisplayBitmapSourceCache(DisplayedBitmapSourceCache.CurrentFrameIndex ?? 0))
                {
                    NotifyChanged(new BitmapDisplayMangerChangedArg(
                        changedEvent: CURRENT_DISPLAYING_SOURCE_CHANGED
                         | SPR_FILE_PALETTE_CHANGED,
                         currentDisplayingSource: DisplayedBitmapSourceCache.DisplayedBitmapSource,
                         paletteChangedArg: new SprPaletteChangedArg(
                             changedEvent: COLOR_CHANGED,
                             colorChangedIndex: paletteIndex,
                             newColor: newColor)));
                }
            }
        }

        void ISprEditorBitmapDisplayManager.ChangeCurrentDisplayMode(bool isSpr)
        {
            if (isSpr && !SprWorkManager.IsWorkSpaceEmpty)
            {
                var frameIndex = DisplayedBitmapSourceCache.CurrentFrameIndex ?? 0;
                DisplayedBitmapSourceCache.IsSprImage = true;
                DisplayedBitmapSourceCache.AnimationSourceCaching?[frameIndex].IfNullThenLet(() =>
                    CreateBitmapSourceFromDecodedFrameData(frameIndex, out _));
                DisplayedBitmapSourceCache.DisplayedBitmapSource =
                    DisplayedBitmapSourceCache.AnimationSourceCaching?[frameIndex];

                NotifyChanged(new BitmapDisplayMangerChangedArg(
                   changedEvent: CURRENT_DISPLAYING_SOURCE_CHANGED
                       | SPR_FILE_HEAD_CHANGED
                       | SPR_FRAME_DATA_CHANGED
                       | SPR_FRAME_COLLECTION_CHANGED
                       | SPR_FRAME_SIZE_CHANGED
                       | SPR_FRAME_OFFSET_CHANGED
                       | SPR_GLOBAL_SIZE_CHANGED
                       | SPR_GLOBAL_OFFSET_CHANGED,
                   currentDisplayingSource: DisplayedBitmapSourceCache.DisplayedBitmapSource,
                   sprFileHead: SprWorkManager.FileHead,
                   sprFrameData: SprWorkManager.GetFrameData(frameIndex),
                   sprFrameCollectionChangedArg: new SprFrameCollectionChangedArg(changedEvent: TOTAL_FRAME_COUNT_CHANGED,
                       frameCount: (uint)(DisplayedBitmapSourceCache.AnimationSourceCaching?.Length ?? 0))));
            }
            else if (!isSpr)
            {
                // TODO: Implement change jpg here
            }
        }

        protected override BitmapSource? CreateBitmapSourceFromDecodedFrameData(uint frameIndex,
            out List<(Color, Color, int)> rgbColorChangedArgs)
        {
            var frameInfo = SprWorkManager.GetFrameData(frameIndex) ?? throw new Exception();

            return SprWorkManager.GetDecodedBGRAData(frameIndex,
                out rgbColorChangedArgs)?
                .Let((it) => ArtWiz.Utils.BitmapUtil.GetBitmapFromRGBArray(it
                    , frameInfo.frameWidth
                    , frameInfo.frameHeight, PixelFormats.Bgra32))
                .Also((it) => it.Freeze());
        }

        bool ISprEditorBitmapDisplayManager.InsertFrame(uint frameIndex, BitmapSource bmpSource)
        {
            return InsertBimapSourceToSprWorkSpace(frameIndex, bmpSource);
        }

        bool ISprEditorBitmapDisplayManager.InsertFrame(uint frameIndex, string filePath)
        {
            var bitmapSource = this.LoadBitmapFromFile(filePath, isFreeze: true)
                ?? throw new Exception($"Failed to load bitmap from path {filePath}.");
            return InsertBimapSourceToSprWorkSpace(frameIndex, bitmapSource, filePath);
        }

        bool ISprEditorBitmapDisplayManager.DeleteFrame(uint frameIndex)
        {
            if (!DisplayedBitmapSourceCache.IsSprImage) return false;
            return SprWorkManager.RemoveFrame(frameIndex).Also(success =>
            {
                if (success)
                {
                    var bmpSrc = new BitmapSource?[SprWorkManager.FileHead.modifiedSprFileHeadCache.FrameCounts];
                    for (int i = 0, j = 0; i < SprWorkManager.FileHead.modifiedSprFileHeadCache.FrameCounts + 1; i++)
                    {
                        if (i != frameIndex)
                        {
                            bmpSrc[j++] = DisplayedBitmapSourceCache.AnimationSourceCaching?[i];
                        }
                    }
                    DisplayedBitmapSourceCache.AnimationSourceCaching = bmpSrc;

                    if (DisplayedBitmapSourceCache.CurrentFrameIndex == frameIndex)
                    {
                        var newFrameIndex = frameIndex == SprWorkManager.FileHead.modifiedSprFileHeadCache.FrameCounts ? frameIndex - 1 : frameIndex;

                        if (DisplayedBitmapSourceCache.AnimationSourceCaching.Length > 0)
                        {
                            DisplayedBitmapSourceCache.AnimationSourceCaching[newFrameIndex] =
                            DisplayedBitmapSourceCache.AnimationSourceCaching[newFrameIndex].IfNullThenLet(() =>
                               CreateBitmapSourceFromDecodedFrameData(newFrameIndex, out _));
                            DisplayedBitmapSourceCache.DisplayedBitmapSource = DisplayedBitmapSourceCache.AnimationSourceCaching[newFrameIndex];
                            DisplayedBitmapSourceCache.CurrentFrameIndex = newFrameIndex;
                        }
                        else
                        {
                            DisplayedBitmapSourceCache.DisplayedBitmapSource = null;
                            DisplayedBitmapSourceCache.CurrentFrameIndex = 0;
                        }

                        NotifyChanged(new BitmapDisplayMangerChangedArg(
                            changedEvent: CURRENT_DISPLAYING_FRAME_INDEX_CHANGED
                            | CURRENT_DISPLAYING_SOURCE_CHANGED,
                            currentDisplayingSource: DisplayedBitmapSourceCache.DisplayedBitmapSource,
                            currentDisplayFrameIndex: newFrameIndex));
                    }

                    NotifyChanged(new BitmapDisplayMangerChangedArg(
                        changedEvent: SPR_FILE_HEAD_CHANGED | SPR_FRAME_COLLECTION_CHANGED,
                        sprFileHead: SprWorkManager.FileHead,
                        sprFrameCollectionChangedArg: new SprFrameCollectionChangedArg(
                            changedEvent: FRAME_REMOVED,
                            oldFrameIndex: (int)frameIndex
                        )
                    ));
                }
            });
        }

        bool ISprEditorBitmapDisplayManager.SwitchFrame(uint frameIndex1, uint frameIndex2)
        {
            if (!DisplayedBitmapSourceCache.IsSprImage) return false;

            return SprWorkManager.SwitchFrame(frameIndex1, frameIndex2).Also(success =>
            {
                if (success)
                {
                    DisplayedBitmapSourceCache.AnimationSourceCaching?.Apply(it =>
                    {
                        var tempCache = it[frameIndex2];
                        it[frameIndex2] = it[frameIndex1];
                        it[frameIndex1] = tempCache;
                    });

                    if (DisplayedBitmapSourceCache.CurrentFrameIndex == frameIndex1)
                    {
                        DisplayedBitmapSourceCache.CurrentFrameIndex = frameIndex2;
                        NotifyChanged(new BitmapDisplayMangerChangedArg(
                            changedEvent: CURRENT_DISPLAYING_FRAME_INDEX_CHANGED,
                            currentDisplayFrameIndex: frameIndex2));
                    }
                    else if (DisplayedBitmapSourceCache.CurrentFrameIndex == frameIndex2)
                    {
                        DisplayedBitmapSourceCache.CurrentFrameIndex = frameIndex1;
                        NotifyChanged(new BitmapDisplayMangerChangedArg(
                            changedEvent: CURRENT_DISPLAYING_FRAME_INDEX_CHANGED,
                            currentDisplayFrameIndex: frameIndex1));
                    }

                    NotifyChanged(new BitmapDisplayMangerChangedArg(changedEvent: SPR_FRAME_COLLECTION_CHANGED,
                        sprFrameCollectionChangedArg: new SprFrameCollectionChangedArg(
                            changedEvent: FRAME_SWITCHED,
                            frameSwitch1Index: frameIndex1,
                            frameSwitch2Index: frameIndex2)));
                }
            });
        }

        void ISprEditorBitmapDisplayManager.SetSprInterval(ushort interval)
        {
            SprWorkManager.SetSprInterval((ushort)interval);
            NotifyChanged(new BitmapDisplayMangerChangedArg(
                changedEvent: SPR_FILE_HEAD_CHANGED,
                sprFileHead: SprWorkManager.FileHead));
        }

        void ISprEditorBitmapDisplayManager.SetSprGlobalSize(ushort width, ushort height)
        {
            if (!DisplayedBitmapSourceCache.IsSprImage) return;

            SprWorkManager.SetGlobalSize(width, height);

            uint index = DisplayedBitmapSourceCache.CurrentFrameIndex ?? 0;

            if (InvalidateDisplayBitmapSourceCache(index))
            {
                NotifyChanged(new BitmapDisplayMangerChangedArg(
                    changedEvent: SPR_FILE_HEAD_CHANGED
                        | SPR_GLOBAL_SIZE_CHANGED,
                    sprFileHead: SprWorkManager.FileHead));
            }
        }

        void ISprEditorBitmapDisplayManager.SetSprGlobalOffset(short offX, short offY)
        {
            if (!DisplayedBitmapSourceCache.IsSprImage) return;

            SprWorkManager.SetGlobalOffset(offX, offY);

            uint index = DisplayedBitmapSourceCache.CurrentFrameIndex ?? 0;

            if (InvalidateDisplayBitmapSourceCache(index))
            {
                NotifyChanged(new BitmapDisplayMangerChangedArg(
                    changedEvent: SPR_FILE_HEAD_CHANGED
                        | SPR_GLOBAL_OFFSET_CHANGED,
                    sprFileHead: SprWorkManager.FileHead));
            }
        }

        void ISprEditorBitmapDisplayManager.SetCurrentlyDisplayedSprFrameIndex(uint index)
        {
            if (index < 0 || index >= SprWorkManager.FileHead.modifiedSprFileHeadCache.FrameCounts) return;
            var currentFrameIndex = DisplayedBitmapSourceCache.CurrentFrameIndex ?? 0;
            var isShouldNotifyPaletteChanged =
                SprWorkManager.GetFrameData(index)?.isInsertedFrame == true
                || (SprWorkManager.GetFrameData(currentFrameIndex)?.isInsertedFrame == true
                    && SprWorkManager.GetFrameData(index)?.isInsertedFrame == false);
            InitAnimationSourceCacheIfAsynchronous();
            if (InvalidateDisplayBitmapSourceCache(index))
            {
                var changeEvent = CURRENT_DISPLAYING_SOURCE_CHANGED
                     | CURRENT_DISPLAYING_FRAME_INDEX_CHANGED
                     | SPR_FRAME_DATA_CHANGED
                     | SPR_FRAME_SIZE_CHANGED
                     | SPR_FRAME_OFFSET_CHANGED;
                var palette = SprWorkManager.GetFrameData(index)?.isInsertedFrame == true ?
                    SprWorkManager.GetFrameData(index)?
                                .modifiedFrameRGBACache
                                .GetFramePaletteData() : SprWorkManager.PaletteData;
                if (isShouldNotifyPaletteChanged)
                {
                    changeEvent |= SPR_FILE_PALETTE_CHANGED;
                }

                NotifyChanged(new BitmapDisplayMangerChangedArg(
                    changedEvent: changeEvent,
                     currentDisplayingSource: DisplayedBitmapSourceCache.DisplayedBitmapSource,
                     currentDisplayFrameIndex: index,
                     sprFrameData: SprWorkManager.GetFrameData(index),
                     paletteChangedArg: isShouldNotifyPaletteChanged ? new SprPaletteChangedArg(
                         changedEvent: NEWLY_ADDED,
                         palette: palette) : null));
            }

        }

        void ISprEditorBitmapDisplayManager.SetCurrentlyDisplayedFrameSize(ushort frameWidth, ushort frameHeight)
        {
            InitAnimationSourceCacheIfAsynchronous();
            uint index = DisplayedBitmapSourceCache.CurrentFrameIndex ?? 0;
            SprWorkManager.SetFrameSize(frameWidth, frameHeight, index);

            if (InvalidateDisplayBitmapSourceCache(index))
            {
                NotifyChanged(new BitmapDisplayMangerChangedArg(
                    changedEvent: SPR_FRAME_DATA_CHANGED
                        | SPR_FRAME_SIZE_CHANGED,
                    sprFrameData: SprWorkManager.GetFrameData(index)));
            }
        }

        void ISprEditorBitmapDisplayManager.SetCurrentlyDisplayedFrameOffset(short frameOffX, short frameOffY)
        {
            InitAnimationSourceCacheIfAsynchronous();
            uint index = DisplayedBitmapSourceCache.CurrentFrameIndex ?? 0;
            SprWorkManager.SetFrameOffset(frameOffY, frameOffX, index);

            if (InvalidateDisplayBitmapSourceCache(index))
            {
                NotifyChanged(new BitmapDisplayMangerChangedArg(
                    changedEvent: SPR_FRAME_DATA_CHANGED
                        | SPR_FRAME_OFFSET_CHANGED,
                    sprFrameData: SprWorkManager.GetFrameData(index)));
            }
        }

        async void ISprEditorBitmapDisplayManager.StartSprAnimation()
        {
            if (!DisplayedBitmapSourceCache.IsPlaying && DisplayedBitmapSourceCache.IsSprImage
                && SprWorkManager.FileHead.modifiedSprFileHeadCache.FrameCounts > 1)
            {
                DisplayedBitmapSourceCache.IsPlaying = true;

                InitAnimationSourceCacheIfAsynchronous();

                await PlayAnimation();
            }
        }

        void ISprEditorBitmapDisplayManager.StopSprAnimation()
        {
            if (DisplayedBitmapSourceCache.IsPlaying && DisplayedBitmapSourceCache.IsSprImage
                && SprWorkManager.FileHead.modifiedSprFileHeadCache.FrameCounts > 1)
            {
                DisplayedBitmapSourceCache.IsPlaying = false;
                DisplayedBitmapSourceCache.AnimationTokenSource?.Cancel();
            }
        }

        void ISprEditorBitmapDisplayManager.OpenBitmapFromFile(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath).ToLower();
            if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png")
            {
                DisplayedBitmapSourceCache.DisplayedBitmapSource = this.LoadBitmapFromFile(filePath)?.Let(it =>
                {
                    var extractedData = this.ConvertBitmapSourceToByteArray(it);
                    var extractedSource = ArtWiz.Utils.BitmapUtil.GetBitmapFromRGBArray(extractedData,
                        it.PixelWidth,
                        it.PixelHeight,
                        it.Format);
                    extractedSource.Freeze();
                    return extractedSource;
                })
                .Also((it) =>
                {

                    DisplayedBitmapSourceCache.IsSprImage = false;
                    DisplayedBitmapSourceCache.IsPlaying = false;
                    DisplayedBitmapSourceCache.CurrentFrameIndex = null;
                });
                NotifyChanged(new BitmapDisplayMangerChangedArg(
                   changedEvent: CURRENT_DISPLAYING_SOURCE_CHANGED
                        | SPR_FILE_HEAD_CHANGED
                        | SPR_FRAME_DATA_CHANGED,
                   currentDisplayingSource: DisplayedBitmapSourceCache.DisplayedBitmapSource,
                   sprFileHead: null,
                   isPlayingAnimation: false));
                return;
            }
            else if (fileExtension == ".spr")
            {
                DisplayedBitmapSourceCache.DisplayedBitmapSource = OpenSprFile(filePath)?.Also((it) =>
                {
                    InitAnimationSourceCacheIfAsynchronous(force: true);
                    Debug.Assert(DisplayedBitmapSourceCache.AnimationSourceCaching != null);
                    DisplayedBitmapSourceCache.AnimationSourceCaching[0] = it;

                    DisplayedBitmapSourceCache.CurrentFrameIndex = 0;
                    DisplayedBitmapSourceCache.IsSprImage = true;
                });
                NotifyChanged(new BitmapDisplayMangerChangedArg(
                    changedEvent: CURRENT_DISPLAYING_SOURCE_CHANGED
                        | CURRENT_DISPLAYING_FRAME_INDEX_CHANGED
                        | SPR_FILE_HEAD_CHANGED
                        | SPR_FRAME_DATA_CHANGED
                        | SPR_FRAME_COLLECTION_CHANGED
                        | SPR_FILE_PALETTE_CHANGED
                        | SPR_GLOBAL_SIZE_CHANGED
                        | SPR_GLOBAL_OFFSET_CHANGED
                        | SPR_FRAME_SIZE_CHANGED
                        | SPR_FRAME_OFFSET_CHANGED,
                    currentDisplayingSource: DisplayedBitmapSourceCache.DisplayedBitmapSource,
                    sprFileHead: SprWorkManager.FileHead,
                    sprFrameData: SprWorkManager.GetFrameData(0),
                    currentDisplayFrameIndex: 0,
                    paletteChangedArg: new SprPaletteChangedArg(
                        changedEvent: NEWLY_ADDED,
                        palette: SprWorkManager.PaletteData),
                    sprFrameCollectionChangedArg: new SprFrameCollectionChangedArg(
                        changedEvent: TOTAL_FRAME_COUNT_CHANGED,
                        frameCount: (uint)(DisplayedBitmapSourceCache.AnimationSourceCaching?.Length ?? 0))));
                return;
            }
        }

        #endregion

        private BitmapSource? OpenSprFile(string filePath)
        {
            if (SprWorkManager.InitWorkManagerFromSprFile(filePath))
            {
                return CreateBitmapSourceFromDecodedFrameData(0, out _);
            }
            return null;
        }


        private bool InsertBimapSourceToSprWorkSpace(uint frameIndex, BitmapSource bitmapSource, string? filePath = null)
        {
            this.CountColors(
                    bitmapSource
                    , out long argbCount
                    , out long rgbCount
                    , out _
                    , out HashSet<Color> rgbSrc);
            if (argbCount > rgbCount && rgbCount <= 256)
            {

            }
            else
            {
                bitmapSource = (this as ISprEditorBitmapDisplayManager).OptimzeImageColorNA256(bitmapSource)
                   ?? throw new Exception($"Failed to optimize image colors.");
            }

            var palettePixelArray = this.ConvertBitmapSourceToPaletteColorArray(bitmapSource,
                out Dictionary<Color, long> argbCountableSource,
                out Dictionary<Color, long> rgbCountableSource,
                out Palette palette,
                out byte[] bgraBytesData,
                out Dictionary<int, List<long>> paletteColorIndexToPixelIndexMap)
                ?? (filePath != null ?
                    throw new Exception($"Failed to load bitmap from path {filePath}") :
                    throw new Exception($"Failed to load bitmap"));

            var isShouldNotifyGlobalSizeChange = SprWorkManager.IsWorkSpaceEmpty;

            if (SprWorkManager.InsertFrame(frameIndex
                , (ushort)bitmapSource.PixelWidth
                , (ushort)bitmapSource.PixelHeight
                , palettePixelArray
                , bgraBytesData
                , palette
                , rgbCountableSource
                , paletteColorIndexToPixelIndexMap))
            {
                // Update current displaying bitmap
                var bmpSrc = new BitmapSource?[SprWorkManager.FileHead.modifiedSprFileHeadCache.FrameCounts];
                for (int i = 0, j = 0; i < SprWorkManager.FileHead.modifiedSprFileHeadCache.FrameCounts; i++)
                {
                    if (i < frameIndex)
                    {
                        bmpSrc[j++] = DisplayedBitmapSourceCache.AnimationSourceCaching?[i];
                    }
                    else if (i == frameIndex)
                    {
                        bmpSrc[j++] = null;
                    }
                    else if (i > frameIndex)
                    {
                        bmpSrc[j++] = DisplayedBitmapSourceCache.AnimationSourceCaching?[i - 1];
                    }
                }
                DisplayedBitmapSourceCache.AnimationSourceCaching = bmpSrc;
                DisplayedBitmapSourceCache.AnimationSourceCaching[frameIndex]
                    = DisplayedBitmapSourceCache.AnimationSourceCaching[frameIndex].IfNullThenLet(() =>
                           CreateBitmapSourceFromDecodedFrameData(frameIndex, out _));
                DisplayedBitmapSourceCache.DisplayedBitmapSource = DisplayedBitmapSourceCache.AnimationSourceCaching[frameIndex];
                DisplayedBitmapSourceCache.CurrentFrameIndex = frameIndex;

                var changeEvent = CURRENT_DISPLAYING_FRAME_INDEX_CHANGED
                            | CURRENT_DISPLAYING_SOURCE_CHANGED
                            | SPR_FILE_HEAD_CHANGED
                            | SPR_FILE_PALETTE_CHANGED
                            | SPR_FRAME_DATA_CHANGED
                            | SPR_FRAME_SIZE_CHANGED
                            | SPR_FRAME_OFFSET_CHANGED
                            | SPR_FRAME_COLLECTION_CHANGED;
                if (isShouldNotifyGlobalSizeChange)
                {
                    changeEvent |= SPR_GLOBAL_OFFSET_CHANGED
                        | SPR_GLOBAL_SIZE_CHANGED;

                }
                NotifyChanged(new BitmapDisplayMangerChangedArg(
                            changedEvent: changeEvent,
                            sprFileHead: SprWorkManager.FileHead,
                            currentDisplayingSource: DisplayedBitmapSourceCache.DisplayedBitmapSource,
                            currentDisplayFrameIndex: frameIndex,
                            sprFrameData: SprWorkManager.GetFrameData(frameIndex),
                            paletteChangedArg: new SprPaletteChangedArg(
                                changedEvent: NEWLY_ADDED,
                                palette: SprWorkManager
                                .GetFrameData(DisplayedBitmapSourceCache.CurrentFrameIndex ?? 0)?
                                .modifiedFrameRGBACache
                                .GetFramePaletteData()),
                            sprFrameCollectionChangedArg: new SprFrameCollectionChangedArg(
                                changedEvent: FRAME_INSERTED,
                                newFrameIndex: (int)frameIndex
                        )));


                return true;
            }

            return false;
        }

        private bool InvalidateDisplayBitmapSourceCache(uint index)
        {
            return DisplayedBitmapSourceCache.AnimationSourceCaching?.Let(it =>
            {
                if (it[index] == null)
                {
                    it[index] = CreateBitmapSourceFromDecodedFrameData(index, out _) ?? throw new Exception();
                }
                else if (SprWorkManager.GetFrameData(index)?.modifiedFrameRGBACache.IsPaletteColorChanged == true)
                {
                    it[index] = CreateBitmapSourceFromDecodedFrameData(index,
                        out List<(Color, Color, int)> rgbColorChangedArgs) ?? throw new Exception();
                }

                DisplayedBitmapSourceCache.DisplayedBitmapSource = it[index];
                DisplayedBitmapSourceCache.CurrentFrameIndex = index;
                return true;
            }) ?? false;
        }

        protected override void NotifyChanged(IDomainChangedArgs args)
        {
            base.NotifyChanged(args);
            //var changedEvent = ((BitmapDisplayMangerChangedArg)args).Event;
            //logger.D($"ChangedEvent: dec={changedEvent},bin={Convert.ToString(changedEvent.Value, 2)}");
        }

        protected override FrameRGBA? GetFrameData(uint frameIndex)
        {
            return SprWorkManager.GetFrameData(frameIndex);
        }

        protected override byte[]? GetDecodedBGRAData(uint index)
        {
            return SprWorkManager.GetDecodedBGRAData(index, out _);
        }
    }

    public class BitmapDisplayMangerChangedArg : SprAnimationChangedArg
    {

        public record BitmapDisplayChangedEvent : SprAnimationChangedEvent
        {
            public static readonly SprAnimationChangedEvent SPR_FRAME_OFFSET_CHANGED = new BitmapDisplayChangedEvent(0b100000);
            public static readonly SprAnimationChangedEvent SPR_FRAME_SIZE_CHANGED = new BitmapDisplayChangedEvent(0b1000000);
            public static readonly SprAnimationChangedEvent SPR_GLOBAL_OFFSET_CHANGED = new BitmapDisplayChangedEvent(0b10000000);
            public static readonly SprAnimationChangedEvent SPR_GLOBAL_SIZE_CHANGED = new BitmapDisplayChangedEvent(0b100000000);
            public static readonly SprAnimationChangedEvent SPR_FRAME_COLLECTION_CHANGED = new BitmapDisplayChangedEvent(0b1000000000);
            public static readonly SprAnimationChangedEvent SPR_FILE_PALETTE_CHANGED = new BitmapDisplayChangedEvent(0b10000000000);
            public static readonly SprAnimationChangedEvent SPR_WORKSPACE_RESET = new BitmapDisplayChangedEvent(0b100000000000);

            public BitmapDisplayChangedEvent(int value) : base(value)
            {
            }
        }

        public SprFileHead? CurrentSprFileHead { get; private set; }
        public SprPaletteChangedArg? PaletteChangedArg { get; private set; }
        public SprFrameCollectionChangedArg? SprFrameCollectionChangedArg { get; private set; }

        public BitmapDisplayMangerChangedArg(SprAnimationChangedEvent changedEvent, BitmapSource? currentDisplayingSource = null,
            SprFileHead? sprFileHead = null,
            bool? isPlayingAnimation = null,
            uint currentDisplayFrameIndex = 0,
            SprFrameCollectionChangedArg? sprFrameCollectionChangedArg = null,
            FrameRGBA? sprFrameData = null,
            uint animationInterval = 0,
            SprPaletteChangedArg? paletteChangedArg = null) : base(changedEvent,
                currentDisplayingSource,
                isPlayingAnimation, currentDisplayFrameIndex,
                sprFrameData, animationInterval)
        {
            CurrentSprFileHead = sprFileHead;
            SprFrameCollectionChangedArg = sprFrameCollectionChangedArg;
            PaletteChangedArg = paletteChangedArg;
        }
    }

    public class SprFrameCollectionChangedArg
    {
        public enum ChangedEvent
        {
            TOTAL_FRAME_COUNT_CHANGED = 0b1,
            FRAME_INSERTED = 0b10,
            FRAME_REMOVED = 0b100,
            FRAME_SWITCHED = 0b1000,
        }

        public ChangedEvent Event { get; private set; }
        public int NewFrameIndex { get; private set; }
        public int OldFrameIndex { get; private set; }
        public uint FrameCount { get; private set; }
        public uint FrameSwitched1Index { get; private set; }
        public uint FrameSwitched2Index { get; private set; }

        public SprFrameCollectionChangedArg(ChangedEvent changedEvent,
            uint frameCount = 0,
            int newFrameIndex = 0,
            uint frameSwitch1Index = 0,
            uint frameSwitch2Index = 0,
            int oldFrameIndex = 0)
        {
            Event = changedEvent;
            FrameCount = frameCount;
            NewFrameIndex = newFrameIndex;
            OldFrameIndex = oldFrameIndex;
            FrameSwitched1Index = frameSwitch1Index;
            FrameSwitched2Index = frameSwitch2Index;
        }
    }

    public class SprPaletteChangedArg
    {
        public enum ChangedEvent
        {
            NEWLY_ADDED = 0b1,
            COLOR_CHANGED = 0b10,
        }

        public ChangedEvent Event { get; private set; }
        public uint ColorChangedIndex { get; private set; }
        public Color NewColor { get; private set; }
        public Palette? Palette { get; private set; }

        public SprPaletteChangedArg(ChangedEvent changedEvent,
            Palette? palette = null,
            uint colorChangedIndex = 0,
            Color newColor = default)
        {
            Event = changedEvent;
            ColorChangedIndex = colorChangedIndex;
            Palette = palette;
            NewColor = newColor;
        }
    }

}
