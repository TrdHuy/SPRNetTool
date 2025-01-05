using ArtWiz.Domain.Base;
using ArtWiz.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WizMachine.Data;

namespace ArtWiz.Domain.Utils
{
    public static class BitmapUtil
    {
        /// <summary>
        /// Chọn ra 1 list các màu được sử dụng nhiều nhất từ các source màu cho trước.
        /// </summary>
        /// <param name="colorDifferenceDelta">
        /// Độ chênh lệch giữa màu đã chọn và màu được đưa vào để đánh giá có nên chọn hay không. 
        /// Nếu màu đang được đánh giá có độ lệch màu với các màu đã chọn lớn hơn giá trị colorDifferenceDelta, 
        /// thì màu đó sẽ được cân nhắc để đưa vào bảng màu đầu ra.</param>
        /// <param name="amount"> Số lượng màu chọn cho output đầu ra.</param>
        /// <param name="countableSources"> Tập các source màu cho trước.</param>
        public static List<Color> SelectMostUseColorFromCountableColorSource(
            this IDomainAdapter adapter,
            int colorDifferenceDelta,
            uint amount = 256,
            params Dictionary<Color, long>[] countableSources)
        {
            // Hợp 2 bộ màu vào làm 1
            var combinedSource = new Dictionary<Color, int>();
            var combinedSource2 = new List<(Color, long)>();

            int i = 0;

            foreach (var source in countableSources)
            {
                foreach (var kp in source)
                {
                    if (combinedSource.ContainsKey(kp.Key))
                    {
                        var index = combinedSource[kp.Key];
                        var item = combinedSource2[index];
                        item.Item2 += kp.Value;
                        combinedSource2[index] = item;
                    }
                    else
                    {
                        combinedSource.Add(kp.Key, i++);
                        combinedSource2.Add((kp.Key, kp.Value));
                    }
                }
            }

            return adapter.SelectMostUseColorFromCountableColorSource(combinedSource2,
                        colorDifferenceDelta,
                        amount,
                        out _,
                        out _,
                        out _,
                        out _);
        }

        public static PaletteColor[] SelectMostUsePaletteColorFromCountableColorSource(
            this IDomainAdapter adapter,
            int colorDifferenceDelta,
            uint amount = 256,
            params Dictionary<Color, long>[] countableSources)
        {
            return adapter.SelectMostUseColorFromCountableColorSource(colorDifferenceDelta,
                amount,
                countableSources)
                .Select(it => new PaletteColor(it.B, it.G, it.R, it.A))
                .ToArray();
        }

        public static List<Color> SelectMostUseColorFromCountableColorSource(
            this IDomainAdapter adapter,
            List<(Color, long)> countableSource,
            int colorDifferenceDelta,
            uint colorSize,
            out int rgbPaletteColorCount,
            out List<Color> selectedColors,
            out List<Color> selectedAlphaColors,
            out List<Color> expectedRGBColors,
            bool isUsingAlpha = false,
            Color? backgroundForBlendColor = null,
            int colorDifferenceDeltaForCalculatingAlpha = 10,
            int deltaDistanceForNewARGBColor = 10,
            int deltaForAlphaAverageDeviation = 3)
        {
            // Sắp xếp theo số lượng sử dụng nhiều nhất
            var orderedSource = countableSource
                .OrderByDescending(kp => kp.Item2)
                .ToList();

            return SelectMostUseColorFromOrderedDescendingColorSource(adapter,
                orderedSource,
                colorDifferenceDelta,
                colorSize,
                out rgbPaletteColorCount,
                out selectedColors,
                out selectedAlphaColors,
                out expectedRGBColors,
                isUsingAlpha,
                backgroundForBlendColor,
                colorDifferenceDeltaForCalculatingAlpha,
                deltaDistanceForNewARGBColor,
                deltaForAlphaAverageDeviation);
        }

