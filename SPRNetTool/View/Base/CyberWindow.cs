using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using WinInterop = System.Windows.Interop;

namespace ArtWiz.View.Base
{
    public interface IWindowTitleBar
    {
        public delegate void TitleBarHeightChangedHandler(object? sender, double oldValue, double newValue);

        public Button MinimizeButton { get; }
        public Button SmallmizeButton { get; }
        public Button CloseButton { get; }

        public Button MaximizeButton { get; }
        public double TitleBarHeight { get; }

        event TitleBarHeightChangedHandler? TitleBarHeightChanged;
    }
    public abstract class CyberWindow : Window
    {
        private static Style? DefaultArtWizWindowStyle;
        static CyberWindow()
        {
            var app = Application.Current;
            if (app != null)
            {
                DefaultArtWizWindowStyle = app.TryFindResource(Definitions.ArtWizDefaultWindowStyle) as Style;
            }
        }
        protected class WindowSizeManager
        {
            /// <summary>
            /// Message detail:
            /// https://docs.microsoft.com/en-us/windows/win32/winmsg/wm-getminmaxinfo
            /// </summary>
            private const int WM_GETMINMAXINFO = 0x0024;

            /// <summary>
            /// Nonclient area double left click event
            /// Message detail:
            /// https://docs.microsoft.com/vi-vn/windows/win32/inputdev/wm-nclbuttondown
            /// </summary>
            private const int WM_NCLBUTTONDBLCLK = 0x00A3;

            /// <summary>
            /// Sent one time to a window, after it has exited the moving or sizing modal loop.
            /// Message detail:
            /// https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-exitsizemove
            /// </summary>
            private const int WM_EXITSIZEMOVE = 0x0232;

            /// <summary>
            /// Sent to a window whose size, position, or place in the Z order has 
            /// changed as a result of a call to the SetWindowPos function or 
            /// another window-management function.
            /// Message detail:
            /// https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-windowposchanged
            /// </summary>
            private const int WM_WINDOWPOSCHANGED = 0x0047;

            /// <summary>
            /// Sent to a window whose size, position, or place in the Z order is about
            /// to change as a result of a call to the SetWindowPos function or another 
            /// window-management function.
            /// Message detail:
            /// https://docs.microsoft.com/en-us/windows/win32/winmsg/wm-windowposchanging
            /// </summary>
            private const int WM_WINDOWPOSCHANGING = 0x0046;

            /// <summary>
            /// A message that is sent to all top-level windows when the 
            /// SystemParametersInfo function changes a system-wide setting or when policy
            /// settings have changed.
            /// Message detail:
            /// https://docs.microsoft.com/en-us/windows/win32/winmsg/wm-settingchange
            /// </summary>
            private const int WM_SETTINGCHANGE = 0x001A;

            /// <summary>
            /// Sent to a window after its size has changed.
            /// Message detail:
            /// https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-size
            /// </summary>
            private const int WM_SIZE = 0x0005;

            private const int WM_GETDPISCALEDSIZE = 0x02E4;
            private const int WM_DPICHANGED = 0x02E0;

            /// <summary>
            /// System parameter info
            /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa
            /// </summary>
            private const int SPI_SETWORKAREA = 0x002F;

            private const int SHADOW_DEF = 5;

            private CyberWindow _cyberWindow;
            private MINMAXINFO _cyberMinMaxInfo;
            private RECT _previousCyberWorkArea;
            private RECT _currentCyberWorkArea;
            private WindowState _newState;
            private bool _isInitialized = false;
            private bool _isCyberWindowDockCache = false;

