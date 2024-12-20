using ArtWiz.View.Utils;
using System.Windows;
using System.Windows.Controls;

namespace ArtWiz.View.Base
{
    public interface IPageViewer : IViewerElement
    {
        IWindowViewer OwnerWindow { get; }

        public bool ProcessHitTest(Window owner, Point mousePositionFromScreen);
        public object? CustomHeaderView { get; }
        public bool ProcessMenuItem(PreProcessMenuItemInfo menuItem);
        public Menu? GetExtraMenuForPage();
        public RowDefinition? HeaderRow { get; }
    }
}