        private static List<Color> SelectMostUseColorFromOrderedDescendingColorSource2(IDomainAdapter adapter,
            List<(Color, long)> orderedSource,
            int colorDifferenceDelta,
            uint colorSize,
            out int rgbPaletteColorCount,
            out List<Color> selectedColors,
            out List<Color> selectedAlphaColors,
            out List<Color> expectedRGBColors,
            bool isUsingAlpha,
            Color? backgroundForBlendColor,
            int colorDifferenceDeltaForCalculatingAlpha,
            int deltaDistanceForNewARGBColor,
            int deltaForAlphaAvarageDeviation)
        {
            var start = DateTime.Now;

            var combinedRGBList = new List<Color>();
            var selectedAlphaColorsList = new List<Color>();
            var expectedRGBList = new List<Color>();

            bool IsOk(List<Color> colors,
                Color targetColor,
                int delta,
                out double biggestDistance,
                out Color? alphaColor,
                out Color? combinedColor)
            {
                alphaColor = null; combinedColor = null;
                biggestDistance = Double.MinValue;
                var isOk = true;
                var alphaColorSmallestDistance = Double.MaxValue;
                foreach (var color in colors)
                {
                    var newDistance = targetColor.CalculateEuclideanDistance(color);

                    if (newDistance < delta)
                    {
                        isOk = false;
                        if (newDistance > biggestDistance)
                        {
                            biggestDistance = newDistance;
                        }
                        if (isUsingAlpha && newDistance < colorDifferenceDeltaForCalculatingAlpha)
                        {
                            var backgroundColor = backgroundForBlendColor ?? Colors.White;
                            var alpha = adapter.FindAlphaColors(color, backgroundColor, targetColor, out byte averageAbsoluteDeviation);
                            var newRGBColor = adapter.BlendColors(Color.FromArgb(alpha, color.R, color.G, color.B), backgroundColor);
                            var distanceNewRGBColor = adapter.CalculateEuclideanDistance(newRGBColor, targetColor);
                            if (averageAbsoluteDeviation <= deltaForAlphaAvarageDeviation
                                && distanceNewRGBColor <= deltaDistanceForNewARGBColor
                                && distanceNewRGBColor < alphaColorSmallestDistance)
                            {
                                combinedColor = newRGBColor;
                                alphaColor = Color.FromArgb(alpha, color.R, color.G, color.B);
                                distanceNewRGBColor = alphaColorSmallestDistance;
                                break;
                            }
                        }
                    }
                }
                return isOk;
            }


            // Chọn item cho bảng màu đầu ra, sao cho thỏa mãn:
            // + Màu được sử dụng nhiều nhất.
            // + Màu có độ chênh lệch với các màu đã chọn > colorDifferenceDelta.
            // + Sau khi duyệt toàn bộ màu từ 2 source mà vẫn chưa đủ số lượng màu cho palette đầu
            // ra, giảm chỉ colorDifferenceDelta và lặp lại việc chọn màu.
            var newPaletteSource = new List<Color>();
            List<(Color, long)> tempSource;
            while (newPaletteSource.Count < colorSize
                && colorDifferenceDelta > 0
                && orderedSource.Count > 0)
            {
                tempSource = new List<(Color, long)>();
                var biggestDistance = 0;
                foreach (var color in orderedSource)
                {
                    if (IsOk(newPaletteSource,
                        color.Item1,
                        colorDifferenceDelta,
                        out double newBiggestdistance,
                        out Color? alphaColor,
                        out Color? combinedColor))
                    {
                        newPaletteSource.Add(color.Item1);
                    }
                    else
                    {
                        if (alphaColor == null && combinedColor == null)
                        {
                            biggestDistance = (int)(newBiggestdistance > biggestDistance ? newBiggestdistance : biggestDistance);
                            tempSource.Add(color);
                        }
                        else if (alphaColor != null && combinedColor != null)
                        {
                            combinedRGBList.Add((Color)combinedColor);
                            expectedRGBList.Add(color.Item1);
                            selectedAlphaColorsList.Add((Color)alphaColor);
                        }

                    }
                    if (newPaletteSource.Count >= colorSize) break;
                }

                //var newDelta = (int)((colorDifferenceDelta * newPaletteSource.Count) / amount);
                //colorDifferenceDelta = newDelta == colorDifferenceDelta ? newDelta - 2 : newDelta;
                colorDifferenceDelta = biggestDistance == colorDifferenceDelta ? biggestDistance - 2 : biggestDistance;
                orderedSource = tempSource;
            }

            //Combine RGB and ARGB color to selected list
            rgbPaletteColorCount = newPaletteSource.Count;
            var combinedColorList = newPaletteSource.ToList().Also((it) => it.AddRange(combinedRGBList));

            //reduce same combined color
            combinedColorList = combinedColorList.ReduceSameItem().ToList();

            selectedColors = combinedColorList;
            selectedAlphaColors = selectedAlphaColorsList;
            expectedRGBColors = expectedRGBList;

#if DEBUG
            // Bởi vì combinedRGBList và selectedAlphaColors chỉ được tính gần đúng
            // nên sẽ khác nhau về độ chênh lệch màu
            // Điều kiện assert dưới chỉ đúng khi 2 màu phải bằng nhau tuyệt đối
            // hay không có độ chênh lệch khi tính màu dựa trên kênh alpha
            if (colorDifferenceDeltaForCalculatingAlpha <= 1)
            {
                Debug.Assert(selectedColors.Count == selectedAlphaColors.Count + colorSize);
                Debug.Assert(expectedRGBColors.Count == selectedAlphaColors.Count);
            }
#endif

            Debug.WriteLine($"SelectMostUseColorFromOrderedDescendingColorSource: {(DateTime.Now - start).TotalMilliseconds}ms");
            return combinedColorList;
        }