            public WindowState NewState
            {
                get
                {
                    return _newState;
                }
                private set
                {
                    _newState = value;
                    if (_newState == WindowState.Maximized)
                    {
                        _cyberWindow.UpdateWindowShadowEffect(0);
                        SetWindowFullScreen();
                    }
                    else if (_newState == WindowState.Normal)
                    {
                        var shadowDef = IsCyberWindowDockCache ? 0 : SHADOW_DEF;
                        _cyberWindow.UpdateWindowShadowEffect(shadowDef);
                        SetWindowBackToLastNormalSize();
                    }
                    else
                    {
                        _cyberWindow.UpdateWindowShadowEffect(SHADOW_DEF);
                    }
                }
            }
            public WindowState OldState { get; private set; }
            public double InitialWidth { get; private set; }
            public double InitialHeight { get; private set; }
            public double LastNormalWidthCache { get; private set; }
            public double LastNormalHeightCache { get; private set; }

            private bool IsCyberWindowDockCache
            {
                get
                {
                    return _isCyberWindowDockCache;
                }
                set
                {
                    if (value != _isCyberWindowDockCache)
                    {
                        _isCyberWindowDockCache = value;
                        if (_cyberWindow.WindowState == WindowState.Maximized)
                        {
                            _cyberWindow.UpdateWindowShadowEffect(0);
                        }
                        else
                        {
                            _cyberWindow.UpdateWindowShadowEffect(_isCyberWindowDockCache ? 0 : SHADOW_DEF);
                        }
                    }
                }
            }

            public WindowSizeManager(CyberWindow window)
            {
                _cyberWindow = window;
            }

            public void NotifyCyberWindowStateChange()
            {
                OldState = NewState;
                NewState = _cyberWindow.WindowState;
            }

            public void InitWindowSizeManager(double initWidth, double initHeight)
            {
                InitialWidth = initWidth;
                InitialHeight = initHeight;
                LastNormalWidthCache = initWidth;
                LastNormalHeightCache = initHeight;
                _isInitialized = true;
                OldState = _cyberWindow.WindowState;
                NewState = _cyberWindow.WindowState;
            }

            public IntPtr WindowProc(
                 IntPtr hwnd,
                 int msg,
                 IntPtr wParam,
                 IntPtr lParam,
                 ref bool handled)
            {
                switch (msg)
                {
                    case WM_DPICHANGED:
                        {
                            //TODO: Implement DPI change
                            break;
                        }
                    case WM_GETDPISCALEDSIZE:
                        {
                            //TODO: Implement DPI change
                            break;
                        }
                    case WM_EXITSIZEMOVE:
                        {
                            RECT cyberRect = new RECT();
                            NativeMethods.GetWindowRect(hwnd, ref cyberRect);

                            int MONITOR_DEFAULTTONEAREST = 0x00000002;
                            IntPtr monitor = NativeMethods.MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                            if (monitor != IntPtr.Zero)
                            {
                                MONITORINFO monitorInfo = new MONITORINFO();
                                NativeMethods.GetMonitorInfo(monitor, monitorInfo);
                                RECT rcWorkArea = monitorInfo.rcWork;
                                RECT rcMonitorArea = monitorInfo.rcMonitor;
                                SetWorkAreaInfo(rcWorkArea);
                                SetWindowDockInfo(cyberRect);
                            }
                            break;
                        }
                    case WM_SETTINGCHANGE:
                        {
                            if (wParam.ToInt32() == SPI_SETWORKAREA)
                            {
                                if (_cyberWindow.WindowState == WindowState.Maximized)
                                {
                                    var isShouldUpdateWindowHeight = _previousCyberWorkArea.bottom < _currentCyberWorkArea.bottom;
                                    if (isShouldUpdateWindowHeight)
                                    {

                                        // Chi tiết chế độ hiển thị:
                                        // https://source.dot.net/#PresentationFramework/System/Windows/Standard/NativeMethods.cs
                                        int SHOWNORMAL = 1;
                                        int SHOWMAXIMIZED = 3;

                                        // Hiển thị window ở trạng thái normal sau đó là maximized
                                        // để cập nhật chiều cao cửa sổ
                                        NativeMethods.ShowWindow(hwnd, SHOWNORMAL);
                                        NativeMethods.ShowWindow(hwnd, SHOWMAXIMIZED);
                                    }

                                }
                                handled = true;
                            }
                            break;
                        }
                    case WM_GETMINMAXINFO:
                        {
                            MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                            // Adjust the maximized size and position to fit the work area of the correct monitor
                            int MONITOR_DEFAULTTONEAREST = 0x00000002;
                            IntPtr monitor = NativeMethods.MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                            if (monitor != System.IntPtr.Zero)
                            {
                                MONITORINFO monitorInfo = new MONITORINFO();
                                NativeMethods.GetMonitorInfo(monitor, monitorInfo);
                                RECT rcWorkArea = monitorInfo.rcWork;
                                RECT rcMonitorArea = monitorInfo.rcMonitor;
                                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
                                SetMinMaxInfo(mmi);
                                SetWorkAreaInfo(rcWorkArea);
                            }
                            Marshal.StructureToPtr(mmi, lParam, true);
                            break;
                        }
                }
                return (IntPtr)0;
            }

