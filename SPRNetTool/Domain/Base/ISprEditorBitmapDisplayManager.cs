using ArtWiz.Domain.Utils;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ArtWiz.Domain.Base
{
    public interface ISprEditorBitmapDisplayManager : IObservableDomain, IDomainAdapter
    {
        void ResetSprWorkSpace();
        void OpenBitmapFromFile(string filePath);

        void SetCurrentlyDisplayedFrameOffset(short frameOffX, short frameOffY);

        void SetCurrentlyDisplayedFrameSize(ushort frameWidth, ushort frameHeight);

        void ChangeCurrentDisplayMode(bool isSpr);

        void StartSprAnimation();

        void StopSprAnimation();

        uint GetCurrentDisplayFrameIndex();

        void SetNewColorToPalette(uint paletteIndex, Color newColor);

        void SetSprGlobalSize(ushort width, ushort height);
        void SetSprGlobalOffset(short offX, short offY);

        void SetCurrentlyDisplayedSprFrameIndex(uint index);

        void SetSprInterval(ushort interval);

        bool SwitchFrame(uint frameIndex1, uint frameIndex2);

        bool DeleteFrame(uint frameIndex);

        bool InsertFrame(uint frameIndex, string filePath);
        bool InsertFrame(uint frameIndex, BitmapSource bmpSrc);

        Dictionary<Color, long> CountBitmapColors(BitmapSource bitmap)
        {
            return this.CountColorsToDictionary(bitmap);
        }

        BitmapSource? OptimzeImageColorNA256(BitmapSource bmpSource)
        {
            var optimizedBmp = OptimzeImageColor(countableColorSource: this.CountColorsToList(bmpSource),
                bmpSource: bmpSource,
                colorSize: 256,
                colorDifferenceDelta: 100,
                isUsingAlpha: false,
                colorDifferenceDeltaForCalculatingAlpha: 10,
                backgroundForBlendColor: Colors.White,
                out _,
                out _,
                out _);
            return optimizedBmp;
        }

        /// <summary>
        /// Tối ưu số lượng màu của 1 bitmap source
        /// </summary>
        /// <param name="countableColorSource">danh sách các màu được đếm trong bitmap source</param>
        /// <param name="bmpSource">bitmap source cần được tối ưu</param>
        /// <param name="colorSize">số lượng màu muốn tối ưu</param>
        /// <param name="colorDifferenceDelta">Độ chênh lệch tối đa giữa 2 màu</param>
        /// <param name="isUsingAlpha">Có sử dụng kênh alpha để tính thêm màu hay không</param>
        /// <param name="colorDifferenceDeltaForCalculatingAlpha">Khi sử dụng kênh alpha để tính thêm màu, cần độ lệch này để xác định màu đó có được chọn hay không</param>
        /// <param name="backgroundForBlendColor">Khi sử dụng kênh alpha để tính thêm màu, cần 1 màu nền để trộn với màu chính, tạo ra 1 màu kết hợp</param>
        /// <param name="selectedColors">Danh sách các màu được chọn</param>
        /// <param name="selectedAlphaColors">Danh sách các màu với kênh alpha được chọn</param>
        /// <param name="expectedRGBColors">Danh sách các màu mong muốn khi sử dụng kênh alpha để tính thêm màu</param>
        /// <returns></returns>
        BitmapSource? OptimzeImageColor(List<(Color, long)> countableColorSource
           , BitmapSource bmpSource
           , int colorSize
           , int colorDifferenceDelta
           , bool isUsingAlpha
           , int colorDifferenceDeltaForCalculatingAlpha
           , Color backgroundForBlendColor
           , out List<Color> selectedColors
           , out List<Color> selectedAlphaColors
           , out List<Color> expectedRGBColors)
        {
            var newPaletteSource = this.SelectMostUseColorFromCountableColorSource(countableColorSource,
                colorDifferenceDelta,
                (uint)colorSize,
                out int optimizedRGBCount,
                out selectedColors,
                out selectedAlphaColors,
                out expectedRGBColors,
                isUsingAlpha,
                backgroundForBlendColor,
                colorDifferenceDeltaForCalculatingAlpha);
            //======================================================
            //Dithering
            if (optimizedRGBCount > 0 && optimizedRGBCount <= colorSize && bmpSource != null)
            {
                var newBmpSrc = this.FloydSteinbergDithering(bmpSource, newPaletteSource);
                newBmpSrc?.Freeze();
                return newBmpSrc;
            }

            return null;
        }

    }
}