        private static List<Color> SelectMostUseColorFromOrderedDescendingColorSource(IDomainAdapter adapter,
            List<(Color, long)> orderedSource,
            int colorDifferenceDelta,
            uint colorSize,
            out int rgbPaletteColorCount,
            out List<Color> selectedColors,
            out List<Color> selectedAlphaColors,
            out List<Color> expectedRGBColors,
            bool isUsingAlpha,
            Color? backgroundForBlendColor,
            int colorDifferenceDeltaForCalculatingAlpha,
            int deltaDistanceForNewARGBColor,
            int deltaForAlphaAvarageDeviation)
        {
            var start = DateTime.Now;
            var selectedColorList = new List<Color>();

            // TODO: Dynamic this
            var selectedAlphaColorsList = new List<Color>();
            var combinedRGBList = new List<Color>();
            var expectedRGBList = new List<Color>();

            // Optimize color palette
            while (selectedColorList.Count < colorSize && orderedSource.Count > 0 && colorDifferenceDelta >= 0)
            {
                for (int i = 0; i < orderedSource.Count; i++)
                {
                    // For performance issue, do not use ElementAt to access the value with index
                    // use indexer instead
                    var expectedColor = orderedSource[i].Item1;
                    var shouldAdd = true;
                    foreach (var selectedColor in selectedColorList)
                    {
                        var distance = adapter.CalculateEuclideanDistance(expectedColor, selectedColor);
                        if (distance < colorDifferenceDelta)
                        {
                            if (isUsingAlpha && distance < colorDifferenceDeltaForCalculatingAlpha)
                            {
                                var bg = backgroundForBlendColor ?? Colors.White;
                                var alpha = adapter.FindAlphaColors(selectedColor, bg, expectedColor, out byte averageAbsoluteDeviation);
                                var newRGBColor = adapter.BlendColors(Color.FromArgb(alpha, selectedColor.R, selectedColor.G, selectedColor.B), bg);
                                var distanceNewRGBColor = adapter.CalculateEuclideanDistance(newRGBColor, expectedColor);
                                if (averageAbsoluteDeviation <= deltaForAlphaAvarageDeviation && distanceNewRGBColor <= deltaDistanceForNewARGBColor)
                                {
                                    expectedRGBList.Add(expectedColor);
                                    combinedRGBList.Add(newRGBColor);
                                    selectedAlphaColorsList.Add(Color.FromArgb(alpha, selectedColor.R, selectedColor.G, selectedColor.B));
                                    orderedSource.RemoveAt(i);
                                    i--;
                                }
                            }
                            shouldAdd = false;
                            break;
                        }
                    }
                    if (shouldAdd)
                    {
                        selectedColorList.Add(expectedColor);
                        orderedSource.RemoveAt(i);
                        i--;
                    }

                    if (selectedColorList.Count >= colorSize) break;
                }
                colorDifferenceDelta -= 2;
                //var newDelta = (int)((colorDifferenceDelta * selectedColorList.Count) / colorSize);
                //colorDifferenceDelta = newDelta == colorDifferenceDelta ? newDelta - 2 : newDelta;
            }

            //Combine RGB and ARGB color to selected list
            rgbPaletteColorCount = selectedColorList.Count;
            var combinedColorList = selectedColorList.ToList().Also((it) => it.AddRange(combinedRGBList));

            //reduce same combined color
            combinedColorList = combinedColorList.ReduceSameItem().ToList();

            selectedColors = combinedColorList;
            selectedAlphaColors = selectedAlphaColorsList;
            expectedRGBColors = expectedRGBList;

#if DEBUG
            // Bởi vì combinedRGBList và selectedAlphaColors chỉ được tính gần đúng
            // nên sẽ khác nhau về độ chênh lệch màu
            // Điều kiện assert dưới chỉ đúng khi 2 màu phải bằng nhau tuyệt đối
            // hay không có độ chênh lệch khi tính màu dựa trên kênh alpha
            if (colorDifferenceDeltaForCalculatingAlpha <= 1)
            {
                Debug.Assert(selectedColors.Count == selectedAlphaColors.Count + colorSize);
                Debug.Assert(expectedRGBColors.Count == selectedAlphaColors.Count);
            }
#endif

            Debug.WriteLine($"SelectMostUseColorFromOrderedDescendingColorSource: {(DateTime.Now - start).TotalMilliseconds}ms");
            return combinedColorList;
        }

