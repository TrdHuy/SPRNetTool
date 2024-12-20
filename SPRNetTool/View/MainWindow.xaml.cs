using ArtWiz.Utils;
using ArtWiz.View.Base;
using ArtWiz.View.Pages;
using ArtWiz.View.Utils;
using ArtWiz.View.Widgets;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using System.Xml.Linq;

namespace ArtWiz.View
{
    public partial class MainWindow : BaseArtWizWindow
    {
        private double previousePageContentScrollViewHeightCache = -1d;

        private Menu? _mainMenu;
        private MenuItem? _sprWorkSpaceItem;
        private MenuItem? _devModeMenuItem;
        private MenuItem? _pakWorkSpaceItem;
        private Dictionary<MenuItem, PreProcessMenuItemInfo> _defaultMenuItemInfo = new Dictionary<MenuItem, PreProcessMenuItemInfo>();
        private List<MenuItem> _cacheExtraMenuItem = new List<MenuItem>();
        private Rect _titleBarCustomAreaRect = Rect.Empty;
        public MainWindow()
        {
            InitializeComponent();
            SetPageContent(new SprEditorPage((IWindowViewer)this));
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

            // Xử lý default menu
            _mainMenu?.Apply(it => TraverseMenuItems(it));

            // Xử lý extra menu
            content.IfIs<IPageViewer>((page) =>
            {
                _windowTitleBar?.IfIs<WindowTitleBar>(it =>
                {
                    if (_cacheExtraMenuItem.Count > 0)
                    {
                        foreach (var item in _cacheExtraMenuItem)
                        {
                            it.MainMenu.Items.Remove(item);
                        }
                    }
                });

                _cacheExtraMenuItem.Clear();
                var extraMenu = page.GetExtraMenuForPage();
                if (_mainMenu != null && extraMenu != null)
                {
                    if (extraMenu.Parent is Panel oldParent)
                    {
                        oldParent.Children.Remove(extraMenu);
                    }
                    else if (extraMenu.Parent is Menu menu)
                    {
                        menu.Items.Remove(extraMenu);
                    }
                    _windowTitleBar?.IfIs<WindowTitleBar>(it =>
                    {
                        var itemsCount = extraMenu.Items.Count;
                        for (int i = itemsCount - 1; i >= 0; i--)
                        {
                            var item = extraMenu.Items[i] as MenuItem;
                            if (item is MenuItem)
                            {
                                _cacheExtraMenuItem.Add(item);
                                extraMenu.Items.RemoveAt(i);
                                it.MainMenu.Items.Add(item);
                            }
                            else
                            {
                                throw new Exception("Should never happen, must be a menu item in extra menu");
                            }
                        }
                    });
                }
            });

            // Xử lý custom title window header view
            _windowTitleBar?.IfIs<WindowTitleBar>(it =>
            {
                content.IfIs<IPageViewer>(page =>
                {
                    if (page.CustomHeaderView is FrameworkElement fe)
                    {
                        if (fe.Parent is Panel oldParent)
                        {
                            oldParent.Children.Remove(fe);
                        }
                        it.CustomHeaderView = page.CustomHeaderView;

                        // Xử lý client area trên title bar
                        fe.Loaded += (s, e) =>
                        {
                            _titleBarCustomAreaRect = fe.TransformToAncestor(this).TransformBounds(new Rect(fe.RenderSize));
                        };
                    }
                    else
                    {
                        it.CustomHeaderView = null;
                        _titleBarCustomAreaRect = Rect.Empty;
                    }
                });
            });

            content.IfIs<IPageViewer>((page) =>
            {
                page.HeaderRow?.Apply(it => it.Height = new GridLength(0));
            });
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
            _mainMenu = (_windowTitleBar as WindowTitleBar)?.MainMenu ?? throw new Exception();
            _sprWorkSpaceItem = (_windowTitleBar as WindowTitleBar)?.SprWorkSpceMenu ?? throw new Exception();
            _devModeMenuItem = (_windowTitleBar as WindowTitleBar)?.DeveloperModeMenu ?? throw new Exception();
            _pakWorkSpaceItem = (_windowTitleBar as WindowTitleBar)?.PakWorkSpaceMenu ?? throw new Exception();

            _devModeMenuItem.Click += MenuItemClick;
            _sprWorkSpaceItem.Click += MenuItemClick;
            _pakWorkSpaceItem.Click += MenuItemClick;
            UpdatePageNameOnWindowBar(PageContentPresenter.Content);

            _defaultMenuItemInfo.Clear();
            TraverseMenuItems(_mainMenu, true);
        }

        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            if (sender == _devModeMenuItem)
            {
                SetPageContent(new DebugPage((IWindowViewer)this));
            }
            else if (sender == _sprWorkSpaceItem)
            {
                SetPageContent(new SprEditorPage((IWindowViewer)this));
            }
            else if (sender == _pakWorkSpaceItem)
            {
                SetPageContent(new PakEditorPage((IWindowViewer)this));
            }
        }