            private void SetWindowFullScreen()
            {
                if (_cyberMinMaxInfo.ptMaxSize.x > 0 && _cyberMinMaxInfo.ptMaxSize.y > 0)
                {
                    LastNormalWidthCache = _cyberWindow.ActualWidth;
                    LastNormalHeightCache = _cyberWindow.ActualHeight;
                    _cyberWindow.Height = GetHeightByPixel(_cyberMinMaxInfo.ptMaxSize.y);
                    _cyberWindow.Width = GetWidthByPixel(_cyberMinMaxInfo.ptMaxSize.x);
                }
            }

            private void SetWindowBackToLastNormalSize()
            {
                _cyberWindow.Height = LastNormalHeightCache;
                _cyberWindow.Width = LastNormalWidthCache;
            }

            private void SetMinMaxInfo(MINMAXINFO mmi)
            {
                if (_cyberMinMaxInfo.ptMaxPosition.x != mmi.ptMaxPosition.x
                    || _cyberMinMaxInfo.ptMaxPosition.y != mmi.ptMaxPosition.y
                    || _cyberMinMaxInfo.ptMaxSize.x != mmi.ptMaxSize.x
                    || _cyberMinMaxInfo.ptMaxSize.y != mmi.ptMaxSize.y)
                {
                    _cyberMinMaxInfo.ptMaxPosition.x = mmi.ptMaxPosition.x;
                    _cyberMinMaxInfo.ptMaxPosition.y = mmi.ptMaxPosition.y;
                    _cyberMinMaxInfo.ptMaxSize.x = mmi.ptMaxSize.x;
                    _cyberMinMaxInfo.ptMaxSize.y = mmi.ptMaxSize.y;
                }
            }

            private void SetWorkAreaInfo(RECT rcWorkArea)
            {
                if (_currentCyberWorkArea != rcWorkArea)
                {
                    _previousCyberWorkArea.left = _currentCyberWorkArea.left;
                    _previousCyberWorkArea.top = _currentCyberWorkArea.top;
                    _previousCyberWorkArea.right = _currentCyberWorkArea.right;
                    _previousCyberWorkArea.bottom = _currentCyberWorkArea.bottom;

                    _currentCyberWorkArea.left = rcWorkArea.left;
                    _currentCyberWorkArea.top = rcWorkArea.top;
                    _currentCyberWorkArea.right = rcWorkArea.right;
                    _currentCyberWorkArea.bottom = rcWorkArea.bottom;
                }
            }

