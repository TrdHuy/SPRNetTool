using ArtWiz.View.Pages.PakEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ArtWiz.View.Base
{
    public abstract class BasePageViewerChild : UserControl, IPageViewerChild
    {
        public static readonly DependencyProperty OwnerPageProperty =
        DependencyProperty.Register(
            nameof(OwnerPage),
            typeof(IPageViewer),
            typeof(BasePageViewerChild),
            new PropertyMetadata(default(IPageViewer), OnOwnerPageChanged));

        private static void OnOwnerPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public IPageViewer OwnerPage
        {
            get => (IPageViewer)GetValue(OwnerPageProperty);
            set => SetValue(OwnerPageProperty, value);
        }


        public Dispatcher ViewElementDispatcher => Dispatcher;

        public object ViewModel => DataContext;

    }
}