        public static List<(Color, long)> CountColorsToList(this IDomainAdapter adapter, BitmapSource bitmap)
        {
            adapter.CountColors(bitmap, out long argbCount, out long rgbCount, out Dictionary<Color, long> argbSrc, out _);
            return argbSrc.Select(kp => (kp.Key, kp.Value)).ToList();
        }

        public static Dictionary<Color, long> CountColorsToDictionary(this IDomainAdapter adapter, BitmapSource bitmap)
        {
            adapter.CountColors(bitmap, out long argbCount, out long rgbCount, out Dictionary<Color, long> argbSrc, out _);
            return argbSrc;
        }

        public static async Task<Dictionary<Color, long>> CountColorsAsync(this IDomainAdapter adapter, BitmapSource bitmap)
        {
            Dictionary<Color, long> argbSrc = new Dictionary<Color, long>();
            var shouldCreateFrozenBitMap = !bitmap.IsFrozen;
            await Task.Run(() =>
            {
                var inputBitmap = bitmap;
                if (shouldCreateFrozenBitMap)
                {
                    inputBitmap = BitmapFrame.Create(bitmap);
                    inputBitmap.Freeze();
                }
                adapter.CountColors(inputBitmap, out long argbCount, out long rgbCount, out argbSrc, out _);
            });
            return argbSrc;
        }

        public static Dictionary<Color, long> CountBGRAColors(this IDomainAdapter adapter, byte[] pixelArray,
            out long argbCount,
            out long rgbCount,
            out Dictionary<Color, long> argbSrc,
            out Dictionary<Color, long> rgbSrc)
        {
            Dictionary<Color, long> aRGBColorSet = new Dictionary<Color, long>();
            Dictionary<Color, long> rGBColorSet = new Dictionary<Color, long>();

            for (int i = 0; i < pixelArray.Length; i += 4)
            {
                byte blue = pixelArray[i];
                byte green = pixelArray[i + 1];
                byte red = pixelArray[i + 2];
                byte alpha = pixelArray[i + 3];

                Color colorARGB = Color.FromArgb(alpha, red, green, blue);
                if (!aRGBColorSet.ContainsKey(colorARGB))
                {
                    aRGBColorSet[colorARGB] = 1;
                }
                else
                {
                    aRGBColorSet[colorARGB]++;
                }

                Color colorRGB = Color.FromRgb(red, green, blue);
                if (!rGBColorSet.ContainsKey(colorRGB))
                {
                    rGBColorSet[colorRGB] = 1;
                }
                else
                {
                    rGBColorSet[colorRGB]++;
                }
            }
            argbCount = aRGBColorSet.Count;
            rgbCount = rGBColorSet.Count;
            argbSrc = aRGBColorSet;
            rgbSrc = rGBColorSet;
            return argbSrc;
        }

        public static Dictionary<Color, long> CountColors(this IDomainAdapter adapter, PaletteColor[] pixelArray,
            out long argbCount,
            out long rgbCount,
            out Dictionary<Color, long> argbSrc,
            out Dictionary<Color, long> rgbSrc)
        {
            Dictionary<Color, long> aRGBColorSet = new Dictionary<Color, long>();
            Dictionary<Color, long> rGBColorSet = new Dictionary<Color, long>();

            for (int i = 0; i < pixelArray.Length; i++)
            {
                byte blue = pixelArray[i].Blue;
                byte green = pixelArray[i].Green;
                byte red = pixelArray[i].Red;
                byte alpha = pixelArray[i].Alpha;

                Color colorARGB = Color.FromArgb(alpha, red, green, blue);
                if (!aRGBColorSet.ContainsKey(colorARGB))
                {
                    aRGBColorSet[colorARGB] = 1;
                }
                else
                {
                    aRGBColorSet[colorARGB]++;
                }

                Color colorRGB = Color.FromRgb(red, green, blue);
                if (!rGBColorSet.ContainsKey(colorRGB))
                {
                    rGBColorSet[colorRGB] = 1;
                }
                else
                {
                    rGBColorSet[colorRGB]++;
                }
            }
            argbCount = aRGBColorSet.Count;
            rgbCount = rGBColorSet.Count;
            argbSrc = aRGBColorSet;
            rgbSrc = rGBColorSet;
            return argbSrc;
        }

