using ArtWiz.Utils;
using ArtWiz.ViewModel;
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

namespace ArtWiz.View.Pages.PakEditor
{
    /// <summary>
    /// Interaction logic for OriginalPakFileItem.xaml
    /// </summary>
    public partial class OriginalPakFileItem : UserControl
    {
        public OriginalPakFileItem()
        {
            InitializeComponent();
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement)?.Tag.IfIs<PakEditorPageId>(it =>
            {
                switch (it)
                {
                    case PakEditorPageId.StartLoadPakFile:
                        DataContext.IfIs<PakFileItemViewModel>(it2 =>
                        {
                            it2.StartLoadPakFileToWorkManagerAsync();
                        });
                        break;
                    case PakEditorPageId.RemoveFilePak:
                        DataContext.IfIs<PakFileItemViewModel>(it2 =>
                        {
                            it2.Parents.IfIs<IPakPageCommand>(it3 =>
                            {
                                it3.OnRemovePakFileClick(it2);
                            });
                        });
                        break;
                }
            });
        }
    }
}