            private void SetWindowDockInfo(RECT newRect)
            {
                if (newRect.IsEmpty) return;

                double dockModeWidth = _currentCyberWorkArea.Width / 2;
                var dpi = NativeMethods.GetDeviceCaps();
                double cyberMinWidthInPixel = GetSizeInPixel(_cyberWindow.MinWidth, dpi.X);
                double cyberMaxWidthInPixel = GetSizeInPixel(_cyberWindow.MaxWidth, dpi.X);
                double cyberMinHeightInPixel = GetSizeInPixel(_cyberWindow.MinHeight, dpi.Y);
                double cyberMaxHeightInPixel = GetSizeInPixel(_cyberWindow.MaxHeight, dpi.Y);

                if (cyberMinWidthInPixel > _currentCyberWorkArea.Width / 2)
                {
                    dockModeWidth = cyberMinWidthInPixel;
                }
                else if (cyberMaxWidthInPixel < _currentCyberWorkArea.Width / 2)
                {
                    dockModeWidth = cyberMaxWidthInPixel;
                }

                double dockModeHeight = _currentCyberWorkArea.Height;
                if (cyberMinHeightInPixel > _currentCyberWorkArea.Height)
                {
                    dockModeHeight = cyberMinHeightInPixel;
                }
                else if (cyberMaxHeightInPixel < _currentCyberWorkArea.Height)
                {
                    dockModeHeight = cyberMaxHeightInPixel;
                }

                var isLeftDocked = newRect.left == _currentCyberWorkArea.left
                    && newRect.top == _currentCyberWorkArea.top
                    && newRect.Height == dockModeHeight
                    && newRect.Width == dockModeWidth;


                var isRightDocked = newRect.right == _currentCyberWorkArea.right
                    && newRect.top == _currentCyberWorkArea.top
                    && newRect.Height == dockModeHeight
                    && newRect.Width == dockModeWidth;

                IsCyberWindowDockCache = isLeftDocked || isRightDocked;
            }

            private double GetSizeInPixel(double size, double dpi)
            {
                return size * dpi / 96;
            }

            private double GetWidthByPixel(double pixel)
            {
                var dpi = NativeMethods.GetDeviceCaps();
                return pixel * 96 / dpi.X;
            }

            private double GetHeightByPixel(double pixel)
            {
                var dpi = NativeMethods.GetDeviceCaps();
                return pixel * 96 / dpi.Y;
            }
        }

        private void UpdateWindowShadowEffect(int shadowDef)
        {
            if (_botShadowRowDefinition != null)
            {
                _botShadowRowDefinition.Height = new GridLength(shadowDef);
            }

            if (_leftShadowColumnDefinition != null)
            {
                _leftShadowColumnDefinition.Width = new GridLength(shadowDef);
            }

            if (_rightShadowColumnDefinition != null)
            {
                _rightShadowColumnDefinition.Width = new GridLength(shadowDef);
            }
        }

        private const string MinimizeButtonName = "MinimizeButton";
        private const string SmallmizeButtonName = "SmallmizeButton";
        private const string CloseButtonName = "CloseButton";
        private const string MaximizeButtonName = "MaximizeButton";
        protected const string WindowControlPanelName = "WindowControlPanel";
        private const string BotShadowRowDefName = "BotShadowRowDef";
        private const string RightShadowColDefName = "RightShadowColumnDef";
        private const string LeftShadowColDefName = "LeftShadowColumnDef";

        protected Button? _minimizeBtn;
        protected Button? _maximizeBtn;
        protected Button? _closeBtn;
        protected Button? _smallmizeBtn;
        private StackPanel? _windowControlPanel;
        private RowDefinition? _botShadowRowDefinition;
        private ColumnDefinition? _leftShadowColumnDefinition;
        private ColumnDefinition? _rightShadowColumnDefinition;
        protected IWindowTitleBar? _windowTitleBar;

        protected WindowSizeManager _windowSizeManager;


        public CyberWindow()
        {
            _windowSizeManager = new WindowSizeManager(this);

            DefaultStyleKey = typeof(BaseArtWizWindow);

            SourceInitialized += new EventHandler((s, e) =>
            {
                System.IntPtr handle = (new WinInterop.WindowInteropHelper(this)).Handle;
                WinInterop.HwndSource.FromHwnd(handle).AddHook(new WinInterop.HwndSourceHook(_windowSizeManager.WindowProc));
            });
            WindowStyle = WindowStyle.None;
            Style = DefaultArtWizWindowStyle;

            Initialized += OnCyberWindowInitialized;
        }

