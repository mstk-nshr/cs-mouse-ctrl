using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

/// <summary>
/// Window Stack でスタックされたウィンドウの上辺に表示するタブ切り替えバー。
/// 常に最上位 (TopMost) に表示され、各タブをクリックすると該当ウィンドウを前面に表示する。
/// </summary>
sealed class WindowStackTabForm : Form
{
    // -----------------------------------------------------------------------
    // Win32 API
    // -----------------------------------------------------------------------

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width  => Right - Left;
        public int Height => Bottom - Top;

        public bool EqualsRect(RECT other) =>
            Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
    }

    private const int VK_LBUTTON = 0x01;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_TOP     = IntPtr.Zero;

    private const uint SWP_NOSIZE     = 0x0001;
    private const uint SWP_NOMOVE     = 0x0002;
    private const uint SWP_NOZORDER   = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B")]
    private interface IVirtualDesktopManager
    {
        [PreserveSig]
        int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, [MarshalAs(UnmanagedType.Bool)] out bool onCurrentDesktop);
        [PreserveSig]
        int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);
        [PreserveSig]
        int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
    }

    private static readonly Guid CLSID_VirtualDesktopManager = new("AA509086-5CA9-4C25-8F95-589D3C07B48A");
    private static IVirtualDesktopManager? _vdm;
    private static bool _vdmInitTried;

    private const int DWMWA_CLOAKED = 14;
    private const int SW_HIDE       = 0;
    private const int SW_RESTORE    = 9;

    private const int GWL_EXSTYLE      = -20;
    private const int WS_EX_TOPMOST    = 0x00000008;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_LAYERED    = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const byte AC_SRC_OVER  = 0x00;
    private const byte AC_SRC_ALPHA = 0x01;
    private const uint ULW_ALPHA    = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT_API
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE_API
    {
        public int cx;
        public int cy;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindow(
        IntPtr hwnd, IntPtr hdcDst, ref POINT_API pptDst, ref SIZE_API psize,
        IntPtr hdcSrc, ref POINT_API pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);

    // -----------------------------------------------------------------------
    // レイアウト定数
    // -----------------------------------------------------------------------

    /// <summary>タブ切り替えウィンドウの高さ (px)。</summary>
    public static int TabBarHeight => AppConfig.StackTabBarHeight;

    private static int CloseButtonWidth => Math.Clamp(TabBarHeight, 20, 40);
    private const int TabPadX          = 8;

    // -----------------------------------------------------------------------
    // フィールド
    // -----------------------------------------------------------------------

    private readonly List<IntPtr> _windows = new();
    private readonly List<string> _titles  = new();
    private readonly Dictionary<IntPtr, RECT> _lastWindowRects = new();
    private RECT _lastStackRect;
    private bool _hasStackRect;
    private bool _pendingTopAdjust;
    private bool _hiddenByDesktop;
    private int _titleSyncCounter;
    private IntPtr _activeWindow;
    private IntPtr _lastObservedFg;
    private int _hoverTabIndex = -1;
    private bool _hoverClose;
    private bool _isDropTarget;
    private IntPtr _dragTabHwnd = IntPtr.Zero;
    private bool _dragTabOutside;
    private readonly int _initLeft;
    private readonly int _initTop;
    private readonly int _initWidth;

    private readonly Color _selectedColor;
    private readonly Color _backgroundColor;
    private readonly Color _hoverColor;
    private readonly Color _barBackColor;
    private readonly Font _tabFont;
    private readonly Font _activeTabFont;
    private readonly Font _closeFont;
    private readonly System.Windows.Forms.Timer _syncTimer;

    // -----------------------------------------------------------------------
    // コンストラクタ
    // -----------------------------------------------------------------------

    protected override bool ShowWithoutActivation => true;

    public WindowStackTabForm(
        IEnumerable<IntPtr> windows,
        int left, int top, int width,
        IntPtr activeWindow)
    {
        foreach (var w in windows)
        {
            if (w != IntPtr.Zero && !_windows.Contains(w))
            {
                _windows.Add(w);
                _titles.Add(GetWindowTitle(w));
                if (GetWindowRect(w, out RECT wr))
                {
                    _lastWindowRects[w] = wr;
                    if (w == activeWindow || !_hasStackRect)
                    {
                        _lastStackRect = wr;
                        _hasStackRect  = true;
                    }
                }
            }
        }
        _activeWindow = activeWindow;

        _selectedColor   = AppConfig.StackSelectedColor ?? GetAccentColor();
        _backgroundColor = AppConfig.StackBackgroundColor;
        _hoverColor      = AppConfig.StackHoverColor;
        _barBackColor    = AppConfig.StackTabBarBgColor ?? DeriveBarBackColor(_backgroundColor);
        _tabFont         = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        _activeTabFont   = new Font("Segoe UI", 8.5f, FontStyle.Bold, GraphicsUnit.Point);
        _closeFont       = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

        FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode   = AutoScaleMode.None;
        TopMost         = true;
        ShowInTaskbar   = false;
        StartPosition   = FormStartPosition.Manual;
        DoubleBuffered  = true;

        _initLeft  = left;
        _initTop   = top;
        _initWidth = Math.Max(CloseButtonWidth + 40, width);

        Location   = new Point(_initLeft, _initTop);
        ClientSize = new Size(_initWidth, TabBarHeight);

        _syncTimer = new System.Windows.Forms.Timer { Interval = 33 };
        _syncTimer.Tick += SyncTimer_Tick;
        _syncTimer.Start();

        MouseMove           += OnMouseMove;
        MouseLeave          += OnMouseLeave;
        MouseDown           += OnMouseDown;
        MouseUp             += OnMouseUp;
        MouseCaptureChanged += OnMouseCaptureChanged;
    }

    // -----------------------------------------------------------------------
    // 公開操作
    // -----------------------------------------------------------------------

    public bool ContainsWindow(IntPtr hwnd) => _windows.Contains(hwnd);

    /// <summary>指定スクリーン座標がこのタブバーの表示領域内にあるかを返す。</summary>
    public bool ContainsScreenPoint(int x, int y)
    {
        if (IsDisposed || !IsHandleCreated || !Visible || _hiddenByDesktop)
            return false;
        if (GetWindowRect(Handle, out RECT r))
            return x >= r.Left && x < r.Right && y >= r.Top && y < r.Bottom;
        return Bounds.Contains(x, y);
    }

    /// <summary>
    /// 現在アクティブなタブから step 分（+1: 右/次、-1: 左/前）だけ切り替える。
    /// 両端を超えた場合は wrap-around する。
    /// </summary>
    public void SwitchTabByDelta(int step)
    {
        if (IsDisposed || _windows.Count == 0 || step == 0) return;

        int count = _windows.Count;
        int curIdx = _windows.IndexOf(_activeWindow);
        if (curIdx < 0) curIdx = 0;

        int nextIdx = ((curIdx + step) % count + count) % count;
        ActivateWindow(_windows[nextIdx]);
    }

    private int  _wheelAccum;
    private long _wheelLastTick;

    /// <summary>
    /// ホイール回転量を蓄積し、1 ノッチ (120) ごとにタブを切り替える。
    /// 上回転(+) で前のタブ、下回転(-) で次のタブ。
    /// </summary>
    public void HandleWheelDelta(int delta)
    {
        if (IsDisposed || delta == 0) return;

        long now = Environment.TickCount64;
        if (now - _wheelLastTick > 300 || (_wheelAccum != 0 && Math.Sign(_wheelAccum) != Math.Sign(delta)))
            _wheelAccum = 0;
        _wheelLastTick = now;
        _wheelAccum += delta;

        const int WheelDelta = 120;
        if (Math.Abs(_wheelAccum) >= WheelDelta)
        {
            int step = _wheelAccum > 0 ? -1 : 1;
            _wheelAccum = 0;
            SwitchTabByDelta(step);
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_MOUSEWHEEL = 0x020A;
        // 低レベルフックで捕捉できなかったホイール（高精度タッチパッド等）をタブバー自身で処理する
        if (m.Msg == WM_MOUSEWHEEL)
        {
            int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
            HandleWheelDelta(delta);
            m.Result = IntPtr.Zero;
            return;
        }
        base.WndProc(ref m);
    }

    /// <summary>ドラッグ中のウィンドウをドロップ対象としてハイライト表示するか設定する。</summary>
    public void SetDropTargetHighlight(bool highlight)
    {
        if (IsDisposed || _isDropTarget == highlight) return;
        _isDropTarget = highlight;
        Redraw();
    }

    /// <summary>
    /// 未管理ウィンドウをこのスタックに追加し、位置・サイズを合わせてアクティブタブとして前面表示する。
    /// </summary>
    public void AddWindowAndActivate(IntPtr hwnd)
    {
        if (IsDisposed || hwnd == IntPtr.Zero || !IsWindow(hwnd) || IsIconic(hwnd))
            return;

        if (_windows.Contains(hwnd))
        {
            ActivateWindow(hwnd);
            return;
        }

        // 現在のスタック矩形を決定
        int targetLeft, targetTop, targetWidth, targetHeight;
        bool activeZoomed = _activeWindow != IntPtr.Zero && IsWindow(_activeWindow) && IsZoomed(_activeWindow);
        if (activeZoomed)
        {
            Rectangle wa = Screen.FromHandle(_activeWindow).WorkingArea;
            targetLeft   = wa.Left;
            targetTop    = wa.Top + TabBarHeight;
            targetWidth  = wa.Width;
            targetHeight = Math.Max(100, wa.Height - TabBarHeight);
        }
        else if (_hasStackRect && _lastStackRect.Width > 0 && _lastStackRect.Height > 0)
        {
            targetLeft   = _lastStackRect.Left;
            targetTop    = _lastStackRect.Top;
            targetWidth  = _lastStackRect.Width;
            targetHeight = _lastStackRect.Height;
        }
        else if (_activeWindow != IntPtr.Zero && GetWindowRect(_activeWindow, out RECT ar)
                 && ar.Width > 0 && ar.Height > 0)
        {
            targetLeft   = ar.Left;
            targetTop    = ar.Top;
            targetWidth  = ar.Width;
            targetHeight = ar.Height;
        }
        else
        {
            return;
        }

        if (IsZoomed(hwnd))
            ShowWindow(hwnd, SW_RESTORE);

        string stackDevice = _windows.Count > 0
            ? Screen.FromHandle(_windows[0]).DeviceName
            : Screen.FromHandle(Handle).DeviceName;
        bool crossMonitor = !string.Equals(
            Screen.FromHandle(hwnd).DeviceName, stackDevice, StringComparison.OrdinalIgnoreCase);

        SetWindowPos(hwnd, IntPtr.Zero, targetLeft, targetTop, targetWidth, targetHeight,
            SWP_NOZORDER | SWP_NOACTIVATE);
        if (crossMonitor)
        {
            SetWindowPos(hwnd, IntPtr.Zero, targetLeft, targetTop, targetWidth, targetHeight,
                SWP_NOZORDER | SWP_NOACTIVATE);
        }

        _windows.Add(hwnd);
        _titles.Add(GetWindowTitle(hwnd));
        if (GetWindowRect(hwnd, out RECT actual))
        {
            _lastWindowRects[hwnd] = actual;
        }
        else
        {
            _lastWindowRects[hwnd] = new RECT
            {
                Left   = targetLeft,
                Top    = targetTop,
                Right  = targetLeft + targetWidth,
                Bottom = targetTop + targetHeight,
            };
        }

        ActivateWindow(hwnd);
    }

    /// <summary>指定したウィンドウをこのスタックから除外する。残りが2未満なら自動で閉じる。</summary>
    public void RemoveWindows(IEnumerable<IntPtr> hwnds)
    {
        bool changed = false;
        foreach (var h in hwnds)
        {
            int idx = _windows.IndexOf(h);
            if (idx >= 0)
            {
                _windows.RemoveAt(idx);
                _titles.RemoveAt(idx);
                _lastWindowRects.Remove(h);
                changed = true;
            }
        }
        if (!changed) return;

        if (_windows.Count < 2)
        {
            CloseSafe();
            return;
        }
        if (!_windows.Contains(_activeWindow))
            _activeWindow = _windows[0];
        Redraw();
    }

    public void CloseSafe()
    {
        if (IsDisposed) return;
        try { Hide(); } catch { }
        if (IsHandleCreated)
            BeginInvoke(() => { if (!IsDisposed) Close(); });
        else
            Close();
    }

    // -----------------------------------------------------------------------
    // Win32 ハンドル設定
    // -----------------------------------------------------------------------

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_LAYERED | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
        exStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_LAYERED | WS_EX_NOACTIVATE;
        SetWindowLong(Handle, GWL_EXSTYLE, exStyle);
        SetWindowPos(Handle, HWND_TOPMOST, _initLeft, _initTop, _initWidth, TabBarHeight,
            SWP_NOACTIVATE | SWP_SHOWWINDOW);
        Redraw();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        SetWindowPos(Handle, HWND_TOPMOST, _initLeft, _initTop, _initWidth, TabBarHeight,
            SWP_NOACTIVATE | SWP_SHOWWINDOW);
        Redraw();
    }

    // -----------------------------------------------------------------------
    // レイアウト計算
    // -----------------------------------------------------------------------

    private Rectangle GetCloseButtonSlotRect() =>
        new Rectangle(ClientSize.Width - CloseButtonWidth, 0, CloseButtonWidth, ClientSize.Height);

    private Rectangle GetCloseButtonRect() =>
        ApplyButtonMargin(GetCloseButtonSlotRect());

    private Rectangle GetTabSlotRect(int index)
    {
        int count = _windows.Count;
        if (count <= 0) return Rectangle.Empty;

        int areaW = Math.Max(1, ClientSize.Width - CloseButtonWidth);
        int x1 = index * areaW / count;
        int x2 = (index + 1) * areaW / count;
        return new Rectangle(x1, 0, Math.Max(1, x2 - x1), ClientSize.Height);
    }

    private Rectangle GetTabRect(int index) =>
        ApplyButtonMargin(GetTabSlotRect(index));

    private static Rectangle ApplyButtonMargin(Rectangle slot)
    {
        if (slot.IsEmpty) return Rectangle.Empty;

        int ml = AppConfig.StackButtonMarginLeft;
        int mr = AppConfig.StackButtonMarginRight;
        int mt = AppConfig.StackButtonMarginTop;
        int mb = AppConfig.StackButtonMarginBottom;

        int maxHoriz = Math.Max(0, slot.Width - 2);
        if (ml + mr > maxHoriz)
        {
            double scale = (ml + mr) > 0 ? (double)maxHoriz / (ml + mr) : 0;
            ml = (int)(ml * scale);
            mr = (int)(mr * scale);
        }
        int maxVert = Math.Max(0, slot.Height - 2);
        if (mt + mb > maxVert)
        {
            double scale = (mt + mb) > 0 ? (double)maxVert / (mt + mb) : 0;
            mt = (int)(mt * scale);
            mb = (int)(mb * scale);
        }

        return new Rectangle(
            slot.Left + ml,
            slot.Top + mt,
            Math.Max(1, slot.Width - ml - mr),
            Math.Max(1, slot.Height - mt - mb));
    }

    private int HitTestTab(Point pt)
    {
        if (GetCloseButtonRect().Contains(pt)) return -2; // 閉じるボタン
        for (int i = 0; i < _windows.Count; i++)
        {
            if (GetTabRect(i).Contains(pt)) return i;
        }
        return -1;
    }

    // -----------------------------------------------------------------------
    // 描画（per-pixel alpha 対応 UpdateLayeredWindow）
    // -----------------------------------------------------------------------

    private void Redraw()
    {
        if (IsDisposed || !IsHandleCreated) return;
        int w = ClientSize.Width;
        int h = ClientSize.Height;
        if (w <= 0 || h <= 0) return;

        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            RenderContent(g, w, h);
        }

        IntPtr screenDc = GetDC(IntPtr.Zero);
        IntPtr memDc = CreateCompatibleDC(screenDc);
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;
        try
        {
            hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
            oldBitmap = SelectObject(memDc, hBitmap);

            var size = new SIZE_API { cx = w, cy = h };
            var ptSrc = new POINT_API { x = 0, y = 0 };
            var ptDst = new POINT_API { x = Left, y = Top };
            var blend = new BLENDFUNCTION
            {
                BlendOp             = AC_SRC_OVER,
                BlendFlags          = 0,
                SourceConstantAlpha = (byte)Math.Clamp((int)Math.Round(AppConfig.StackOpacity * 255.0), 1, 255),
                AlphaFormat         = AC_SRC_ALPHA,
            };

            UpdateLayeredWindow(Handle, screenDc, ref ptDst, ref size, memDc, ref ptSrc, 0, ref blend, ULW_ALPHA);
        }
        finally
        {
            if (oldBitmap != IntPtr.Zero) SelectObject(memDc, oldBitmap);
            if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
            if (memDc != IntPtr.Zero) DeleteDC(memDc);
            if (screenDc != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private void RenderContent(Graphics g, int width, int height)
    {
        g.Clear(Color.Transparent);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        if (_barBackColor.A > 0)
        {
            using var barBgBrush = new SolidBrush(_barBackColor);
            g.FillRectangle(barBgBrush, 0, 0, width, height);
        }

        bool rounded = AppConfig.StackButtonRounded && AppConfig.StackButtonCornerRadius > 0;
        int radius = rounded ? AppConfig.StackButtonCornerRadius : 0;
        bool hasButtonGap = rounded
            || AppConfig.StackButtonMarginLeft > 0
            || AppConfig.StackButtonMarginRight > 0
            || AppConfig.StackButtonMarginTop > 0
            || AppConfig.StackButtonMarginBottom > 0;

        Color closeBgColor    = DeriveCloseBgColor(_backgroundColor);
        Color closeHoverColor = Color.FromArgb(196, 43, 28);

        using var activeBrush     = new SolidBrush(_selectedColor);
        using var normalBrush     = new SolidBrush(_backgroundColor);
        using var hoverBrush      = new SolidBrush(_hoverColor);
        using var closeBgBrush    = new SolidBrush(closeBgColor);
        using var closeHoverBrush = new SolidBrush(closeHoverColor);
        using var activeTextBrush = new SolidBrush(GetContrastTextColor(_selectedColor, isActive: true));
        using var normalTextBrush = new SolidBrush(GetContrastTextColor(_backgroundColor, isActive: false));
        using var hoverTextBrush  = new SolidBrush(GetContrastTextColor(_hoverColor, isActive: false));
        using var closeTextBrush  = new SolidBrush(GetContrastTextColor(_hoverClose ? closeHoverColor : closeBgColor, isActive: true));
        using var sepPen          = new Pen(Color.FromArgb(24, 24, 24), 1f);
        using var borderPen       = new Pen(Color.FromArgb(80, 80, 80), 1f);

        using var tabSf = new StringFormat
        {
            Alignment     = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming      = StringTrimming.EllipsisCharacter,
            FormatFlags   = StringFormatFlags.NoWrap,
        };
        using var centerSf = new StringFormat
        {
            Alignment     = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        for (int i = 0; i < _windows.Count; i++)
        {
            Rectangle r = GetTabRect(i);
            bool isActive  = _windows[i] == _activeWindow;
            bool isHovered = i == _hoverTabIndex;
            bool isDetaching = _dragTabOutside && _windows[i] == _dragTabHwnd;

            if (isDetaching)
            {
                using var detachBrush = new SolidBrush(Color.FromArgb(140, 196, 43, 28));
                FillButton(g, detachBrush, r, radius);
            }
            else
            {
                Brush bg = isActive ? activeBrush : isHovered ? hoverBrush : normalBrush;
                FillButton(g, bg, r, radius);
            }

            if (!hasButtonGap && i > 0)
                g.DrawLine(sepPen, r.Left, 2, r.Left, r.Bottom - 2);

            int padX = Math.Min(TabPadX, Math.Max(2, r.Width / 6));
            var textRect = new Rectangle(
                r.Left + padX,
                r.Top,
                Math.Max(0, r.Width - padX * 2),
                r.Height);
            string title = i < _titles.Count ? _titles[i] : "";
            Brush textBrush = isActive ? activeTextBrush : isHovered ? hoverTextBrush : normalTextBrush;
            g.DrawString(
                title,
                isActive ? _activeTabFont : _tabFont,
                textBrush,
                textRect,
                tabSf);
        }

        // 閉じるボタン
        Rectangle closeRect = GetCloseButtonRect();
        FillButton(g, _hoverClose ? closeHoverBrush : closeBgBrush, closeRect, radius);
        if (!hasButtonGap)
            g.DrawLine(sepPen, closeRect.Left, 2, closeRect.Left, closeRect.Bottom - 2);
        g.DrawString("×", _closeFont, closeTextBrush, closeRect, centerSf);

        // 外枠
        if (AppConfig.StackTabBarBorder)
            g.DrawRectangle(borderPen, 0, 0, width - 1, height - 1);

        // ドラッグ＆ドロップ受け入れハイライト
        if (_isDropTarget)
        {
            using var dropOverlay = new SolidBrush(Color.FromArgb(64, _selectedColor));
            using var dropPen     = new Pen(_selectedColor, 2f);
            g.FillRectangle(dropOverlay, 0, 0, width, height);
            g.DrawRectangle(dropPen, 1, 1, Math.Max(1, width - 2), Math.Max(1, height - 2));
        }
    }

    private static void FillButton(Graphics g, Brush brush, Rectangle rect, int radius)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;

        int r = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2);
        if (r <= 0)
        {
            g.FillRectangle(brush, rect);
            return;
        }

        var prevMode = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = CreateRoundedRectPath(rect, r);
        g.FillPath(brush, path);
        g.SmoothingMode = prevMode;
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color DeriveBarBackColor(Color bg)
    {
        return Color.FromArgb(
            255,
            (int)Math.Round(bg.R * 0.67),
            (int)Math.Round(bg.G * 0.67),
            (int)Math.Round(bg.B * 0.67));
    }

    private static Color DeriveCloseBgColor(Color bg)
    {
        return Color.FromArgb(
            255,
            (int)Math.Round(bg.R * 0.78),
            (int)Math.Round(bg.G * 0.78),
            (int)Math.Round(bg.B * 0.78));
    }

    private static Color GetContrastTextColor(Color bg, bool isActive)
    {
        double luma = 0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B;
        if (luma > 155)
            return isActive ? Color.FromArgb(16, 16, 16) : Color.FromArgb(40, 40, 40);
        return isActive ? Color.White : Color.FromArgb(215, 215, 215);
    }

    // -----------------------------------------------------------------------
    // マウス操作
    // -----------------------------------------------------------------------

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_dragTabHwnd != IntPtr.Zero)
        {
            if ((e.Button & MouseButtons.Left) == 0)
            {
                ResetTabDrag();
                return;
            }
            bool outside = !ClientRectangle.Contains(e.Location);
            if (outside != _dragTabOutside)
            {
                _dragTabOutside = outside;
                Cursor = outside ? Cursors.SizeAll : Cursors.Default;
                Redraw();
            }
            return;
        }

        int hit = HitTestTab(e.Location);
        int newTab = hit >= 0 ? hit : -1;
        bool newClose = hit == -2;
        if (newTab != _hoverTabIndex || newClose != _hoverClose)
        {
            _hoverTabIndex = newTab;
            _hoverClose    = newClose;
            Redraw();
        }
    }

    private void OnMouseLeave(object? sender, EventArgs e)
    {
        if (_dragTabHwnd != IntPtr.Zero) return;

        if (_hoverTabIndex != -1 || _hoverClose)
        {
            _hoverTabIndex = -1;
            _hoverClose    = false;
            Redraw();
        }
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        int hit = HitTestTab(e.Location);
        if (hit == -2)
        {
            CloseSafe();
            return;
        }
        if (hit >= 0 && hit < _windows.Count)
        {
            IntPtr hwnd = _windows[hit];
            _dragTabHwnd = hwnd;
            _dragTabOutside = false;
            Capture = true;
            ActivateWindow(hwnd);
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _dragTabHwnd == IntPtr.Zero) return;

        IntPtr hwnd = _dragTabHwnd;
        bool droppedOutside = !ClientRectangle.Contains(e.Location);
        ResetTabDrag();

        if (droppedOutside && hwnd != IntPtr.Zero)
        {
            int idx = _windows.IndexOf(hwnd);
            string title = (idx >= 0 && idx < _titles.Count && !string.IsNullOrWhiteSpace(_titles[idx]))
                ? _titles[idx]
                : GetWindowTitle(hwnd);

            MoveWindowTitleBarToPoint(hwnd, Cursor.Position);
            RemoveWindows(new[] { hwnd });
            TrayIcon.ShowMessage($"Stack から除外しました:\n{title}", durationMs: 3000, clickThrough: true);
        }
    }

    /// <summary>除外したウィンドウのタイトルバーがドロップ位置付近に来るよう移動する（サイズは維持）。</summary>
    private static void MoveWindowTitleBarToPoint(IntPtr hwnd, Point pt)
    {
        if (!IsWindow(hwnd) || IsIconic(hwnd)) return;
        if (IsZoomed(hwnd)) ShowWindow(hwnd, SW_RESTORE);
        if (!GetWindowRect(hwnd, out RECT r)) return;

        int w = r.Width, h = r.Height;
        Rectangle wa = Screen.FromPoint(pt).WorkingArea;

        int left = pt.X - w / 2;
        int top  = pt.Y - 16; // タイトルバー中央付近にカーソルが来る
        left = Math.Max(wa.Left - w + 100, Math.Min(left, wa.Right - 100));
        top  = Math.Max(wa.Top, Math.Min(top, wa.Bottom - 40));

        SetWindowPos(hwnd, IntPtr.Zero, left, top, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private void OnMouseCaptureChanged(object? sender, EventArgs e)
    {
        if (!Capture && _dragTabHwnd != IntPtr.Zero)
            ResetTabDrag();
    }

    private void ResetTabDrag()
    {
        bool needRedraw = _dragTabOutside;
        _dragTabHwnd = IntPtr.Zero;
        _dragTabOutside = false;
        Cursor = Cursors.Default;
        if (Capture) Capture = false;
        if (needRedraw) Redraw();
    }

    private void ActivateWindow(IntPtr hwnd)
    {
        if (!IsWindow(hwnd))
        {
            RemoveWindows(new[] { hwnd });
            return;
        }

        bool wasMinimized = IsIconic(hwnd);
        if (wasMinimized)
            ShowWindow(hwnd, SW_RESTORE);

        // 最小化中に他のウィンドウが移動・リサイズされていた場合に追従させる
        if (wasMinimized && _hasStackRect && _lastStackRect.Width > 0 && _lastStackRect.Height > 0)
        {
            if (IsZoomed(hwnd)) ShowWindow(hwnd, SW_RESTORE);
            SetWindowPos(hwnd, IntPtr.Zero,
                _lastStackRect.Left, _lastStackRect.Top,
                _lastStackRect.Width, _lastStackRect.Height,
                SWP_NOZORDER | SWP_NOACTIVATE);
            if (GetWindowRect(hwnd, out RECT actual))
                _lastWindowRects[hwnd] = actual;
        }

        SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        ForceForeground(hwnd);

        // タブバー自身は常に最上位を維持
        if (IsHandleCreated)
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

        _activeWindow = hwnd;
        _lastObservedFg = GetForegroundWindow();
        Redraw();
    }

    /// <summary>
    /// マウスホイール等の非クリック起点からでも確実に他プロセスウィンドウを前面化する。
    /// </summary>
    private static void ForceForeground(IntPtr hwnd)
    {
        if (SetForegroundWindow(hwnd) && GetForegroundWindow() == hwnd)
            return;

        IntPtr fg = GetForegroundWindow();
        uint curThread = GetCurrentThreadId();
        uint fgThread = fg != IntPtr.Zero ? GetWindowThreadProcessId(fg, out _) : 0;
        uint targetThread = GetWindowThreadProcessId(hwnd, out _);

        bool attachedFg = false;
        bool attachedTarget = false;
        try
        {
            if (fgThread != 0 && fgThread != curThread)
                attachedFg = AttachThreadInput(curThread, fgThread, true);
            if (targetThread != 0 && targetThread != curThread && targetThread != fgThread)
                attachedTarget = AttachThreadInput(curThread, targetThread, true);

            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attachedTarget) AttachThreadInput(curThread, targetThread, false);
            if (attachedFg) AttachThreadInput(curThread, fgThread, false);
        }

        if (GetForegroundWindow() != hwnd)
            SwitchToThisWindow(hwnd, true);
    }

    // -----------------------------------------------------------------------
    // 定期同期（ウィンドウ終了・タイトル変更・アクティブ変化の追従）
    // -----------------------------------------------------------------------

    private void SyncTimer_Tick(object? sender, EventArgs e)
    {
        if (IsDisposed) return;

        bool changed = false;
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            if (!IsWindow(_windows[i]))
            {
                _lastWindowRects.Remove(_windows[i]);
                _windows.RemoveAt(i);
                _titles.RemoveAt(i);
                changed = true;
            }
        }

        if (_windows.Count < 2)
        {
            CloseSafe();
            return;
        }

        // 仮想デスクトップが切り替わり、スタック内のウィンドウが現在のデスクトップに無い場合は非表示にする
        bool onCurrentDesktop = IsAnyWindowOnCurrentDesktop();
        if (!onCurrentDesktop)
        {
            if (!_hiddenByDesktop && IsHandleCreated)
            {
                _hiddenByDesktop = true;
                ShowWindow(Handle, SW_HIDE);
            }
            return;
        }
        else if (_hiddenByDesktop)
        {
            _hiddenByDesktop = false;
            if (IsHandleCreated)
            {
                SetWindowPos(Handle, HWND_TOPMOST, Left, Top, Width, Height,
                    SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            changed = true;
        }

        // いずれかのスタックウィンドウが移動・リサイズされていたら他ウィンドウとタブバーを追従
        if (SyncGeometry())
            changed = true;

        // タイトル・アクティブ状態は 約250ms 間隔で更新
        _titleSyncCounter++;
        if (_titleSyncCounter >= 8)
        {
            _titleSyncCounter = 0;
            for (int i = 0; i < _windows.Count; i++)
            {
                string t = GetWindowTitle(_windows[i]);
                if (_titles[i] != t)
                {
                    _titles[i] = t;
                    changed = true;
                }
            }
        }

        IntPtr fg = GetForegroundWindow();
        if (fg != _lastObservedFg)
        {
            _lastObservedFg = fg;
            if (fg != IntPtr.Zero && _windows.Contains(fg) && _activeWindow != fg)
            {
                _activeWindow = fg;
                changed = true;
            }
        }
        if (!_windows.Contains(_activeWindow))
        {
            _activeWindow = _windows[0];
            changed = true;
        }

        if (changed) Redraw();
    }

    /// <summary>
    /// いずれかのスタックウィンドウの移動・サイズ変更を検出し、
    /// 他のスタックウィンドウおよびタブバーの位置・サイズを同期する。
    /// </summary>
    private bool SyncGeometry()
    {
        bool isLeftButtonDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

        // アクティブウィンドウを優先して変化を検出
        IntPtr leader = IntPtr.Zero;
        RECT leaderRect = default;

        if (_activeWindow != IntPtr.Zero && _windows.Contains(_activeWindow)
            && !IsIconic(_activeWindow) && GetWindowRect(_activeWindow, out RECT activeRect)
            && activeRect.Width > 0 && activeRect.Height > 0
            && ((_pendingTopAdjust && !isLeftButtonDown)
                || !_lastWindowRects.TryGetValue(_activeWindow, out RECT prevActive)
                || !activeRect.EqualsRect(prevActive)))
        {
            leader = _activeWindow;
            leaderRect = activeRect;
        }
        else
        {
            foreach (var w in _windows)
            {
                if (w == _activeWindow || IsIconic(w)) continue;
                if (!GetWindowRect(w, out RECT r) || r.Width <= 0 || r.Height <= 0) continue;
                if (!_lastWindowRects.TryGetValue(w, out RECT prev) || !r.EqualsRect(prev))
                {
                    leader = w;
                    leaderRect = r;
                    break;
                }
            }
        }

        if (leader == IntPtr.Zero) return false;

        bool leaderZoomed = IsZoomed(leader);
        Rectangle wa = Screen.FromHandle(leader).WorkingArea;

        int targetLeft   = leaderRect.Left;
        int targetTop    = leaderRect.Top;
        int targetWidth  = leaderRect.Width;
        int targetHeight = leaderRect.Height;

        int tabLeft  = targetLeft;
        int tabTop   = targetTop - TabBarHeight;
        int tabWidth = Math.Max(CloseButtonWidth + 40, targetWidth);

        _pendingTopAdjust = false;
        if (leaderZoomed)
        {
            // 最大化時は作業領域の上辺にタブバーを配置
            tabLeft  = wa.Left;
            tabTop   = wa.Top;
            tabWidth = Math.Max(CloseButtonWidth + 40, wa.Width);
        }
        else if (tabTop < wa.Top)
        {
            // 上端付近でタブバーが画面外に出る場合
            tabTop = wa.Top;
            if (isLeftButtonDown)
            {
                _pendingTopAdjust = true;
            }
            else
            {
                // ドラッグ完了後であれば、タブバーがタイトルバーに被らないようウィンドウ側も調整
                int adjustedTop = wa.Top + TabBarHeight;
                int adjustedH   = Math.Max(100, leaderRect.Bottom - adjustedTop);
                targetTop    = adjustedTop;
                targetHeight = adjustedH;
                SetWindowPos(leader, IntPtr.Zero, targetLeft, targetTop, targetWidth, targetHeight,
                    SWP_NOZORDER | SWP_NOACTIVATE);
                if (GetWindowRect(leader, out RECT actualLeader))
                {
                    leaderRect   = actualLeader;
                    targetLeft   = actualLeader.Left;
                    targetTop    = actualLeader.Top;
                    targetWidth  = actualLeader.Width;
                    targetHeight = actualLeader.Height;
                }
            }
        }

        _lastWindowRects[leader] = leaderRect;
        _lastStackRect = new RECT
        {
            Left   = targetLeft,
            Top    = targetTop,
            Right  = targetLeft + targetWidth,
            Bottom = targetTop + targetHeight,
        };
        _hasStackRect = true;

        // タブバーの位置・サイズを更新（最上位を維持）
        if (IsHandleCreated)
        {
            SetWindowPos(Handle, HWND_TOPMOST, tabLeft, tabTop, tabWidth, TabBarHeight,
                SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
        else
        {
            Location   = new Point(tabLeft, tabTop);
            ClientSize = new Size(tabWidth, TabBarHeight);
        }

        // 背面の他スタックウィンドウを同期（Z-order とフォーカスは変えない）
        string leaderDevice = Screen.FromHandle(leader).DeviceName;
        int siblingLeft   = leaderZoomed ? wa.Left : targetLeft;
        int siblingTop    = leaderZoomed ? wa.Top + TabBarHeight : targetTop;
        int siblingWidth  = leaderZoomed ? wa.Width : targetWidth;
        int siblingHeight = leaderZoomed ? Math.Max(100, wa.Height - TabBarHeight) : targetHeight;

        foreach (var w in _windows)
        {
            if (w == leader || IsIconic(w)) continue;
            if (IsZoomed(w)) ShowWindow(w, SW_RESTORE);

            bool crossMonitor = !string.Equals(
                Screen.FromHandle(w).DeviceName, leaderDevice, StringComparison.OrdinalIgnoreCase);

            SetWindowPos(w, IntPtr.Zero, siblingLeft, siblingTop, siblingWidth, siblingHeight,
                SWP_NOZORDER | SWP_NOACTIVATE);
            if (crossMonitor)
            {
                SetWindowPos(w, IntPtr.Zero, siblingLeft, siblingTop, siblingWidth, siblingHeight,
                    SWP_NOZORDER | SWP_NOACTIVATE);
            }

            if (GetWindowRect(w, out RECT actualSibling))
                _lastWindowRects[w] = actualSibling;
        }

        return true;
    }

    // -----------------------------------------------------------------------
    // ヘルパー
    // -----------------------------------------------------------------------

    private bool IsAnyWindowOnCurrentDesktop()
    {
        if (!_vdmInitTried)
        {
            _vdmInitTried = true;
            try
            {
                Type? t = Type.GetTypeFromCLSID(CLSID_VirtualDesktopManager);
                if (t != null)
                    _vdm = Activator.CreateInstance(t) as IVirtualDesktopManager;
            }
            catch
            {
                _vdm = null;
            }
        }

        foreach (var w in _windows)
        {
            if (_vdm != null)
            {
                try
                {
                    if (_vdm.IsWindowOnCurrentVirtualDesktop(w, out bool onCurrent) == 0)
                    {
                        if (onCurrent) return true;
                        continue;
                    }
                }
                catch { }
            }

            // フォールバック: DWMWA_CLOAKED で別デスクトップ判定
            if (DwmGetWindowAttribute(w, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0)
            {
                if (cloaked == 0) return true;
            }
            else
            {
                return true;
            }
        }
        return false;
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var sb = new StringBuilder(256);
        int len = GetWindowText(hwnd, sb, sb.Capacity);
        if (len > 0) return sb.ToString();

        try
        {
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid != 0)
            {
                using var proc = Process.GetProcessById((int)pid);
                if (!string.IsNullOrEmpty(proc.ProcessName))
                    return proc.ProcessName;
            }
        }
        catch { }

        return "(無題)";
    }

    private static Color GetAccentColor()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int abgr)
            {
                byte r  = (byte)( abgr        & 0xFF);
                byte gg = (byte)((abgr >>  8) & 0xFF);
                byte b  = (byte)((abgr >> 16) & 0xFF);
                return Color.FromArgb(255, r, gg, b);
            }
        }
        catch { }
        return Color.FromArgb(0, 120, 215);
    }

    // -----------------------------------------------------------------------
    // Dispose
    // -----------------------------------------------------------------------

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _syncTimer.Stop();
            _syncTimer.Dispose();
            _tabFont.Dispose();
            _activeTabFont.Dispose();
            _closeFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
