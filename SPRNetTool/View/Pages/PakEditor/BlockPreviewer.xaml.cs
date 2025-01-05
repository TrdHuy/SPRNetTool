using ArtWiz.Utils;
using ArtWiz.ViewModel.CommandVM;
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
using ArtWiz.ViewModel.PakEditor;

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
                        DataContext.IfIs<PakBlockItemViewModel>(it2 =>
                        {
                            if (it2.Parents is PakFileItemViewModel vm)
                            {
                                vm.CurrentSelectedPakBlock = null;
                            }
                        });
                        break;
                    case PakEditorPageId.ExtractBlock:
                        DataContext.IfIs<PakBlockItemViewModel>(it2 =>
                        {
                            if (it2.Parents is PakFileItemViewModel pfVM && pfVM.Parents is PakPageViewModel ppVM)
                            {
                                if (OwnerPage != null && OwnerPage.OwnerWindow is Window w)
                                {
                                    if (string.IsNullOrEmpty(ppVM.BlockFolderOutputPath) || !Directory.Exists(ppVM.BlockFolderOutputPath))
                                    {
                                        using (var folderDialog = new FolderBrowserDialog())
                                        {
                                            folderDialog.Description = "Chọn thư mục để lưu block đã extract:";
                                            folderDialog.ShowNewFolderButton = true;

                                            if (folderDialog.ShowDialog() == DialogResult.OK)
                                            {
                                                ppVM.BlockFolderOutputPath = folderDialog.SelectedPath;
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
                                            (ppVM as IPakPageCommand).OnExtractCurrentSelectedBlock();
                                        });
                                    });
                                }
                                else
                                {
                                    (ppVM as IPakPageCommand).OnExtractCurrentSelectedBlock();
                                }
                            }
                        });
                        break;
                }
            });
        }
    }
}
