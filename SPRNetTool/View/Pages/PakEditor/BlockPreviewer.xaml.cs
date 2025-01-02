using ArtWiz.Utils;
using ArtWiz.ViewModel.CommandVM;
using ArtWiz.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ArtWiz.View.Base.Windows;
using ArtWiz.View.Base;
using System.IO;
using ArtWiz.LogUtil;
using System.Windows.Forms;

namespace ArtWiz.View.Pages.PakEditor
{
    /// <summary>
    /// Interaction logic for BlockPreviewer.xaml
    /// </summary>
    public partial class BlockPreviewer : BasePageViewerChild
    {
        public BlockPreviewer()
        {
            InitializeComponent();
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement)?.Tag.IfIs<PakEditorPageId>(it =>
            {
                switch (it)
                {
                    case PakEditorPageId.CloseBlockDetailPanel:
                        DataContext.IfIs<PakPageViewModel>(it2 =>
                        {
                            if (it2.CurrentSelectedPakFile != null)
                            {
                                it2.CurrentSelectedPakFile.CurrentSelectedPakBlock = null;
                            }
                        });
                        break;
                    case PakEditorPageId.ExtractBlock:
                        DataContext.IfIs<PakPageViewModel>(it2 =>
                        {
                            if (it2.CurrentSelectedPakFile != null && it2.CurrentSelectedPakFile.CurrentSelectedPakBlock != null)
                            {
                                if (OwnerPage != null && OwnerPage.OwnerWindow is Window w)
                                {
                                    if (string.IsNullOrEmpty(it2.BlockFolderOutputPath) || !Directory.Exists(it2.BlockFolderOutputPath))
                                    {
                                        using (var folderDialog = new FolderBrowserDialog())
                                        {
                                            folderDialog.Description = "Chọn thư mục để lưu block đã extract:";
                                            folderDialog.ShowNewFolderButton = true;

                                            if (folderDialog.ShowDialog() == DialogResult.OK)
                                            {
                                                it2.BlockFolderOutputPath = folderDialog.SelectedPath;
                                            }
                                            else
                                            {
                                                return;
                                            }
                                        }
                                    }
                                    LoadingWindow l = new LoadingWindow(w, "Extracting block!");
                                    l.Show(block: async () =>
                                    {
                                        await Task.Run(() =>
                                        {
                                            (it2 as IPakPageCommand).OnExtractCurrentSelectedBlock();
                                        });
                                    });
                                }
                                else
                                {
                                    (it2 as IPakPageCommand).OnExtractCurrentSelectedBlock();
                                }
                            }
                        });
                        break;

                }
            });
        }
    }
}
