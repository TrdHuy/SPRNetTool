using ArtWiz.View.Base;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ArtWiz.View.Utils;
using ArtWiz.Utils;
using ArtWiz.ViewModel.CommandVM;

namespace ArtWiz.View.Pages
{
    public class Item
    {
        public string ItemName { get; set; }
        public string FileSize { get; set; }
    }

    public enum PakEditorPageId
    {
        AddFilePak,
        RemoveFilePak,
        ReloadFilePak
    }
    /// <summary>
    /// Interaction logic for PakEditorPage.xaml
    /// </summary>
    public partial class PakEditorPage : BasePageViewer
    {
        public override object ViewModel => DataContext;
        public override string PageName => "PAK EDITOR";
        private Window ownerWindow;
        private IPakPageCommand? commandVM;

        public PakEditorPage(IWindowViewer ownerWindow) : base(ownerWindow)
        {
            InitializeComponent();
            this.ownerWindow = (Window)ownerWindow;
            DataContext.IfIsThenAlso<IPakPageCommand>((it) => commandVM = it);

        }
        public override object? CustomHeaderView => CustomHeaderViewPanel;

        public override bool ProcessMenuItem(PreProcessMenuItemInfo menuItem)
        {
            base.ProcessMenuItem(menuItem);
            switch (menuItem.MenuTag)
            {
                case AppMenuTag.HomeMenu:
                    {
                        break;
                    }
                case AppMenuTag.SupportMenu:
                    {
                        menuItem.Visibility = Visibility.Collapsed;
                        return true;
                    }
            }
            return false;
        }
        public override RowDefinition? HeaderRow => HeaderRowDef;
        public override Menu? GetExtraMenuForPage()
        {
            return ExtraMenu;
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement)?.Tag.IfIs<PakEditorPageId>(it =>
            {
                switch (it)
                {
                    case PakEditorPageId.AddFilePak:
                        var openFileDialog = new Microsoft.Win32.OpenFileDialog
                        {
                            Title = "Chọn tệp Pak",
                            Filter = "Pak Files (*.pak)|*.pak|All Files (*.*)|*.*", // Lọc tệp .pak
                            Multiselect = false // Không cho phép chọn nhiều tệp
                        };

                        if (openFileDialog.ShowDialog() == true)
                        {
                            string filePath = openFileDialog.FileName;
                            commandVM?.OnAddedPakFileClick(filePath); // Gửi filePath vào ViewModel
                        }
                        break;
                    case PakEditorPageId.RemoveFilePak:
                        commandVM?.OnRemovePakFileClick((sender as FrameworkElement)!.DataContext);
                        break;
                }
            });
        }
    }
}