        public static void CountColors(this IDomainAdapter adapter, BitmapSource bitmap,
            out long argbCount,
            out long rgbCount,
            out Dictionary<Color, long> argbSrc,
            out HashSet<Color> rgbSrc)
        {
            Dictionary<Color, long> aRGBColorSet = new Dictionary<Color, long>();
            HashSet<Color> rGBColorSet = new HashSet<Color>();

            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int stride = (width * bitmap.Format.BitsPerPixel + 7) / 8;
            byte[] pixelData = new byte[stride * height];

            bitmap.CopyPixels(pixelData, stride, 0);

            var isIncludedAlphaChannel = bitmap.Format.BitsPerPixel / 8 == 4;
            for (int i = 0; i < pixelData.Length; i += bitmap.Format.BitsPerPixel / 8)
            {
                byte blue = pixelData[i];
                byte green = pixelData[i + 1];
                byte red = pixelData[i + 2];
                if (isIncludedAlphaChannel)
                {
                    byte alpha = pixelData[i + 3];
                    Color colorARGB = Color.FromArgb(alpha, red, green, blue);
                    if (!aRGBColorSet.ContainsKey(colorARGB))
                    {
                        aRGBColorSet[colorARGB] = 1;
                    }
                    else
                    {
                        aRGBColorSet[colorARGB]++;
                    }
                }

                Color colorRGB = Color.FromRgb(red, green, blue);
                rGBColorSet.Add(colorRGB);
            }
            argbCount = aRGBColorSet.Count;
            rgbCount = rGBColorSet.Count;
            argbSrc = aRGBColorSet;
            rgbSrc = rGBColorSet;
        }

        public static byte[] ConvertBitmapSourceToByteArray(this IDomainAdapter adapter, BitmapSource bmp)
        {
            int width = bmp.PixelWidth;
            int height = bmp.PixelHeight;
            int stride = (width * bmp.Format.BitsPerPixel + 7) / 8;
            byte[] pixelData = new byte[stride * height];
            bmp.CopyPixels(pixelData, stride, 0);
            return pixelData;
        }

        #region ConvertBitmapSourceToPaletteColorArray
        public static PaletteColor[] ConvertBitmapSourceToPaletteColorArray(this IDomainAdapter adapter,
            BitmapSource bitmapSource)
        {
            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;
            int stride = (width * bitmapSource.Format.BitsPerPixel + 7) / 8;
            byte[] pixelData = new byte[height * stride];

            bitmapSource.CopyPixels(pixelData, stride, 0);
            PaletteColor[] paletteColors = new PaletteColor[width * height];
            for (int i = 0; i < width * height; i++)
            {
                if (bitmapSource.Format == PixelFormats.Bgr32 ||
                    bitmapSource.Format == PixelFormats.Bgra32)
                {
                    int offset = i * 4;
                    paletteColors[i] = new PaletteColor(blue: pixelData[offset],
                        green: pixelData[offset + 1],
                        red: pixelData[offset + 2],
                        alpha: pixelData[offset + 3]);
                }
                else if (bitmapSource.Format == PixelFormats.Rgb24)
                {
                    int offset = i * 3;
                    paletteColors[i] = new PaletteColor(blue: pixelData[offset + 2],
                        green: pixelData[offset + 1],
                        red: pixelData[offset],
                        alpha: 255);
                }
                else
                {
                    throw new Exception("ConvertBitmapSourceToPaletteColorArray: Invaild format!");
                }

            }

            return paletteColors;
        }

