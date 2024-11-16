using ArtWiz.View.Base;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ArtWiz.View.Utils;

namespace ArtWiz.View.Pages
{
    public class Item
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
    /// <summary>
    /// Interaction logic for PakEditorPage.xaml
    /// </summary>
    public partial class PakEditorPage : BasePageViewer
    {
        public override object ViewModel => DataContext;
        public override string PageName => "PAK EDITOR";
        private Window ownerWindow;

        public PakEditorPage(IWindowViewer ownerWindow) : base(ownerWindow)
        {
            InitializeComponent();
            this.ownerWindow = (Window)ownerWindow;

            var items = new List<Item>
            {
                new Item { Name = "Item 1", Description = "Description for item 1" },
                new Item { Name = "Item 2", Description = "Description for item 2" }
            };

            // Gán danh sách vào ItemsSource của ListBox
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

        }
    }
}
