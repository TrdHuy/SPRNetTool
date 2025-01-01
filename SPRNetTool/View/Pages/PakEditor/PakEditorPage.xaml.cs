using ArtWiz.View.Base;
using System.Windows;
using System.Windows.Controls;
using ArtWiz.View.Utils;
using ArtWiz.Utils;
using ArtWiz.ViewModel.CommandVM;
using System.Windows.Input;
using ArtWiz.View.Widgets;
using System.ComponentModel;
using System;
using ArtWiz.ViewModel;

namespace ArtWiz.View.Pages.PakEditor
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
        ReloadFilePak,
        ClearSearchBox,
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
            DataContext.IfIsThenAlso<INotifyPropertyChanged>((it) =>
            {
                it.PropertyChanged += OnViewModelPropertyChanged;
                return it;
            });
            if (DataContext is PakPageViewModel viewModel)
            {
                SearchTextBox.Visibility = viewModel.SearchBoxVisibility;
                ClearSearchBox.Visibility = viewModel.SearchBoxVisibility;
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (DataContext is PakPageViewModel viewModel)
            {
                if (e.PropertyName == nameof(viewModel.SearchBoxVisibility))
                {
                    SearchTextBox.Visibility = viewModel.SearchBoxVisibility;
                    ClearSearchBox.Visibility = viewModel.SearchBoxVisibility;
                }
            }
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
                    case PakEditorPageId.ClearSearchBox:
                        SearchTextBox.Text = string.Empty;
                        break;
                }
            });
        }

        private void OnSearchBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as PlaceHolderTextBox; // Hoặc kiểu của wg:PlaceHolderTextBox nếu khác
                if (textBox != null)
                {
                    string searchText = textBox.Text;
                    commandVM?.OnSearchPakBlockByPath(searchText);
                }
            }
        }


        private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender.Equals(SearchTextBox))
            {
                if (string.IsNullOrEmpty(SearchTextBox.Text))
                {
                    commandVM?.OnResetSearchBox();
                }
            }
        }
    }
}
