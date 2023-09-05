﻿using Microsoft.Win32;
using SPRNetTool.Data;
using SPRNetTool.Domain;
using SPRNetTool.Utils;
using SPRNetTool.View.Base;
using SPRNetTool.ViewModel;
using SPRNetTool.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static SPRNetTool.View.InputWindow;

namespace SPRNetTool.View
{
    public enum MainWindowTagID
    {
        OptimizeList_RGBHeader,
        OptimizeList_ARGBHeader,
        OptimizeList_CombineRGBHeader,
        OriginalList_RGBHeader,
        OriginalList_CountHeader,
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INetView
    {
        WorkManager workManager = new WorkManager();
        MainWindowViewModel viewModel;
        public MainWindow()
        {
            InitializeComponent();
            viewModel = (MainWindowViewModel)DataContext;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string filePath = "Resources/cuukiem.spr".FullPath();
            try
            {

                US_SprFileHead header;
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var temp = fs.BinToStruct<US_SprFileHead>(0);

                    if (temp != null && (temp?.GetVersionInfoStr().StartsWith("SPR") ?? false))
                    {
                        header = (US_SprFileHead)temp;
                        workManager.Init();
                        workManager.InitFromFileHead(header);
                    }
                    else
                    {
                        return;
                    }
                    workManager.PaletteData = new Palette(workManager.FileHead.ColourCounts);

                    fs.Position = Marshal.SizeOf(typeof(US_SprFileHead));
                    for (int i = 0; i < workManager.FileHead.ColourCounts; i++)
                    {
                        workManager.PaletteData.Data[i].Red = (byte)fs.ReadByte();
                        workManager.PaletteData.Data[i].Green = (byte)fs.ReadByte();
                        workManager.PaletteData.Data[i].Blue = (byte)fs.ReadByte();
                    }
                    workManager.FrameDataBegPos = fs.Position;

                    workManager.InitFrameData(fs);

                    //if (decdata != null)
                    //    Extension.Print2DArrayToFile(decdata, frameHeight, frameWidth, "test.txt");
                    var data = workManager.FrameData?[0];
                    if (data != null)
                    {
                        //Extension.Print2DArrayToFile(data?.decodedFrameData, data?.frameHeight ?? 0, data?.frameWidth ?? 0, "dec.txt");
                        BitmapUtil.Print2DArrayToFile(data?.globleFrameData, workManager.FileHead.GlobleHeight, workManager.FileHead.GlobleWidth, "glb.txt");
                        var byteData = BitmapUtil.ConvertPaletteColourArrayToByteArray(data?.globleFrameData);
                        var bmpSrc = BitmapUtil.GetBitmapFromRGBArray(byteData, workManager.FileHead.GlobleWidth, workManager.FileHead.GlobleHeight, PixelFormats.Bgra32);
                        StaticImageView.Source = bmpSrc;
                        BitmapUtil.CountColors(bmpSrc, out long argbCount, out long rgbCount, out Dictionary<Color, long> src);

                        //var ditheringBmp = BitmapUtil.ApplyDithering(bmpSrc, 100);
                        //CountColors(ditheringBmp, out long argbCount2, out long rgbCount2);
                        //StaticImageView2.Source = ditheringBmp;

                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
            }
        }


        private void OpenImageClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Tệp ảnh (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                BitmapSource? bmpSource = null;
                LoadingWindow l = new LoadingWindow(this);
                l.Show(block: async () =>
                {
                    string imagePath = openFileDialog.FileName;
                    bmpSource = BitmapUtil.LoadBitmapFromFile(imagePath);
                    StaticImageView.Source = bmpSource;
                    if (bmpSource == null) { return; }
                    var src = await BitmapUtil.CountColorsAsync(bmpSource);
                    await viewModel.SetColorSource(src);

                    Debug.WriteLine($"WxH= {bmpSource.PixelWidth * bmpSource.PixelHeight}");
                });

            }
        }

        private void OptimizeImageColorClick(object sender, RoutedEventArgs e)
        {
            InputBuilder builder = new InputBuilder();
            var colorCountKey = "Số màu (max = 256)";
            var colorCountDef = "256";
            var deltaKey = "Độ chênh lệch tối đa giữa 2 màu";
            var deltaDes =
                "Độ chênh lệch tối đa giữa 2 màu là tham số thể xác định màu chưa được chọn tiếp theo có nên add vào list các màu\n" +
                "được chọn không.\n\n" +
                "Ví dụ:\n" +
                "Màu đã chọn RGB (10,10,10)\n" +
                "Màu chưa chọn tiếp theo (11,11,11)\n" +
                "Độ chênh lệch = 1,7 < delta = 10 => Màu này sẽ không được chọn vì giống màu đã chọn (10,10,10)";
            var deltaDef = "100";
            var isUsingAlphaKey = "Sử dụng alpha để tính được nhiều màu cho palette";
            var isUsingAlphaDef = false;
            var deltaForCompareRecalculateKey = "Độ chênh lệch màu ARGB";
            var deltaForCompareRecalculateDes =
                "Độ chênh lệch màu ARGB là tham số để xác định màu tiếp theo có cần cân nhắc để tính giá trị cho kênh alpha hay không.\n" +
                "Nếu giữa màu đã chọn và màu chưa được chọn tiếp theo có độ chênh lệch nhỏ hơn 'Độ chênh lệch màu ARGB'\n" +
                "thì màu chưa được chọn tiếp theo sẽ được cân nhắc để tính giá trị Alpha từ màu đã chọn.\n\n" +
                "Ví dụ:\n" +
                "Màu đã chọn RGB (10,10,10)\n" +
                "Màu chưa chọn tiếp theo (11,11,11)\n" +
                "Độ chênh lệch = 1,7 < delta = 10 => Màu này sẽ được cân nhắc tính giá trị alpha dựa theo Màu đã chọn (10,10,10)";
            var deltaForCompareRecalculateDef = "10";
            var backgroundForBlendColorKey = "Màu nền cho blend";
            var backgroundForBlendColorDes = "Màu nền được dùng cho việc blend màu foreground với kênh alpha.\n" +
                "https://stackoverflow.com/questions/1855884/determine-font-color-based-on-background-color";



            var srcInput = builder.Add(colorCountKey
                , colorCountKey
                , colorCountDef
                , (cur, input) => input.Any(char.IsNumber) && Convert.ToInt32(cur + input) <= 256)
                .Add(deltaKey
                , deltaDes
                , deltaDef
                , (cur, input) => input.Any(char.IsNumber))
                .Add(isUsingAlphaKey
                , isUsingAlphaKey
                , isUsingAlphaDef
                , () => true
                , (src, isChecked) =>
                {
                    src[3].IsDisabled = !isChecked;
                    src[4].IsDisabled = !isChecked;
                })
                .Add(deltaForCompareRecalculateKey
                , deltaForCompareRecalculateDes
                , deltaForCompareRecalculateDef
                , (cur, input) => input.Any(char.IsNumber) && Convert.ToInt32(cur + input) <= 10)
                .Add(backgroundForBlendColorKey
                , backgroundForBlendColorDes
                , new List<string> { "WHITE (255,255,255)", "BLACK (0,0,0)" }
                , 0
                , () => true
                , (cur, input) => { })
                .Build();

            int colorSize = 256;
            int delta = 100;
            var isUsingAlpha = false;
            var deltaForCompareRecalculate = 10;
            var backgroundForBlendColor = Colors.White;

            InputWindow inputWindow = new InputWindow(srcInput, this, (res) =>
            {
                colorSize = Convert.ToInt32(res[colorCountKey]);
                delta = Convert.ToInt32(res[deltaKey]);
                isUsingAlpha = Convert.ToBoolean(res[isUsingAlphaKey]);
                deltaForCompareRecalculate = Convert.ToInt32(res[deltaForCompareRecalculateKey]);
                switch (Convert.ToInt32(res[backgroundForBlendColorKey]))
                {
                    case 0:
                        backgroundForBlendColor = Colors.White;
                        break;
                    case 1:
                        backgroundForBlendColor = Colors.Black;
                        break;
                }
            });
            var res = inputWindow.Show();
            if (res == Res.CANCEL) return;

            LoadingWindow l = new LoadingWindow(this, "Optimizing!");
            l.Show(block: async () =>
            {
                if (viewModel.OriginalColorSource.Count == 0) return;

                await Task.Run(async () =>
                {
                    var orderedList = viewModel.OrderByDescendingCount(isSetToDisplaySource: false).ToList();
                    var selectedList = new ObservableCollection<OptimizedColorItemViewModel>();
                    var selectedColorList = new List<Color>();


                    // TODO: Dynamic this
                    var selectedColorRecalculatedAlapha = new List<Color>();
                    var expectedRGBList = new List<Color>();
                    var deltaDistanceForNewARGBColor = 10;
                    var deltaForAlphaAvarageDeviation = 3;

                    // Calculate palette
                    while (selectedList.Count < colorSize && orderedList.Count > 0 && delta >= 0)
                    {
                        for (int i = 0; i < orderedList.Count; i++)
                        {
                            var item = orderedList[i];
                            var expectedColor = item.ItemColor;
                            var shouldAdd = true;
                            foreach (var item2 in selectedList)
                            {
                                var distance = expectedColor.CalculateEuclideanDistance(item2.ItemColor);
                                if (distance < delta)
                                {
                                    if (isUsingAlpha && distance < deltaForCompareRecalculate)
                                    {
                                        var alpha = item2.ItemColor.FindAlphaColors(backgroundForBlendColor, expectedColor, out byte averageAbsoluteDeviation);
                                        var newRGBColor = Color.FromArgb(alpha, item2.ItemColor.R, item2.ItemColor.G, item2.ItemColor.B).BlendColors(backgroundForBlendColor);
                                        var distanceNewRGBColor = newRGBColor.CalculateEuclideanDistance(expectedColor);
                                        if (averageAbsoluteDeviation <= deltaForAlphaAvarageDeviation && distanceNewRGBColor <= deltaDistanceForNewARGBColor)
                                        {
                                            expectedRGBList.Add(expectedColor);
                                            selectedColorRecalculatedAlapha.Add(Color.FromArgb(alpha, item2.ItemColor.R, item2.ItemColor.G, item2.ItemColor.B));
                                            orderedList.RemoveAt(i);
                                            i--;
                                        }
                                    }
                                    shouldAdd = false;
                                    break;
                                }
                            }
                            if (shouldAdd)
                            {
                                selectedList.Add<OptimizedColorItemViewModel>(new OptimizedColorItemViewModel()
                                {
                                    ItemColor = expectedColor
                                });
                                selectedColorList.Add(expectedColor);
                                orderedList.RemoveAt(i);
                                i--;
                            }

                            if (selectedList.Count >= colorSize) break;
                        }
                        delta -= 2;
                    }

                    //Combine RGB and ARGB color to selected list
                    var optimizedRGBCount = selectedList.Count;
                    int iii = 0;
                    foreach (var item in selectedColorRecalculatedAlapha)
                    {
                        selectedList.Add<OptimizedColorItemViewModel>(new OptimizedColorItemViewModel(backgroundForBlendColor)
                        {
                            ItemColor = item,
                            ExpectedColor = expectedRGBList[iii++]
                        });
                    }

                    //reduce same combined color
                    selectedList = selectedList.GroupBy(c => c.CombinedColor).Select(g => g.First()).ToIndexableObservableCollection();
                    var combinedColorList = selectedList.Select(c => c.CombinedColor).ToList();

                    viewModel.SetOptimizedColorSource(selectedList);

                    //======================================================
                    //Dithering
                    BitmapSource? oldBmpSource = null;
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        oldBmpSource = StaticImageView.Source as BitmapSource;

                    }));
                    if (optimizedRGBCount > 0 && optimizedRGBCount <= colorSize && oldBmpSource != null)
                    {
                        //var newBmpSrc = BitmapUtil.FloydSteinbergDithering(oldBmpSource, selectedColorList, isUsingAlpha, selectedColorRecalculatedAlapha, backgroundForBlendColor);
                        var newBmpSrc = BitmapUtil.FloydSteinbergDithering(oldBmpSource, combinedColorList);
                        newBmpSrc?.Freeze();
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            StaticImageView2.Source = newBmpSrc;
                        }));

                        if (newBmpSrc != null)
                        {
                            var src = await BitmapUtil.CountColorsAsync(newBmpSrc);
                            var newCountedSrc = new ObservableCollection<ColorItemViewModel>();
                            await Task.Run(() =>
                            {
                                foreach (var color in src)
                                {
                                    var newColor = color.Key;
                                    newCountedSrc.Add<ColorItemViewModel>(new ColorItemViewModel { ItemColor = newColor, Count = color.Value });
                                }
                            });

                            viewModel.SetResultRGBColorSource(newCountedSrc);
                        }
                    }
                });
            });

        }

        private int originalCountClick = 0;
        private int originalRgbClick = 0;
        private int optimizeCombinedRgbClick = 0;
        private int optimizeRgbClick = 0;
        private int optimizeArgbClick = 0;
        private void HeaderMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var tag = (sender as TextBlock)?.Tag;
            if (tag == null || !(tag is MainWindowTagID)) return;
            tag = (MainWindowTagID)tag;
            switch (tag)
            {
                case MainWindowTagID.OptimizeList_CombineRGBHeader:
                    if (optimizeCombinedRgbClick == 0)
                    {
                        viewModel.OptimizedOrderByCombinedRGB();
                        optimizeCombinedRgbClick = 1;
                    }
                    else if (optimizeCombinedRgbClick == 1)
                    {
                        viewModel.ResetOptimizedOrder();
                        optimizeCombinedRgbClick = 0;
                    }
                    break;
                case MainWindowTagID.OptimizeList_RGBHeader:
                    if (optimizeRgbClick == 0)
                    {
                        viewModel.OptimizedOrderByRGB();
                        optimizeRgbClick = 1;
                    }
                    else if (optimizeRgbClick == 1)
                    {
                        viewModel.ResetOptimizedOrder();
                        optimizeRgbClick = 0;
                    }
                    break;
                case MainWindowTagID.OptimizeList_ARGBHeader:
                    if (optimizeArgbClick == 0)
                    {
                        viewModel.OptimizedOrderByARGB();
                        optimizeArgbClick = 1;
                    }
                    else if (optimizeArgbClick == 1)
                    {
                        viewModel.ResetOptimizedOrder();
                        optimizeArgbClick = 0;
                    }
                    break;
                case MainWindowTagID.OriginalList_CountHeader:
                    if (originalCountClick == 0)
                    {
                        viewModel.OrderByCount();
                        originalCountClick = 1;
                    }
                    else if (originalCountClick == 1)
                    {
                        viewModel.OrderByDescendingCount();
                        originalCountClick = 2;
                    }
                    else if (originalCountClick == 2)
                    {
                        viewModel.ResetOrder();
                        originalCountClick = 0;
                    }
                    break;
                case MainWindowTagID.OriginalList_RGBHeader:
                    if (originalRgbClick == 0)
                    {
                        viewModel.OrderByRGB();
                        originalRgbClick = 1;
                    }
                    else if (originalRgbClick == 1)
                    {
                        viewModel.ResetOrder();
                        originalRgbClick = 0;
                    }
                    break;
            }

        }

        public void DisableWindow(bool isDisabled)
        {
            if (isDisabled)
            {
                DisableLayer.Visibility = Visibility.Visible;
            }
            else
            {
                DisableLayer.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearClick(object sender, RoutedEventArgs e)
        {
            viewModel.ResetViewModel();
            StaticImageView.Source = null;
            StaticImageView2.Source = null;
        }

        private void TestBtnClick(object sender, RoutedEventArgs e)
        {
            var newColor = Color.FromArgb(127, 20, 30, 40).BlendColors(Colors.White);


            var background = Colors.White;
            var foreGround = Color.FromRgb(100, 179, 150);
            var combinedColor = Color.FromRgb(0, 0, 0);

            var colorDistance = foreGround.CalculateEuclideanDistance(combinedColor);
            var alphaChanel = foreGround.FindAlphaColors(background, combinedColor, out byte averageAbsoluteDeviation);

            colorDistance = Color.FromArgb(alphaChanel, 100, 179, 150).CalculateEuclideanDistance(combinedColor);

        }
    }


}