        private void OnCyberWindowInitialized(object? sender, EventArgs e)
        {
            _windowSizeManager.InitWindowSizeManager(Width, Height);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _windowTitleBar = GetTemplateChild("WindowTitleBar") as IWindowTitleBar ?? throw new ArgumentNullException();
            _minimizeBtn = _windowTitleBar.MinimizeButton;
            _maximizeBtn = _windowTitleBar.MaximizeButton;
            _closeBtn = _windowTitleBar.CloseButton;
            _smallmizeBtn = _windowTitleBar.SmallmizeButton;
            _botShadowRowDefinition = GetTemplateChild(BotShadowRowDefName) as RowDefinition ?? throw new ArgumentNullException();
            _leftShadowColumnDefinition = GetTemplateChild(LeftShadowColDefName) as ColumnDefinition ?? throw new ArgumentNullException();
            _rightShadowColumnDefinition = GetTemplateChild(RightShadowColDefName) as ColumnDefinition ?? throw new ArgumentNullException();

            _maximizeBtn.Click += (s, e) =>
            {
                this.WindowState = WindowState.Maximized;
            };

            _smallmizeBtn.Click += (s, e) =>
            {
                this.WindowState = WindowState.Normal;
            };

            _closeBtn.Click += (s, e) =>
            {
                this.Close();
            };

            _minimizeBtn.Click += (s, e) =>
            {
                this.WindowState = WindowState.Minimized;
            };

            _windowTitleBar.TitleBarHeightChanged += (s, o, n) =>
            {
                UpdateCaptionHeight(this, n);
            };

            UpdateCaptionHeight(this, _windowTitleBar.TitleBarHeight);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            _windowSizeManager.NotifyCyberWindowStateChange();
            base.OnStateChanged(e);
        }

        private static void UpdateCaptionHeight(Window window, double newHeight)
        {
            // Lấy WindowChrome hiện tại
            var windowChrome = WindowChrome.GetWindowChrome(window);
            if (windowChrome != null)
            {
                // Cập nhật CaptionHeight
                windowChrome.CaptionHeight = newHeight;

                // Nếu cần, áp dụng lại
                WindowChrome.SetWindowChrome(window, windowChrome);
            }
            else
            {
                // Nếu WindowChrome chưa được thiết lập, tạo mới và gán
                windowChrome = new WindowChrome
                {
                    CaptionHeight = newHeight
                };
                WindowChrome.SetWindowChrome(window, windowChrome);
            }
        }
    }

    public static class NativeMethods
    {

        /// <summary>
        /// Logical pixels inch in X
        /// </summary>
        public const int LOGPIXELSX = 88;
        /// <summary>
        /// Logical pixels inch in Y
        /// </summary>
        public const int LOGPIXELSY = 90;

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateDC(string lpszDriver, string lpszDeviceName, string lpszOutput, IntPtr devMode);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int GetDeviceCaps(IntPtr hDC, int nIndex);

        [DllImport("user32")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("User32")]
        public static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        [DllImport("User32")]
        public static extern bool ClientToScreen(IntPtr hWnd, out POINT flags);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref RECT rectangle);

        [DllImport("user32")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("Kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx
        (
            string lpszPath,                    // Must name a folder, must end with '\'.
            ref long lpFreeBytesAvailable,
            ref long lpTotalNumberOfBytes,
            ref long lpTotalNumberOfFreeBytes
        );

        public static Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            // NOTE: If you need error handling
            // bool success = GetCursorPos(out lpPoint);
            // if (!success)

            return lpPoint;
        }

        public static Point GetDeviceCaps()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);

