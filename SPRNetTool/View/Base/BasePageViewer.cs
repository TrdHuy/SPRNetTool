using ArtWiz.Utils;
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
        public BasePageViewer(IWindowViewer ownerWindow)
        {
            _ownerWindow = ownerWindow;
            ownerWindow.AddOnWindowClosedEvent(OnWindowClosed);
        }

        private void OnWindowClosed(Window w)
        {
            DataContext.IfIs<IArtWizViewModel>((it) => it.OnDestroy());
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            DataContext.IfIs<IArtWizViewModel>((it) => it.OnArtWizViewModelOwnerCreate(this));
        }

        ~BasePageViewer()
        {
            _ownerWindow.RemoveOnWindowClosedEvent(OnWindowClosed);
        }
    }
}
