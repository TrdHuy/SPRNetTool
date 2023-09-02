﻿using SPRNetTool.Data;
using System;
using System.IO;
using System.Text;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using ImageMagick;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SPRNetTool.Utils
{
    public static class BitmapUtil
    {
        public static async Task<Dictionary<Color, long>> CountColorsAsync(BitmapSource bitmap)
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
                CountColors(inputBitmap, out long argbCount, out long rgbCount, out argbSrc);
            });
            return argbSrc;
        }

        public static void CountColors(BitmapSource bitmap, out long argbCount, out long rgbCount, out Dictionary<Color, long> argbSrc)
        {
            Dictionary<Color, long> aRGBColorSet = new Dictionary<Color, long>();
            HashSet<Color> rGBColorSet = new HashSet<Color>();

            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int stride = (width * bitmap.Format.BitsPerPixel + 7) / 8;
            byte[] pixelData = new byte[stride * height];

            bitmap.CopyPixels(pixelData, stride, 0);

            for (int i = 0; i < pixelData.Length; i += bitmap.Format.BitsPerPixel / 8)
            {
                byte blue = pixelData[i];
                byte green = pixelData[i + 1];
                byte red = pixelData[i + 2];
                byte alpha = pixelData[i + 3];

                Color colorARGB = Color.FromArgb(alpha, red, green, blue);
                Color colorRGB = Color.FromRgb(red, green, blue);

                if (!aRGBColorSet.ContainsKey(colorARGB))
                {
                    aRGBColorSet[colorARGB] = 1;
                }
                else
                {
                    aRGBColorSet[colorARGB]++;
                }
                rGBColorSet.Add(colorRGB);
            }
            argbCount = aRGBColorSet.Count;
            rgbCount = rGBColorSet.Count;
            argbSrc = aRGBColorSet;
        }

        public static byte[] ConvertPaletteColourArrayToByteArray(PaletteColour[] colors)
        {
            int colorSize = Marshal.SizeOf(typeof(PaletteColour));
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


        public static BitmapSource? FloydSteinbergDithering(BitmapSource sourceImage
            , List<System.Windows.Media.Color> palette
            , bool isUsingAlpha)
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
                Color closestColor = FindClosestPaletteColor(sourceColor, palette, isUsingAlpha);

                resultPixels[i] = closestColor.B;
                resultPixels[i + 1] = closestColor.G;
                resultPixels[i + 2] = closestColor.R;
                resultPixels[i + 3] = closestColor.A;
            }

            // Duyệt qua từng pixel trong ảnh
            //for (int y = 0; y < height; y++)
            //{
            //    for (int x = 0; x < width; x++)
            //    {
            //        Color sourceColor = sourceImage.GetPixel(x, y);
            //        Color closestColor = FindClosestPaletteColor(sourceColor, palette);

            //        resultImage.SetPixel(x, y, closestColor);

            //        int quantizationErrorR = sourceColor.R - closestColor.R;
            //        int quantizationErrorG = sourceColor.G - closestColor.G;
            //        int quantizationErrorB = sourceColor.B - closestColor.B;

            //        // Phân phối sai số sang các pixel lân cận
            //        if (x + 1 < width)
            //            PropagateError(sourceImage, x + 1, y, quantizationErrorR, quantizationErrorG, quantizationErrorB, 7 / 16.0);
            //        if (x - 1 >= 0 && y + 1 < height)
            //            PropagateError(sourceImage, x - 1, y + 1, quantizationErrorR, quantizationErrorG, quantizationErrorB, 3 / 16.0);
            //        if (y + 1 < height)
            //            PropagateError(sourceImage, x, y + 1, quantizationErrorR, quantizationErrorG, quantizationErrorB, 5 / 16.0);
            //        if (x + 1 < width && y + 1 < height)
            //            PropagateError(sourceImage, x + 1, y + 1, quantizationErrorR, quantizationErrorG, quantizationErrorB, 1 / 16.0);
            //    }
            //}
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

        private static Color FindClosestPaletteColor(Color sourceColor, List<Color> palette, bool isUsingAlpha = false)
        {
            //TODO: Set blend bg color

            var blendBackgroundColor = Colors.White;
            var threshHoldAlpha = 80;
            // Tìm màu gần nhất trong bảng màu giới hạn
            double minDistanceSquared = double.MaxValue;
            Color closestColor = Colors.Black;

            for (int i = 0; i < palette.Count; i++)
            {
                Color paletteColor = palette[i];
                if (isUsingAlpha)
                {
                    for (int j = 255; j >= 0; j -= threshHoldAlpha)
                    {
                        paletteColor = palette[i];
                        paletteColor.A = (byte)j;
                        paletteColor = paletteColor.BlendColors(blendBackgroundColor);
                        double distanceSquared = sourceColor.CalculateEuclideanDistance(paletteColor);

                        if (distanceSquared < minDistanceSquared)
                        {
                            minDistanceSquared = distanceSquared;
                            closestColor = paletteColor;
                        }
                    }
                }
                else
                {
                    double distanceSquared = sourceColor.CalculateEuclideanDistance(paletteColor);

                    if (distanceSquared < minDistanceSquared)
                    {
                        minDistanceSquared = distanceSquared;
                        closestColor = paletteColor;
                    }
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

        public static BitmapSource ApplyDithering(BitmapSource sourceImage, int colorCount)
        {
            // Chuyển đổi BitmapSource thành mảng byte
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;
            int stride = (width * sourceImage.Format.BitsPerPixel + 7) / 8;
            byte[] pixelData = new byte[stride * height];
            sourceImage.CopyPixels(pixelData, stride, 0);

            // Tạo MagickImage từ mảng byte
            using (var magickImage = new MagickImage(pixelData, width, height, MagickFormat.Bgra))
            {
                // Thiết lập số lượng màu
                magickImage.Quantize(new QuantizeSettings
                {
                    Colors = colorCount,
                    DitherMethod = DitherMethod.FloydSteinberg // Hoặc DitherMethod.Stucki
                });

                // Chuyển đổi MagickImage thành BitmapSource
                byte[] resultPixelData = magickImage.ToByteArray(MagickFormat.Bmp);
                using (var memoryStream = new MemoryStream(resultPixelData))
                {
                    var decoder = new BmpBitmapDecoder(memoryStream, BitmapCreateOptions.None, BitmapCacheOption.Default);
                    return decoder.Frames[0];
                }
            }
        }

        public static BitmapSource GetBitmapFromRGBArray(byte[] imageData, int width, int height, PixelFormat formats)
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

        public static BitmapSource? LoadBitmapFromFile(string filePath, bool isFreeze = true)
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


        public static BitmapSource ConvertBitmapToBitmapSource(System.Drawing.Bitmap bitmap)
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

        public static string PaletteColourToString(PaletteColour color)
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

        public static void Print2DArrayToFile(PaletteColour[] arr, int rows, int cols, string relativePath)
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
                        outputFile.Write(PaletteColourToString(arr[index]));
                        outputFile.Write('\t');
                    }

                    outputFile.WriteLine();
                }

                Console.WriteLine($"Đã ghi vào tệp '{outputPath}'");
            }
        }
    }
}
