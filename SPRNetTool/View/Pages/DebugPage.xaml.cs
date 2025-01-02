using Microsoft.Win32;
using ArtWiz.Utils;
using ArtWiz.View.Base;
using ArtWiz.View.Utils;
using ArtWiz.View.Widgets;
using ArtWiz.ViewModel;
using ArtWiz.ViewModel.Base;
using ArtWiz.ViewModel.CommandVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static ArtWiz.View.InputWindow;
using static ArtWiz.View.Widgets.PaletteEditor;
using ArtWiz.View.Base.Windows;

namespace ArtWiz.View.Pages
{
    public enum DebugPageTagID
    {
        OptimizeList_RGBHeader,
        OptimizeList_ARGBHeader,
        OptimizeList_CombineRGBHeader,
        OriginalList_RGBHeader,
        OriginalList_CountHeader,

        SPRInfo_PlayButton,
        SPRInfo_FrameIndexPlusButton,
        SPRInfo_FrameIndexMinusButton,
        SPRInfo_FrameOffsetXPlusButton,
        SPRInfo_FrameOffsetXMinusButton,
        SPRInfo_FrameOffsetYPlusButton,
        SPRInfo_FrameOffsetYMinusButton,
        SPRInfo_IntervalMinusButton,
        SPRInfo_IntervalPlusButton,
        SPRInfo_FrameWidthMinusButton,
        SPRInfo_FrameWidthPlusButton,
        SPRInfo_FrameHeightMinusButton,
        SPRInfo_FrameHeightPlusButton,
        SPRInfo_GlobalWidthMinusButton,
        SPRInfo_GlobalWidthPlusButton,
        SPRInfo_GlobalHeightMinusButton,
        SPRInfo_GlobalHeightPlusButton,
        SPRInfo_GlobalOffsetXPlusButton,
        SPRInfo_GlobalOffsetXMinusButton,
        SPRInfo_GlobalOffsetYPlusButton,
        SPRInfo_GlobalOffsetYMinusButton,
        SPRInfo_GlobalWidthBalloonBox,
        SPRInfo_GlobalHeightBalloonBox,
        SPRInfo_GlobalOffXBalloonBox,
        SPRInfo_GlobalOffYBalloonBox,
        SPRInfo_FrameWidthBalloonBox,
        SPRInfo_FrameHeightBalloonBox,
        SPRInfo_FrameOffXBalloonBox,
        SPRInfo_FrameOffYBalloonBox,
        SPRInfo_SprIntervalBalloonBox,

        ImageInfo_ExportToSingleFrameSprFile,
        ImageInfo_ImportToNextFrameOfSprWorkSpace,
    }

    public partial class DebugPage : BasePageViewer
    {
        private Window ownerWindow;

        public override object ViewModel => DataContext;

        public override string PageName => "Debug page";

        //TODO: remove this because it belong to domain layer
        private DebugPageViewModel viewModel;
        private IDebugPageCommand? commandVM;


        public DebugPage(IWindowViewer ownerWindow) : base(ownerWindow)
        {
            InitializeComponent();
            viewModel = (DebugPageViewModel)DataContext;
            this.ownerWindow = (Window)ownerWindow;
            commandVM = DataContext.IfIsThenAlso<IDebugPageCommand>((it) => it);
        }

