using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
    private const int WS_EX_NOACTIVATE = 0x08000000;

    // -----------------------------------------------------------------------
    // レイアウト定数
    // -----------------------------------------------------------------------

    /// <summary>タブ切り替えウィンドウの高さ (px)。</summary>
    public const int TabBarHeight = 28;

    private const int CloseButtonWidth = 28;
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
    private int _hoverTabIndex = -1;
    private bool _hoverClose;

    private readonly Color _accentColor;
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

        _accentColor   = GetAccentColor();
        _tabFont       = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        _activeTabFont = new Font("Segoe UI", 8.5f, FontStyle.Bold, GraphicsUnit.Point);
        _closeFont     = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

        FormBorderStyle = FormBorderStyle.None;
        TopMost         = true;
        ShowInTaskbar   = false;
        StartPosition   = FormStartPosition.Manual;
        BackColor       = Color.FromArgb(30, 30, 30);
        DoubleBuffered  = true;

        Location   = new Point(left, top);
        ClientSize = new Size(Math.Max(CloseButtonWidth + 40, width), TabBarHeight);

        _syncTimer = new System.Windows.Forms.Timer { Interval = 33 };
        _syncTimer.Tick += SyncTimer_Tick;
        _syncTimer.Start();

        MouseMove  += OnMouseMove;
        MouseLeave += OnMouseLeave;
        MouseDown  += OnMouseDown;
    }

    // -----------------------------------------------------------------------
    // 公開操作
    // -----------------------------------------------------------------------

    public bool ContainsWindow(IntPtr hwnd) => _windows.Contains(hwnd);

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
        Invalidate();
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
            cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
        exStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLong(Handle, GWL_EXSTYLE, exStyle);
        SetWindowPos(Handle, HWND_TOPMOST, Left, Top, Width, Height,
            SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        SetWindowPos(Handle, HWND_TOPMOST, Left, Top, Width, Height,
            SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    // -----------------------------------------------------------------------
    // レイアウト計算
    // -----------------------------------------------------------------------

    private Rectangle GetCloseButtonRect() =>
        new Rectangle(ClientSize.Width - CloseButtonWidth, 0, CloseButtonWidth, ClientSize.Height);

    private Rectangle GetTabRect(int index)
    {
        int count = _windows.Count;
        if (count <= 0) return Rectangle.Empty;

        int areaW = Math.Max(1, ClientSize.Width - CloseButtonWidth);
        int x1 = index * areaW / count;
        int x2 = (index + 1) * areaW / count;
        return new Rectangle(x1, 0, Math.Max(1, x2 - x1), ClientSize.Height);
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
    // 描画
    // -----------------------------------------------------------------------

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var activeBrush     = new SolidBrush(_accentColor);
        using var normalBrush     = new SolidBrush(Color.FromArgb(45, 45, 45));
        using var hoverBrush      = new SolidBrush(Color.FromArgb(68, 68, 68));
        using var closeBgBrush    = new SolidBrush(Color.FromArgb(35, 35, 35));
        using var closeHoverBrush = new SolidBrush(Color.FromArgb(196, 43, 28));
        using var activeTextBrush = new SolidBrush(Color.White);
        using var normalTextBrush = new SolidBrush(Color.FromArgb(215, 215, 215));
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

            Brush bg = isActive ? activeBrush : isHovered ? hoverBrush : normalBrush;
            g.FillRectangle(bg, r);

            if (i > 0)
                g.DrawLine(sepPen, r.Left, 2, r.Left, r.Bottom - 2);

            var textRect = new Rectangle(
                r.Left + TabPadX,
                r.Top,
                Math.Max(0, r.Width - TabPadX * 2),
                r.Height);
            string title = i < _titles.Count ? _titles[i] : "";
            g.DrawString(
                title,
                isActive ? _activeTabFont : _tabFont,
                isActive ? activeTextBrush : normalTextBrush,
                textRect,
                tabSf);
        }

        // 閉じるボタン
        Rectangle closeRect = GetCloseButtonRect();
        g.FillRectangle(_hoverClose ? closeHoverBrush : closeBgBrush, closeRect);
        g.DrawLine(sepPen, closeRect.Left, 2, closeRect.Left, closeRect.Bottom - 2);
        g.DrawString("×", _closeFont, activeTextBrush, closeRect, centerSf);

        // 外枠
        g.DrawRectangle(borderPen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }

    // -----------------------------------------------------------------------
    // マウス操作
    // -----------------------------------------------------------------------

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        int hit = HitTestTab(e.Location);
        int newTab = hit >= 0 ? hit : -1;
        bool newClose = hit == -2;
        if (newTab != _hoverTabIndex || newClose != _hoverClose)
        {
            _hoverTabIndex = newTab;
            _hoverClose    = newClose;
            Invalidate();
        }
    }

    private void OnMouseLeave(object? sender, EventArgs e)
    {
        if (_hoverTabIndex != -1 || _hoverClose)
        {
            _hoverTabIndex = -1;
            _hoverClose    = false;
            Invalidate();
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
            ActivateWindow(_windows[hit]);
        }
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

        SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        SetForegroundWindow(hwnd);

        // タブバー自身は常に最上位を維持
        if (IsHandleCreated)
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

        _activeWindow = hwnd;
        Invalidate();
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
        if (fg != IntPtr.Zero && _windows.Contains(fg) && _activeWindow != fg)
        {
            _activeWindow = fg;
            changed = true;
        }
        else if (!_windows.Contains(_activeWindow))
        {
            _activeWindow = _windows[0];
            changed = true;
        }

        if (changed) Invalidate();
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
