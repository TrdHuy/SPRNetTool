using ArtWiz.Utils;
using ArtWiz.ViewModel.Base;
using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ArtWiz.View.Base
{
    public abstract class BaseArtWizWindow : CyberWindow, IWindowViewer, IArtWizViewModelOwner
    {

        public Dispatcher ViewElementDispatcher => Dispatcher;

        public object ViewModel => DataContext;

        public Dispatcher ViewDispatcher => Dispatcher;

        private IWindowViewer.WindowClosedHandler? onWindowClosed;


        public virtual void DisableWindow(bool isDisabled)
        {
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            onWindowClosed?.Invoke(this);
            ViewModel.IfIs<IArtWizViewModel>((it) => it.OnArtWizViewModelDestroy());
        }

        public void AddOnWindowClosedEvent(IWindowViewer.WindowClosedHandler onWindowClosed)
        {
            this.onWindowClosed += onWindowClosed;
        }

        public void RemoveOnWindowClosedEvent(IWindowViewer.WindowClosedHandler onWindowClosed)
        {
            this.onWindowClosed -= onWindowClosed;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            ViewModel.IfIs<IArtWizViewModel>((it) => it.OnArtWizViewModelOwnerCreate(this));
        }      
    }
}
