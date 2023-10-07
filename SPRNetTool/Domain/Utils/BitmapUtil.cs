﻿using SPRNetTool.Data;
using SPRNetTool.Domain.Base;
using SPRNetTool.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SPRNetTool.Domain.Utils
{
    public static class BitmapUtil
    {
        public static List<(Color, long)> CountColorsTolist(this IDomainAdapter adapter, BitmapSource bitmap)
        {
            adapter.CountColors(bitmap, out long argbCount, out long rgbCount, out Dictionary<Color, long> argbSrc);
            return argbSrc.Select(kp => (kp.Key, kp.Value)).ToList();
        }

        public static Dictionary<Color, long> CountColorsToDictionary(this IDomainAdapter adapter, BitmapSource bitmap)
        {
            adapter.CountColors(bitmap, out long argbCount, out long rgbCount, out Dictionary<Color, long> argbSrc);
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
                adapter.CountColors(inputBitmap, out long argbCount, out long rgbCount, out argbSrc);
            });
            return argbSrc;
        }

        public static void CountColors(this IDomainAdapter adapter, BitmapSource bitmap, out long argbCount, out long rgbCount, out Dictionary<Color, long> argbSrc)
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

        public static PaletteColor[] ConvertBitmapSourceToPaletteColorArray(this IDomainAdapter adapter, BitmapSource bitmapSource)
        {
            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;
            int stride = (width * bitmapSource.Format.BitsPerPixel + 7) / 8;
            byte[] pixelData = new byte[height * stride];

            bitmapSource.CopyPixels(pixelData, stride, 0);
            PaletteColor[] paletteColors = new PaletteColor[width * height];
            for (int i = 0; i < width * height; i++)
            {
                if (bitmapSource.Format == PixelFormats.Bgr32)
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

            }

            return paletteColors;
        }

        public static byte[] ConvertPaletteColourArrayToByteArray(this IDomainAdapter adapter, PaletteColor[] colors)
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

        public static BitmapSource GetBitmapFromRGBArray(this IDomainAdapter adapter, byte[] imageData, int width, int height, PixelFormat formats)
        {
            // Tạo một WriteableBitmap
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, formats, null);

            // Gán dữ liệu từ mảng imageData vào WriteableBitmap
            bitmap.Lock();
            if (formats == PixelFormats.Bgra32 || formats == PixelFormats.Pbgra32)
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), imageData, width * 4, 0);
            else if (formats == PixelFormats.Rgb24 || formats == PixelFormats.Bgr24)
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), imageData, width * 3, 0);
            bitmap.Unlock();

            return bitmap;
        }

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