        private void OpenImageClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Tệp ảnh |*.png;*.jpg;*.jpeg;*.spr;*.bin";
            if (openFileDialog.ShowDialog() == true)
            {
                BitmapSource? bmpSource = null;
                string imagePath = openFileDialog.FileName;

                string fileExtension = Path.GetExtension(imagePath).ToLower();
                if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png" || fileExtension == ".spr")
                {
                    LoadingWindow l = new LoadingWindow(ownerWindow);
                    l.Show(block: async () =>
                    {
                        await (commandVM?.OnOpenImageFromFileClickAsync(imagePath) ?? Task.CompletedTask);
                        bmpSource = viewModel.CurrentlyDisplayedBitmapSource;

                        Debug.WriteLine($"WxH= {bmpSource?.PixelWidth * bmpSource?.PixelHeight}");
                    });
                }
                else if (fileExtension == ".bin")
                {
                    using (FileStream fs = new FileStream(imagePath, FileMode.Open))
                    {
                        // Tạo một mảng byte với kích thước bằng kích thước của file
                        byte[] data = new byte[fs.Length];

                        // Đọc dữ liệu từ FileStream vào mảng byte
                        fs.Read(data, 0, data.Length);

                        WriteableBitmap bitmap = new WriteableBitmap(319, 319, 96, 96, PixelFormats.Bgra32, null);

                        // Gán dữ liệu từ mảng imageData vào WriteableBitmap
                        bitmap.Lock();
                        bitmap.WritePixels(new Int32Rect(0, 0, 319, 319), data, 319 * 4, 0);
                        bitmap.Unlock();
                        bitmap.Freeze();
                        viewModel.CurrentlyDisplayedOptimizedBitmapSource = bitmap;
                    }
                }
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

            var srcInput = builder.AddTextInputOption(colorCountKey
                , colorCountKey
                , colorCountDef
                , (cur, input) => input.Any(char.IsNumber) && Convert.ToInt32(cur + input) <= 256)
                .AddTextInputOption(deltaKey
                , deltaDes
                , deltaDef
                , (cur, input) => input.Any(char.IsNumber))
                .AddCheckBoxOption(isUsingAlphaKey
                , isUsingAlphaKey
                , isUsingAlphaDef
                , () => true
                , (src, isChecked) =>
                {
                    src[3].IsDisabled = !isChecked;
                    src[4].IsDisabled = !isChecked;
                })
                .AddTextInputOption(deltaForCompareRecalculateKey
                , deltaForCompareRecalculateDes
                , deltaForCompareRecalculateDef
                , (cur, input) => input.Any(char.IsNumber) && Convert.ToInt32(cur + input) <= 500)
                .AddComboBoxOption(backgroundForBlendColorKey
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

            InputWindow inputWindow = new InputWindow(srcInput, ownerWindow, (res) =>
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

            LoadingWindow l = new LoadingWindow(ownerWindow, "Optimizing!");
            l.Show(block: async () =>
            {
                if (viewModel.OriginalColorSource.Count == 0) return;
                await Task.Run(() =>
                {
                    viewModel.OptimizeImageColor(colorSize: colorSize,
                    colorDifferenceDelta: delta,
                    isUsingAlpha: isUsingAlpha,
                    colorDifferenceDeltaForCalculatingAlpha: deltaForCompareRecalculate,
                    backgroundForBlendColor: backgroundForBlendColor);
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
            if (tag == null || !(tag is DebugPageTagID)) return;
            tag = (DebugPageTagID)tag;
            switch (tag)
            {
                case DebugPageTagID.OptimizeList_CombineRGBHeader:
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
                case DebugPageTagID.OptimizeList_RGBHeader:
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
                case DebugPageTagID.OptimizeList_ARGBHeader:
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
                case DebugPageTagID.OriginalList_CountHeader:
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
                case DebugPageTagID.OriginalList_RGBHeader:
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

        private void ClearClick(object sender, RoutedEventArgs e)
        {
            commandVM?.OnResetSprWorkspaceClicked();
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

        private async void ResizeImageClick(object sender, RoutedEventArgs e)
        {
            if (viewModel.BitmapViewerVM.FrameSource == null) return;
            var image = viewModel.BitmapViewerVM.FrameSource as BitmapSource;
            if (image == null) return;

            var oldBytes = image.ToRawByteArray();
            var oldColorSrc = await BitmapUtil.CountColorsFromByteArrayAsync(oldBytes, image.Format);
            viewModel.SetColorSource(oldColorSrc);
            double newHeight = 300d;
            double newWidth = 300d;
            double oldHeight = (double)image.PixelHeight;
            double oldWidth = (double)image.PixelWidth;
            TransformedBitmap resizedBitmap = new TransformedBitmap(image, new ScaleTransform(newWidth / oldWidth, newHeight / oldHeight));
            StaticImageView2.Source = resizedBitmap;

            var newBytes = resizedBitmap.ToRawByteArray();
            var newColorSrc = await BitmapUtil.CountColorsFromByteArrayAsync(newBytes, resizedBitmap.Format);
            viewModel.SetResultRGBColorSource(await CreateColorSourceItems(newColorSrc));

        }

        private async Task<ObservableCollection<ColorItemViewModel>> CreateColorSourceItems(Dictionary<Color, long> src)
        {
            var newCountedSrc = new ObservableCollection<ColorItemViewModel>();
            await Task.Run(() =>
            {
                foreach (var color in src)
                {
                    var newColor = color.Key;
                    newCountedSrc.Add<ColorItemViewModel>(new ColorItemViewModel { ItemColor = newColor, Count = color.Value });
                }
            });
            return newCountedSrc;
        }

        private void OnRunLeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            (sender as TextBlock)?.Tag.IfIs<DebugPageTagID>((tag) =>
            {
                switch (tag)
                {
                    case DebugPageTagID.ImageInfo_ExportToSingleFrameSprFile:
                        {
                            SaveFileDialog saveFileDialog = new SaveFileDialog();
                            if (saveFileDialog.ShowDialog() == true)
                            {
                                LoadingWindow l = new LoadingWindow(ownerWindow, "Saving to Spr file!");
                                l.Show(block: async () =>
                                {
                                    await Task.Run(() =>
                                    {
                                        commandVM?.OnSaveCurrentDisplayedBitmapSourceToSpr(Path.ChangeExtension(saveFileDialog.FileName, "spr"));
                                    });
                                });
                            }
                            break;
                        }
                    case DebugPageTagID.ImageInfo_ImportToNextFrameOfSprWorkSpace:
                        {
                            LoadingWindow l = new LoadingWindow(ownerWindow, "Exporting to next frame of SprWorkSpace!");
                            l.Show(block: async () =>
                            {
                                await Task.Run(() =>
                                {
                                    commandVM?.OnImportCurrentDisplaySourceToNextFrameOfSprWorkSpace();
                                });
                            });
                            break;
                        }

                }
            });
            (sender as Run)?.Tag.IfIs<DebugPageTagID>((tag) =>
            {
                switch (tag)
                {
                    case DebugPageTagID.SPRInfo_PlayButton:
                        {
                            commandVM?.OnPlayPauseAnimationSprClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameIndexMinusButton:
                        {
                            commandVM?.OnDecreaseCurrentlyDisplayedSprFrameIndex();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameIndexPlusButton:
                        {
                            commandVM?.OnIncreaseCurrentlyDisplayedSprFrameIndex();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameOffsetXMinusButton:
                        {
                            commandVM?.OnDecreaseFrameOffsetXButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameOffsetXPlusButton:
                        {
                            commandVM?.OnIncreaseFrameOffsetXButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameOffsetYMinusButton:
                        {
                            commandVM?.OnDecreaseFrameOffsetYButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameOffsetYPlusButton:
                        {
                            commandVM?.OnIncreaseFrameOffsetYButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_IntervalPlusButton:
                        {
                            commandVM?.OnIncreaseIntervalButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_IntervalMinusButton:
                        {
                            commandVM?.OnDecreaseIntervalButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameWidthMinusButton:
                        {
                            commandVM?.OnDecreaseFrameWidthButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameWidthPlusButton:
                        {
                            commandVM?.OnIncreaseFrameWidthButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameHeightMinusButton:
                        {
                            commandVM?.OnDecreaseFrameHeightButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameHeightPlusButton:
                        {
                            commandVM?.OnIncreaseFrameHeightButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalOffsetXMinusButton:
                        {
                            commandVM?.OnDecreaseSprGlobalOffsetXButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalOffsetXPlusButton:
                        {
                            commandVM?.OnIncreaseSprGlobalOffsetXButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalOffsetYMinusButton:
                        {
                            commandVM?.OnDecreaseSprGlobalOffsetYButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalOffsetYPlusButton:
                        {
                            commandVM?.OnIncreaseSprGlobalOffsetYButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalWidthMinusButton:
                        {
                            commandVM?.OnDecreaseSprGlobalWidthButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalWidthPlusButton:
                        {
                            commandVM?.OnIncreaseSprGlobalWidthButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalHeightMinusButton:
                        {
                            commandVM?.OnDecreaseSprGlobalHeightButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalHeightPlusButton:
                        {
                            commandVM?.OnIncreaseSprGlobalHeightButtonClicked();
                            break;
                        }
                }
            });
        }

        private void SaveCurrentSourceClick(object sender, RoutedEventArgs e)
        {
            var builder = new InputBuilder();
            var SavingTitle = "Lưu với định dạng";
            var SavingDes = "Save";
            List<string> SavingOptions = new List<string>() { "jpg", "png", "spr", "bin" };
            var inputSrc = builder.AddRadioOptions(SavingTitle, SavingDes, SavingOptions).Build();
            var checkedContent = "";
            InputWindow inputWindow = new InputWindow(inputSrc, ownerWindow, (res) =>
            {
                if (res != null)
                {
                    foreach (var item in res)
                    {
                        if (item.Key != null) checkedContent = Convert.ToString(item.Value);
                        break;
                    }
                }
            });
            Res res = inputWindow.Show();
            if (res == Res.CANCEL) return;

            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.AddExtension = true;
            saveFile.DefaultExt = checkedContent;
            if (saveFile.ShowDialog() == true)
            {
                string filePath = saveFile.FileName;
                LoadingWindow l = new LoadingWindow(ownerWindow, "Saving to " + checkedContent + " file!");
                l.Show(block: async () =>
                {
                    if (checkedContent == "jpg" || checkedContent == "png")
                    {
                        using (FileStream stream = new FileStream(filePath, FileMode.Create))
                        {
                            if (viewModel.CurrentlyDisplayedBitmapSource == null) return;
                            await Task.Run(() =>
                            {
                                BitmapEncoder? encoder = null;
                                switch (checkedContent)
                                {
                                    case "jpg":
                                        encoder = new JpegBitmapEncoder();
                                        break;
                                    case "png":
                                        encoder = new PngBitmapEncoder();
                                        break;
                                    default:
                                        return;
                                }
                                encoder.Frames.Add(BitmapFrame.Create(viewModel.CurrentlyDisplayedBitmapSource));
                                encoder.Save(stream);
                            });
                        }
                    }
                    else if (checkedContent == "spr")
                    {
                        commandVM?.OnSaveCurrentWorkManagerToFileSprClicked(filePath);
                    }
                    else if (checkedContent == "bin")
                    {
                        using (FileStream fs = new FileStream(filePath, FileMode.Create))
                        {
                            var bmp = viewModel.CurrentlyDisplayedBitmapSource;
                            bmp?.Apply(it =>
                            {
                                int width = bmp.PixelWidth;
                                int height = bmp.PixelHeight;
                                int stride = (width * bmp.Format.BitsPerPixel + 7) / 8;
                                byte[] pixelData = new byte[stride * height];
                                bmp.CopyPixels(pixelData, stride, 0);

                                fs.Write(pixelData, 0, pixelData.Length);
                            });

                        }
                    }
                });
            }
        }

        private void OnRunMouseHold(object sender, MouseHoldEventArgs args)
        {
            (sender as Run)?.Tag.IfIs<DebugPageTagID>((tag) =>
            {
                switch (tag)
                {

                    case DebugPageTagID.SPRInfo_FrameIndexMinusButton:
                        {
                            commandVM?.OnDecreaseCurrentlyDisplayedSprFrameIndex();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameIndexPlusButton:
                        {
                            commandVM?.OnIncreaseCurrentlyDisplayedSprFrameIndex();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameOffsetXMinusButton:
                        {
                            commandVM?.OnDecreaseFrameOffsetXButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameOffsetXPlusButton:
                        {
                            commandVM?.OnIncreaseFrameOffsetXButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameOffsetYMinusButton:
                        {
                            commandVM?.OnDecreaseFrameOffsetYButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameOffsetYPlusButton:
                        {
                            commandVM?.OnIncreaseFrameOffsetYButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_IntervalPlusButton:
                        {
                            commandVM?.OnIncreaseIntervalButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_IntervalMinusButton:
                        {
                            commandVM?.OnDecreaseIntervalButtonClicked();
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameWidthMinusButton:
                        {
                            commandVM?.OnDecreaseFrameWidthButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameWidthPlusButton:
                        {
                            commandVM?.OnIncreaseFrameWidthButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameHeightMinusButton:
                        {
                            commandVM?.OnDecreaseFrameHeightButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_FrameHeightPlusButton:
                        {
                            commandVM?.OnIncreaseFrameHeightButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalOffsetXMinusButton:
                        {
                            commandVM?.OnDecreaseSprGlobalOffsetXButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalOffsetXPlusButton:
                        {
                            commandVM?.OnIncreaseSprGlobalOffsetXButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalOffsetYMinusButton:
                        {
                            commandVM?.OnDecreaseSprGlobalOffsetYButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalOffsetYPlusButton:
                        {
                            commandVM?.OnIncreaseSprGlobalOffsetYButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalWidthMinusButton:
                        {
                            commandVM?.OnDecreaseSprGlobalWidthButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalWidthPlusButton:
                        {
                            commandVM?.OnIncreaseSprGlobalWidthButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalHeightMinusButton:
                        {
                            commandVM?.OnDecreaseSprGlobalHeightButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                    case DebugPageTagID.SPRInfo_GlobalHeightPlusButton:
                        {
                            commandVM?.OnIncreaseSprGlobalHeightButtonClicked((uint)(1 + args.HoldingCounter / 5));
                            break;
                        }
                }
            });
        }

        private void BasePageViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox)
            {
                Keyboard.ClearFocus();
            }
        }

        private void OnBalloonBoxPreviewTextContentUpdated(object sender, TextContentUpdatedEventArgs e)
        {
            (sender as BalloonBox)?.Tag.IfIs<DebugPageTagID>(tag =>
            {
                switch (tag)
                {
                    case DebugPageTagID.SPRInfo_GlobalWidthBalloonBox:
                        commandVM?.SetSprGlobalSize(width: (ushort)Convert.ToUInt32(e.NewText));
                        break;
                    case DebugPageTagID.SPRInfo_GlobalHeightBalloonBox:
                        commandVM?.SetSprGlobalSize(height: (ushort)Convert.ToUInt32(e.NewText));
                        break;
                    case DebugPageTagID.SPRInfo_GlobalOffXBalloonBox:
                        commandVM?.SetSprGlobalOffset(offX: (short)Convert.ToInt32(e.NewText));
                        break;
                    case DebugPageTagID.SPRInfo_GlobalOffYBalloonBox:
                        commandVM?.SetSprGlobalOffset(offY: (short)Convert.ToInt32(e.NewText));
                        break;
                    case DebugPageTagID.SPRInfo_FrameHeightBalloonBox:
                        commandVM?.SetSprFrameSize(height: (ushort)Convert.ToInt32(e.NewText));
                        break;
                    case DebugPageTagID.SPRInfo_FrameWidthBalloonBox:
                        commandVM?.SetSprFrameSize(width: (ushort)Convert.ToInt32(e.NewText));
                        break;
                    case DebugPageTagID.SPRInfo_FrameOffYBalloonBox:
                        commandVM?.SetSprFrameOffset(offY: (short)Convert.ToInt32(e.NewText));
                        break;
                    case DebugPageTagID.SPRInfo_FrameOffXBalloonBox:
                        commandVM?.SetSprFrameOffset(offX: (short)Convert.ToInt32(e.NewText));
                        break;
                    case DebugPageTagID.SPRInfo_SprIntervalBalloonBox:
                        commandVM?.SetSprInterval((ushort)Convert.ToInt32(e.NewText));
                        break;
                }
                e.Handled = true;
            });
        }

        private void OnPreviewFrameIndexSwitched(object sender, FrameLineEventArgs args)
        {
            if (args.SwitchedFrame1Index >= 0 && args.SwitchedFrame2Index >= 0)
            {
                commandVM?.OnSwitchFrameClicked((uint)args.SwitchedFrame1Index, (uint)args.SwitchedFrame2Index);
                args.Handled = true;
            }
        }

        private void OnPreviewRemovingFrame(object sender, FrameLineEventArgs args)
        {
            if (args.OldFrameIndex >= 0)
            {
                commandVM?.OnRemoveFrameClicked((uint)args.OldFrameIndex);
                args.Handled = true;
            }
        }

        private void OnPreviewAddingFrame(object sender, FrameLineEventArgs args)
        {
            if (args.NewFrameIndex >= 0)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Tệp ảnh |*.png;*.jpg;*.jpeg;*.spr";
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == true)
                {
                    string[] imagePaths = openFileDialog.FileNames
                        .Where(it =>
                        {
                            string fileExtension = Path.GetExtension(it).ToLower();
                            return fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png";
                        })
                        .ToArray();

                    if (imagePaths.Length > 0)
                    {
                        LoadingWindow l = new LoadingWindow(ownerWindow, tilte: "Inserting new frame");
                        l.Show(block: async () =>
                        {
                            await Task.Run(() =>
                            {
                                commandVM?.OnInsertFrameClicked((uint)args.NewFrameIndex, imagePaths);
                            });
                        });
                    }
                }

                args.Handled = true;
            }
        }

        private void OnEllipseMouseClick(object sender, MouseButtonEventArgs args)
        {
            //sender.IfIs<EllipseController>(it =>
            //{
            //    commandVM?.OnFramePointerClick(it.CurrentIndex);
            //});
        }

        private void PaletteEditorPreviewColorItemChange(object sender, PaletteEditorEventChangedArgs arg)
        {
            commandVM?.OnPreviewColorPaletteChanged((uint)arg.ColorIndex, arg.NewColor);
            arg.Handled = true;
        }

        private void ReloadColorSourceClick(object sender, RoutedEventArgs e)
        {
            LoadingWindow l = new LoadingWindow(ownerWindow, tilte: "Reloading color source");
            l.Show(block: async () =>
            {
                await Task.Run(() =>
                {
                    commandVM?.OnReloadColorSourceClick();
                });
            });
        }

        private void OnEllipseMouseClick(object sender, FameLineMouseEventArgs args)
        {
            commandVM?.OnFramePointerClick((uint)args.FrameIndex);
        }
    }
}