            var dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
            var dpiY = GetDeviceCaps(hdc, LOGPIXELSY);
            return new Point(dpiX, dpiX);
        }

        public static Point GetDeviceCaps(IntPtr dv)
        {
            var dpiX = GetDeviceCaps(dv, LOGPIXELSX);
            var dpiY = GetDeviceCaps(dv, LOGPIXELSY);
            return new Point(dpiX, dpiX);
        }

        /// <summary>
        /// Returns free space for drive containing the specified folder, or returns -1 on failure.
        /// </summary>
        /// <param name="folderName">A folder on the drive in question.</param>
        /// <returns>Space free on the volume containing 'folder_name' or -1 on error.</returns>
        public static long GetFreeSpace(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
            {
                throw new ArgumentNullException("folderName");
            }

            if (!Directory.Exists(folderName))
            {
                throw new ArgumentNullException(folderName + " not exist!");
            }

            if (!folderName.EndsWith("\\"))
            {
                folderName += '\\';
            }

            long free = 0, dummy1 = 0, dummy2 = 0;

            if (GetDiskFreeSpaceEx(folderName, ref free, ref dummy1, ref dummy2))
            {
                return free;
            }
            else
            {
                return -1;
            }
        }
    }

    public enum DeviceCap
    {
        HORZRES = 8,
        VERTRES = 10,
        DESKTOPVERTRES = 117,
        LOGPIXELSY = 90,
        DESKTOPHORZRES = 118
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        /// <summary>
        /// x coordinate of point.
        /// </summary>
        public int x;
        /// <summary>
        /// y coordinate of point.
        /// </summary>
        public int y;

        /// <summary>
        /// Construct a point of coordinates (x,y).
        /// </summary>
        public POINT(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator Point(POINT point)
        {
            return new Point(point.x, point.y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int flags;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MONITORINFO
    {
        /// <summary>
        /// </summary>            
        public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));

        /// <summary>
        /// </summary>            
        public RECT rcMonitor = new RECT();

        /// <summary>
        /// </summary>            
        public RECT rcWork = new RECT();

        /// <summary>
        /// </summary>            
        public int dwFlags = 0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RECT
    {
        /// <summary> Win32 </summary>
        public int left;
        /// <summary> Win32 </summary>
        public int top;
        /// <summary> Win32 </summary>
        public int right;
        /// <summary> Win32 </summary>
        public int bottom;

        /// <summary> Win32 </summary>
        public static readonly RECT Empty = new RECT();

        /// <summary> Win32 </summary>
        public int Width
        {
            get { return Math.Abs(right - left); }  // Abs needed for BIDI OS
        }
        /// <summary> Win32 </summary>
        public int Height
        {
            get { return bottom - top; }
        }

        /// <summary> Win32 </summary>
        public RECT(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }


        /// <summary> Win32 </summary>
        public RECT(RECT rcSrc)
        {
            this.left = rcSrc.left;
            this.top = rcSrc.top;
            this.right = rcSrc.right;
            this.bottom = rcSrc.bottom;
        }

        /// <summary> Win32 </summary>
        public bool IsEmpty
        {
            get
            {
                // BUGBUG : On Bidi OS (hebrew arabic) left > right
                return left >= right || top >= bottom;
            }
        }
        /// <summary> Return a user friendly representation of this struct </summary>
        public override string ToString()
        {
            if (this == RECT.Empty) { return "RECT {Empty}"; }
            return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
        }

        /// <summary> Determine if 2 RECT are equal (deep compare) </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Rect)) { return false; }
            return (this == (RECT)obj);
        }

        /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
        public override int GetHashCode()
        {
            return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
        }


        /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
        public static bool operator ==(RECT rect1, RECT rect2)
        {
            return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
        }

        /// <summary> Determine if 2 RECT are different(deep compare)</summary>
        public static bool operator !=(RECT rect1, RECT rect2)
        {
            return !(rect1 == rect2);
        }


    }
}
