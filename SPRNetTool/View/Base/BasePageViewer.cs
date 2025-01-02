using ArtWiz.Utils;
using ArtWiz.View.Utils;
using ArtWiz.ViewModel.Base;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ArtWiz.View.Base
{
    public abstract class BasePageViewer : UserControl, IPageViewer, IArtWizViewModelOwner
    {
        private IWindowViewer _ownerWindow;
        public IWindowViewer OwnerWindow => _ownerWindow;
        public Dispatcher ViewElementDispatcher => Dispatcher;
        public Dispatcher ViewDispatcher => Dispatcher;
        public abstract object ViewModel { get; }
        public abstract string PageName { get; }

        public virtual object? CustomHeaderView => null;

        public virtual RowDefinition? HeaderRow => null;

        public BasePageViewer()
        {

        }

        public BasePageViewer(IWindowViewer ownerWindow)
        {
            _ownerWindow = ownerWindow;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DataContext.IfIs<IArtWizViewModel>((it) => it.OnArtWizViewModelOwnerDestroy());
            Unloaded -= OnUnloaded;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            DataContext.IfIs<IArtWizViewModel>((it) => it.OnArtWizViewModelOwnerCreate(this));
        }

        public virtual bool ProcessHitTest(Window owner, Point mousePositionFromScreen)
        {
            return false;
        }

        public virtual bool ProcessMenuItem(PreProcessMenuItemInfo menuItem)
        {
            return false;
        }

        public virtual Menu? GetExtraMenuForPage()
        {
            return null;
        }
    }
}
