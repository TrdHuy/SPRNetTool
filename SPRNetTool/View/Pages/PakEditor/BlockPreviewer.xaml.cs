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

namespace ArtWiz.View.Pages.PakEditor
{
    /// <summary>
    /// Interaction logic for BlockPreviewer.xaml
    /// </summary>
    public partial class BlockPreviewer : UserControl
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
                    case PakEditorPageId.RemoveFilePak:

                        break;
                }
            });
        }
    }
}
