using Microsoft.Win32;
using ArtWiz.Utils;
using ArtWiz.View.Base;
using ArtWiz.View.Widgets;
using ArtWiz.ViewModel;
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
using System.ComponentModel.Design;

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

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {

        }
    }
}