        private void OnWindowStateChanged(object sender, System.EventArgs e)
        {
            if (_windowSizeManager.OldState == WindowState.Normal &&
                WindowState == WindowState.Maximized &&
                PageContentPresenter.Content is SprEditorPage &&
                PageContentScrollViewer.VerticalScrollBarVisibility != ScrollBarVisibility.Visible)
            {
                previousePageContentScrollViewHeightCache = PageContentScrollViewer.ActualHeight;
            }
            else if (
                PageContentPresenter.Content is SprEditorPage sprPage &&
                _windowSizeManager.OldState == WindowState.Maximized &&
                WindowState == WindowState.Normal &&
                previousePageContentScrollViewHeightCache != -1d &&
                previousePageContentScrollViewHeightCache >= sprPage.MinHeight)
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

        protected override bool ProcessHitTest(Point mousePositionFromScreen)
        {
            // Xử lý hit test trên title bar
            if (_titleBarCustomAreaRect != Rect.Empty && _titleBarCustomAreaRect.Contains(mousePositionFromScreen))
            {
                return true;
            }
            return base.ProcessHitTest(mousePositionFromScreen);
        }

        #region Menu Item Processing
        private void TraverseMenuItems(Menu menu, bool isInit = false)
        {
            foreach (var item in menu.Items)
            {
                if (item is MenuItem menuItem)
                {
                    ProcessMenuItem(menuItem, isInit);

                    // Duyệt đệ quy các MenuItem con
                    if (menuItem.Items.Count > 0)
                    {
                        TraverseMenuItems(menuItem, isInit);
                    }
                }
            }
        }

        private void TraverseMenuItems(MenuItem menuItem, bool isInit)
        {
            foreach (var item in menuItem.Items)
            {
                if (item is MenuItem childMenuItem)
                {
                    ProcessMenuItem(childMenuItem, isInit);

                    // Tiếp tục duyệt đệ quy
                    if (childMenuItem.Items.Count > 0)
                    {
                        TraverseMenuItems(childMenuItem, isInit);
                    }
                }
            }
        }

        private void ProcessMenuItem(MenuItem menuItem, bool isInit)
        {
            if (isInit)
            {
                _defaultMenuItemInfo[menuItem] = PreProcessMenuItemInfo.MakeFromMenuItem(menuItem) ??
                    new PreProcessMenuItemInfo() { Visibility = Visibility.Visible };
            }

            // Chỉ xử lý các default item, các extra item không xử lý
            if (_defaultMenuItemInfo.ContainsKey(menuItem))
            {
                PageContentPresenter.Content.IfIs<IPageViewer>(it =>
                {
                    var preProcessInfo = PreProcessMenuItemInfo.MakeFromMenuItem(menuItem);
                    if (preProcessInfo != null && it.ProcessMenuItem(preProcessInfo))
                    {
                        menuItem.Visibility = preProcessInfo.Visibility;
                    }
                    else
                    {
                        menuItem.Visibility = _defaultMenuItemInfo[menuItem].Visibility;
                    }
                });
            }
        }
        #endregion
    }


}