        public static PaletteColor[] ConvertBitmapSourceToPaletteColorArray(this IDomainAdapter adapter,
            BitmapSource bitmapSource,
            out Dictionary<Color, long> argbCountableSource,
            out Dictionary<Color, long> rgbCountableSource,
            out Palette palette,
            out byte[] bgraBytesData,
            out Dictionary<int, List<long>> paletteColorIndexToPixelIndexMap)
        {
            paletteColorIndexToPixelIndexMap = new Dictionary<int, List<long>>();
            var colorPixelIndexMap = new Dictionary<Color, List<long>>();
            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;
            int stride = (width * bitmapSource.Format.BitsPerPixel + 7) / 8;
            byte[] pixelData = new byte[height * stride];
            var bgraCounter = 0;
            if (bitmapSource.Format == PixelFormats.Bgr32 ||
                   bitmapSource.Format == PixelFormats.Bgra32)
            {
                bgraBytesData = pixelData;
            }
            else
            {
                bgraBytesData = new byte[height * width * 4];
            }
            bitmapSource.CopyPixels(pixelData, stride, 0);

            PaletteColor[] paletteColors = new PaletteColor[width * height];
            argbCountableSource = new Dictionary<Color, long>();
            var rgbSource = new Dictionary<Color, long>();
            for (int i = 0; i < width * height; i++)
            {
                var argbColor = Colors.Transparent;
                var rgbColor = Colors.White;
                if (bitmapSource.Format == PixelFormats.Bgr32 ||
                    bitmapSource.Format == PixelFormats.Bgra32)
                {
                    int offset = i * 4;
                    paletteColors[i] = new PaletteColor(blue: pixelData[offset],
                        green: pixelData[offset + 1],
                        red: pixelData[offset + 2],
                        alpha: pixelData[offset + 3]);
                    argbColor = Color.FromArgb(pixelData[offset + 3],
                        pixelData[offset + 2],
                        pixelData[offset + 1],
                        pixelData[offset]);
                    rgbColor = Color.FromRgb(
                        pixelData[offset + 2],
                        pixelData[offset + 1],
                        pixelData[offset]);
                }
                else if (bitmapSource.Format == PixelFormats.Rgb24)
                {
                    int offset = i * 3;
                    paletteColors[i] = new PaletteColor(blue: pixelData[offset + 2],
                        green: pixelData[offset + 1],
                        red: pixelData[offset],
                        alpha: 255);
                    argbColor = Color.FromArgb(255,
                        pixelData[offset],
                        pixelData[offset + 1],
                        pixelData[offset + 2]);
                    rgbColor = Color.FromRgb(
                        pixelData[offset + 2],
                        pixelData[offset + 1],
                        pixelData[offset]);

                    bgraBytesData[bgraCounter] = pixelData[offset + 2];
                    bgraBytesData[bgraCounter] = pixelData[offset + 1];
                    bgraBytesData[bgraCounter] = pixelData[offset];
                    bgraBytesData[bgraCounter] = 255;
                }
                else
                {
                    throw new Exception("ConvertBitmapSourceToPaletteColorArray: Invaild format!");
                }

                if (argbCountableSource.ContainsKey(argbColor))
                {
                    argbCountableSource[argbColor] += 1;
                }
                else
                {
                    argbCountableSource.Add(argbColor, 1);
                }

                if (rgbSource.ContainsKey(rgbColor))
                {
                    colorPixelIndexMap[rgbColor].Add(i);
                    rgbSource[rgbColor]++;
                }
                else
                {
                    colorPixelIndexMap.Add(rgbColor, new List<long> { i });
                    rgbSource.Add(rgbColor, 1);
                }
            }
            palette = new Palette(rgbSource.Count);
            int j = 0;
            foreach (var color in rgbSource.Keys)
            {
                palette.Data[j] = new PaletteColor(color.B, color.G, color.R, 255);
                paletteColorIndexToPixelIndexMap.Add(j, colorPixelIndexMap[color]);
                j++;
            }
            rgbCountableSource = rgbSource;
            return paletteColors;
        }
        #endregion

        public static byte[] ConvertPaletteColorArrayToByteArray(this IDomainAdapter adapter, PaletteColor[] colors)
        {
            int colorSize = Marshal.SizeOf(typeof(PaletteColor));
            byte[] byteArray = new byte[colors.Length * colorSize];

            for (int i = 0; i < colors.Length; i++)
            {
                byte[] colorBytes = new byte[colorSize];
                IntPtr ptr = Marshal.AllocHGlobal(colorSize);

                try
                {
                    Marshal.StructureToPtr(colors[i], ptr, false);
                    Marshal.Copy(ptr, colorBytes, 0, colorSize);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }

                Buffer.BlockCopy(colorBytes, 0, byteArray, i * colorSize, colorSize);
            }

            return byteArray;
        }

        #region FloydSteinbergDithering
        public static BitmapSource? FloydSteinbergDithering(this IDomainAdapter adapter, BitmapSource sourceImage
            , List<Color> rgbPalette)
        {
            if (sourceImage.Format != PixelFormats.Bgra32 &&
                sourceImage.Format != PixelFormats.Bgr32 &&
                sourceImage.Format != PixelFormats.Bgr24)
            {
                return null;
            }
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;
            int stride = (width * sourceImage.Format.BitsPerPixel + 7) / 8;

            byte[] oldBmpPixels = new byte[stride * height];
            sourceImage.CopyPixels(oldBmpPixels, stride, 0);

            byte[] resultPixels = new byte[stride * height];

            for (int i = 0; i < oldBmpPixels.Length; i += sourceImage.Format.BitsPerPixel / 8)
            {
                byte blue = oldBmpPixels[i];
                byte green = oldBmpPixels[i + 1];
                byte red = oldBmpPixels[i + 2];
                byte alpha = oldBmpPixels[i + 3];


                Color sourceColor = Color.FromArgb(alpha, red, green, blue);
                Color closestColor = adapter.FindClosestPaletteColor(sourceColor, rgbPalette);

                resultPixels[i] = closestColor.B;
                resultPixels[i + 1] = closestColor.G;
                resultPixels[i + 2] = closestColor.R;
                resultPixels[i + 3] = closestColor.A;
            }

            var formats = sourceImage.Format;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, formats, null);

