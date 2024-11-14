using ArtWiz.Utils;
using ArtWiz.View.Base;
using ArtWiz.View.Pages;
using ArtWiz.View.Widgets;
using System;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;

namespace ArtWiz.View
{
    public partial class MainWindow : BaseArtWizWindow
    {
        private DebugPage? debugPage = null;
        private SprEditorPage? sprEditorPage = null;
        private PakEditorPage? pakEditorPage = null;
        private double previousePageContentScrollViewHeightCache = -1d;

        private MenuItem? _sprWorkSpaceItem;
        private MenuItem? _devModeMenuItem;
        private MenuItem? _pakWorkSpaceItem;

        public MainWindow()
        {
            InitializeComponent();
            SetPageContent(sprEditorPage ?? new SprEditorPage((IWindowViewer)this).Also((it) => sprEditorPage = it));
        }

        private void SetPageContent(object content)
        {
            PageContentPresenter.Content = content;
            var chrome = WindowChrome.GetWindowChrome(this);
            if (chrome != null)
            {
                if (content is SprEditorPage)
                {
                    chrome.ResizeBorderThickness = new Thickness(0);
                }
                else
                {
                    chrome.ResizeBorderThickness = new Thickness(10);
                }
            }

            UpdatePageNameOnWindowBar(PageContentPresenter.Content);
        }


        public override void DisableWindow(bool isDisabled)
        {
            if (isDisabled)
            {
                DisableLayer.Visibility = Visibility.Visible;
            }
            else
            {
                DisableLayer.Visibility = Visibility.Collapsed;
            }
        }



        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _sprWorkSpaceItem = (_windowTitleBar as WindowTitleBar)?.SprWorkSpceMenu ?? throw new Exception();
            _devModeMenuItem = (_windowTitleBar as WindowTitleBar)?.DeveloperModeMenu ?? throw new Exception();
            _pakWorkSpaceItem = (_windowTitleBar as WindowTitleBar)?.PakWorkSpaceMenu ?? throw new Exception();
            _devModeMenuItem.Click += MenuItemClick;
            _sprWorkSpaceItem.Click += MenuItemClick;
            _pakWorkSpaceItem.Click += MenuItemClick;
            UpdatePageNameOnWindowBar(PageContentPresenter.Content);
        }

        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            if (sender == _devModeMenuItem)
            {
                SetPageContent(debugPage ?? new DebugPage((IWindowViewer)this).Also((it) => debugPage = it));
                _windowTitleBar?.IfIs<WindowTitleBar>(it =>
                {
                    it.CustomHeaderView = null;
                });
            }
            else if (sender == _sprWorkSpaceItem)
            {
                SetPageContent(sprEditorPage ?? new SprEditorPage((IWindowViewer)this).Also((it) => sprEditorPage = it));
                _windowTitleBar?.IfIs<WindowTitleBar>(it =>
                {
                    it.CustomHeaderView = null;
                });
            }
            else if (sender == _pakWorkSpaceItem)
            {
                SetPageContent(pakEditorPage ?? new PakEditorPage((IWindowViewer)this).Also((it) => pakEditorPage = it));

                _windowTitleBar?.IfIs<WindowTitleBar>(it =>
                {
                    //it.CustomHeaderView = pakEditorPage!.PageHeader.CustomHeaderView;
                });
            }
        }

        private void OnWindowStateChanged(object sender, System.EventArgs e)
        {
            if (_windowSizeManager.OldState == WindowState.Normal &&
                WindowState == WindowState.Maximized &&
                PageContentPresenter.Content == sprEditorPage &&
                PageContentScrollViewer.VerticalScrollBarVisibility != ScrollBarVisibility.Visible)
            {
                previousePageContentScrollViewHeightCache = PageContentScrollViewer.ActualHeight;
            }
            else if (_windowSizeManager.OldState == WindowState.Maximized &&
                WindowState == WindowState.Normal &&
                previousePageContentScrollViewHeightCache != -1d &&
                sprEditorPage != null &&
                previousePageContentScrollViewHeightCache >= sprEditorPage.MinHeight)
            {
                PageContentScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                PageContentScrollViewer.UpdateLayout();
                PageContentScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
        }

        private void UpdatePageNameOnWindowBar(object pageContent)
        {
            _windowTitleBar?.IfIs<WindowTitleBar>(it1
              => pageContent?.IfIs<BasePageViewer>(it2
                  => it1.PageTitleTextBlock.Text = it2.PageName));
        }
    }


}


