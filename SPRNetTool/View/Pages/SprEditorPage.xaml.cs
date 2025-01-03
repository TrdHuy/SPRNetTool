using Microsoft.Win32;
using ArtWiz.Utils;
using ArtWiz.View.Base;
using ArtWiz.View.Widgets;
using ArtWiz.ViewModel.CommandVM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static ArtWiz.View.InputWindow;
using static ArtWiz.View.Widgets.PaletteEditor;
using ArtWiz.View.Base.Windows;
using ArtWiz.ViewModel.SprEditor;

namespace ArtWiz.View.Pages
{
    public enum SprEditorPageTagId
    {
        OpenImageFile,
        SaveImageFile,
        PlayPauseSprAnimation,
        ImportToSprWorkSpace,
        ResetSprWorkSpace,
    }
    /// <summary>
    /// Interaction logic for SprEditorPage.xaml
    /// </summary>
    public partial class SprEditorPage : BasePageViewer
    {
        public override object ViewModel => DataContext;

        public override string PageName => "SPR EDITOR";

        private IDebugPageCommand? commandVM;
        private DebugPageViewModel? viewModel;
        private Window ownerWindow;

        public SprEditorPage(IWindowViewer ownerWindow) : base(ownerWindow)
        {
            InitializeComponent();
            DataContext.IfIsThenAlso<IDebugPageCommand>((it) => commandVM = it);
            DataContext.IfIsThenAlso<DebugPageViewModel>((it) => viewModel = it);
            this.ownerWindow = (Window)ownerWindow;
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


        private void OnFramePreviewerMouseClick(object sender, FameLineMouseEventArgs args)
        {
            commandVM?.OnFramePointerClick((uint)args.FrameIndex);
        }

        private void PaletteEditorPreviewColorItemChange(object sender, PaletteEditorEventChangedArgs arg)
        {
            commandVM?.OnPreviewColorPaletteChanged((uint)arg.ColorIndex, arg.NewColor);
            arg.Handled = true;
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement)?.Tag.IfIs<SprEditorPageTagId>(it =>
            {
                switch (it)
                {
                    case SprEditorPageTagId.PlayPauseSprAnimation:
                        {
                            var success = commandVM?.OnPlayPauseAnimationSprClicked();
                            if (success == false)
                            {
                                PlayPauseButton.IsChecked = !PlayPauseButton.IsChecked;
                            }
                            break;
                        }
                    case SprEditorPageTagId.OpenImageFile:
                        {
                            OpenFileDialog openFileDialog = new OpenFileDialog();
                            openFileDialog.Filter = "Tệp ảnh |*.png;*.jpg;*.jpeg;*.spr";
                            if (openFileDialog.ShowDialog() == true)
                            {
                                string imagePath = openFileDialog.FileName;

                                string fileExtension = Path.GetExtension(imagePath).ToLower();
                                if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png" || fileExtension == ".spr")
                                {
                                    LoadingWindow l = new LoadingWindow(ownerWindow);
                                    l.Show(block: async () =>
                                    {
                                        await (commandVM?.OnOpenImageFromFileClickAsync(imagePath) ?? Task.CompletedTask);
                                    });
                                }
                            }
                            break;
                        }
                    case SprEditorPageTagId.SaveImageFile:
                        {
                            var builder = new InputBuilder();
                            var SavingTitle = "Lưu với định dạng";
                            var SavingDes = "Save";
                            List<string> SavingOptions = new List<string>() { "jpg", "png", "spr" };
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
                                            if (viewModel?.CurrentlyDisplayedBitmapSource == null) return;
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
                                        if (viewModel?.IsSpr == true)
                                        {
                                            commandVM?.OnSaveCurrentWorkManagerToFileSprClicked(filePath);
                                        }
                                        else
                                        {
                                            commandVM?.OnSaveCurrentDisplayedBitmapSourceToSpr(Path.ChangeExtension(filePath, "spr"));
                                        }
                                    }
                                });
                            }
                            break;
                        }
                    case SprEditorPageTagId.ImportToSprWorkSpace:
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
                    case SprEditorPageTagId.ResetSprWorkSpace:
                        {
                            commandVM?.OnResetSprWorkspaceClicked();
                            break;
                        }
                }
            });
        }

    }
}