            // Gán dữ liệu từ mảng imageData vào WriteableBitmap
            bitmap.Lock();
            if (formats == PixelFormats.Bgra32 || formats == PixelFormats.Pbgra32 || formats == PixelFormats.Bgr32)
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width * 4, 0);
            else if (formats == PixelFormats.Rgb24 || formats == PixelFormats.Bgr24)
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width * 3, 0);
            bitmap.Unlock();

            return bitmap;
        }

        public static BitmapSource? FloydSteinbergDithering(this IDomainAdapter adapter, BitmapSource sourceImage
            , List<Color> rgbPalette
            , bool isUsingAlpha
            , List<Color>? argbPalette
            , Color blendedBGColor)
        {
            if (sourceImage.Format != PixelFormats.Bgra32 &&
                sourceImage.Format != PixelFormats.Bgr32 &&
                sourceImage.Format != PixelFormats.Bgr24)
            {
                return null;
            }
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;
            int stride = (width * sourceImage.Format.BitsPerPixel + 7) / 8;

            byte[] oldBmpPixels = new byte[stride * height];
            sourceImage.CopyPixels(oldBmpPixels, stride, 0);

            byte[] resultPixels = new byte[stride * height];

            List<(Color, Color, long)> countedList = new List<(Color, Color, long)>();

            var recalculateRGBColor = new List<Color>();
            if (isUsingAlpha && argbPalette != null)
            {
                foreach (var color in rgbPalette)
                {
                    recalculateRGBColor.Add(color);
                }
                foreach (var color in argbPalette)
                {
                    recalculateRGBColor.Add(color.BlendColors(blendedBGColor));
                }

                recalculateRGBColor = recalculateRGBColor.GroupBy(c => c).Select(g => g.First()).ToList();
            }

            for (int i = 0; i < oldBmpPixels.Length; i += sourceImage.Format.BitsPerPixel / 8)
            {
                byte blue = oldBmpPixels[i];
                byte green = oldBmpPixels[i + 1];
                byte red = oldBmpPixels[i + 2];
                byte alpha = oldBmpPixels[i + 3];


                Color sourceColor = Color.FromArgb(alpha, red, green, blue);
                Color closestColor = sourceColor;
                if (isUsingAlpha)
                {
                    closestColor = adapter.FindClosestPaletteColor(sourceColor, recalculateRGBColor);
                }
                else
                {
                    closestColor = adapter.FindClosestPaletteColor(closestColor, rgbPalette);
                }

                resultPixels[i] = closestColor.B;
                resultPixels[i + 1] = closestColor.G;
                resultPixels[i + 2] = closestColor.R;
                resultPixels[i + 3] = closestColor.A;
            }

            var formats = sourceImage.Format;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, formats, null);

            // Gán dữ liệu từ mảng imageData vào WriteableBitmap
            bitmap.Lock();
            if (formats == PixelFormats.Bgra32 || formats == PixelFormats.Pbgra32 || formats == PixelFormats.Bgr32)
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width * 4, 0);
            else if (formats == PixelFormats.Rgb24 || formats == PixelFormats.Bgr24)
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width * 3, 0);
            bitmap.Unlock();

            return bitmap;
        }
        #endregion

        public static bool AreByteArraysEqual(this IDomainAdapter adapter, byte[] array1, byte[] array2)
        {
            // Nếu mảng có chiều dài khác nhau, chúng không giống nhau
            if (array1.Length != array2.Length)
            {
                return false;
            }

            for (int i = 0; i < array1.Length; i++)
            {
                // So sánh từng phần tử của hai mảng
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }

            // Nếu không có phần tử nào khác nhau, chúng giống nhau
            return true;
        }

        public static bool AreCountableSourcesEqual(this IDomainAdapter adapter
            , Dictionary<Color, long> sourceA
            , Dictionary<Color, long> sourceB)
        {
            if (sourceA.Count != sourceB.Count)
            {
                return false;
            }

            foreach (var kp in sourceA)
            {
                try
                {
                    if (sourceB[kp.Key] != kp.Value)
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        private static Color FindClosestPaletteColor(this IDomainAdapter adapter, Color sourceColor, List<Color> palette)
        {
            // Tìm màu gần nhất trong bảng màu giới hạn
            double minDistanceSquared = double.MaxValue;
            Color closestColor = Colors.Black;

            for (int i = 0; i < palette.Count; i++)
            {
                Color paletteColor = palette[i];

                double distanceSquared = sourceColor.CalculateEuclideanDistance(paletteColor);

                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                    closestColor = paletteColor;
                }

            }

            return closestColor;
        }

        //public static BitmapSource ApplyDithering2(BitmapSource sourceImage, int colorCount)
        //{
        //    int width = sourceImage.PixelWidth;
        //    int height = sourceImage.PixelHeight;
        //    int stride = (width * sourceImage.Format.BitsPerPixel + 7) / 8;
        //    WriteableBitmap newBmp = new WriteableBitmap(width, height, 96, 96, sourceImage.Format, null);

        //    byte[] oldBmpPixels = new byte[stride * height];
        //    sourceImage.CopyPixels(oldBmpPixels, stride, 0);

        //    byte[] resultPixels = new byte[stride * height];

        //    for (int y = 0; y < height; y++)
        //    {
        //        for (int x = 0; x < width; x++)
        //        {
        //            int index = y * width + x;
        //            byte oldPixel = oldBmpPixels[index];
        //            byte newPixel = (byte)Math.Round((oldPixel * (colorCount - 1)) / 255.0) * (255 / (colorCount - 1));
        //            resultPixels[index] = newPixel;

        //            int quantError = oldPixel - newPixel;

        //            // Dithering Floyd-Steinberg
        //            if (x < width - 1)
        //                newBmpPixels[index + 1] = (byte)Math.Max(0, Math.Min(255, newBmpPixels[index + 1] + quantError * 7 / 16));

        //            if (x > 0 && y < height - 1)
        //                newBmpPixels[index - 1 + width] = (byte)Math.Max(0, Math.Min(255, newBmpPixels[index - 1 + width] + quantError * 3 / 16));

        //            if (y < height - 1)
        //                newBmpPixels[index + width] = (byte)Math.Max(0, Math.Min(255, newBmpPixels[index + width] + quantError * 5 / 16));

        //            if (x < width - 1 && y < height - 1)
        //                newBmpPixels[index + 1 + width] = (byte)Math.Max(0, Math.Min(255, newBmpPixels[index + 1 + width] + quantError * 1 / 16));
        //        }
        //    }

        //    BitmapSource ditheredBitmap = BitmapSource.Create(width, height, 96, 96, sourceImage.Format, null, resultPixels, stride);
        //    return ditheredBitmap;
        //}


        public static BitmapSource? LoadBitmapFromFile(this IDomainAdapter adapter, string filePath, bool isFreeze = true)
        {
            BitmapImage bitmapImage = new BitmapImage();

            try
            {
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(filePath);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải tệp hình ảnh: {ex.Message}");
                return null;
            }

            if (bitmapImage.IsDownloading)
            {
                bitmapImage.DownloadCompleted += (sender, e) =>
                {
                    // Bitmap đã được tải xong, bạn có thể sử dụng nó ở đây
                    // Ví dụ: Image.Source = bitmapImage;
                };
            }
            else
            {
                // Bitmap đã được tải xong, bạn có thể sử dụng nó ở đây
                // Ví dụ: Image.Source = bitmapImage;
            }
            return (bitmapImage as BitmapSource).Also((it) =>
            {
                if (isFreeze)
                    it.Freeze();
            });
        }

        public static BitmapSource ConvertBitmapToBitmapSource(this IDomainAdapter adapter, System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            // Chuyển đổi Bitmap thành BitmapSource
            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return bitmapSource;
        }

        public static string PaletteColourToString(this IDomainAdapter adapter, PaletteColor color)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(color.Red.ToString("D3"));
            sb.Append(",");
            sb.Append(color.Green.ToString("D3"));
            sb.Append(",");
            sb.Append(color.Blue.ToString("D3"));
            sb.Append(",");
            sb.Append(color.Alpha.ToString("D3"));
            return sb.ToString();
        }

        public static void Print2DArrayToFile(this IDomainAdapter adapter, PaletteColor[] arr, int rows, int cols, string relativePath)
        {
            string outputPath = relativePath.FullPath();
            using (StreamWriter outputFile = new StreamWriter(outputPath))
            {
                // In hàng đầu tiên (các số từ 1 đến cols)
                outputFile.Write("\t");
                for (int j = 0; j < cols; ++j)
                {
                    outputFile.Write(j + 1);
                    outputFile.Write("\t\t\t\t");
                }
                outputFile.WriteLine();

                // Duyệt qua từng hàng và in các giá trị
                for (int i = 0; i < rows; ++i)
                {
                    outputFile.Write(i + 1); // In số hàng
                    outputFile.Write('\t');

                    for (int j = 0; j < cols; ++j)
                    {
                        int index = i * cols + j;
                        outputFile.Write(adapter.PaletteColourToString(arr[index]));
                        outputFile.Write('\t');
                    }

                    outputFile.WriteLine();
                }

                Console.WriteLine($"Đã ghi vào tệp '{outputPath}'");
            }
        }
    }
}
