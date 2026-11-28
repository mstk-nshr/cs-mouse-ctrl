using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

class Program
{
    // Win32 API -------------------------------------------------------------

    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    static extern IntPtr WindowFromPoint(POINT Point);

[DllImport("user32.dll")]
static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

[StructLayout(LayoutKind.Sequential)]
private struct GUITHREADINFO
{
    public int cbSize;
    public uint flags;
    public IntPtr hwndActive, hwndFocus, hwndCapture, hwndMenuOwner, hwndMoveSize, hwndCaret;
    public int rcLeft, rcTop, rcRight, rcBottom;
}
private const uint GUI_INMOVESIZE = 0x00000002;

private static bool IsWindowInMoveSize(IntPtr hwnd)
{
    uint tid = GetWindowThreadProcessId(hwnd, out _);
    if (tid == 0) return false;
    var gti = new GUITHREADINFO { cbSize = Marshal.SizeOf<GUITHREADINFO>() };
    return GetGUIThreadInfo(tid, ref gti) && (gti.flags & GUI_INMOVESIZE) != 0;
}

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    [DllImport("user32.dll")]
    static extern bool ClipCursor(ref RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool ClipCursor(IntPtr lpRect);

    [DllImport("user32.dll")]
    static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
        IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", EntryPoint = "SetWindowsHookEx")]
    static extern IntPtr SetWindowsHookExMouse(int idHook, LowLevelMouseProc lpfn,
        IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("dwmapi.dll")]
    static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute,
        out int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    static extern bool SetSystemCursor(IntPtr hcur, uint id);

    [DllImport("user32.dll")]
    static extern IntPtr CopyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    static extern bool SystemParametersInfo(uint uiAction, uint uiParam,
        IntPtr pvParam, uint fWinIni);

    [DllImport("user32.dll")]
    static extern IntPtr CreateIconIndirect(ref ICONINFO piconinfo);

    [DllImport("user32.dll")]
    static extern bool DestroyCursor(IntPtr hCursor);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi,
        uint usage, out IntPtr ppvBits, IntPtr hSection, uint offset);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint nPlanes,
        uint nBitCount, byte[] lpBits);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

    [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
    static extern void CopyMemory(IntPtr dest, IntPtr src, UIntPtr count);

    // Structs ---------------------------------------------------------------

    private struct POINT { public int X; public int Y; }
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public uint bmiColors;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // Keyboard hook ---------------------------------------------------------

    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_MOUSEHWHEEL = 0x020E;
    private const uint WM_CLOSE = 0x0010;
    private const uint GA_ROOT = 2;
    private const int LLKHF_EXTENDED = 0x01;
    private const int SW_MAXIMIZE = 3;
    private const int SW_MINIMIZE = 6;
    private const int SW_RESTORE = 9;
    private const int DWMWA_CLOAKED = 14;
    private const int KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_MENU_MASK = 0xE8;
    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;
    private const int VK_MBUTTON = 0x04;
    private const int VK_XBUTTON1 = 0x05;
    private const int VK_XBUTTON2 = 0x06;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const uint LLMHF_INJECTED = 0x00000001;
    private const uint LLMHF_LOWER_IL_INJECTED = 0x00000002;

    private static bool IsKeyDownAsync(int vKey) => (GetAsyncKeyState(vKey) & 0x8000) != 0;

    private static IntPtr _hookID = IntPtr.Zero;
    private static IntPtr _mouseHookID = IntPtr.Zero;
    private static bool leftCtrlPressed;
    private static bool rightCtrlPressed;
    private static bool leftAltPressed;
    private static bool rightAltPressed;
    private static bool _altMaskSent;
    private static bool leftWinPressed;
    private static bool rightWinPressed;
    private static bool leftShiftPressed;
    private static bool rightShiftPressed;
    private static IntPtr activeWindow;

    // 高速移動・減速移動: 直前のマウス座標および端数アキュムレータ
    private static POINT _lastMousePt;
    private static bool _lastMousePtValid = false;
    private static double _mouseRemainderX = 0;
    private static double _mouseRemainderY = 0;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref int pvParam, uint fWinIni);

    private const uint SPI_GETMOUSESPEED = 0x0070;
    private const uint SPI_SETMOUSESPEED = 0x0071;
    private const uint SPIF_UPDATEINIFILE = 0x01;

    /// <summary>Windows 標準のマウスポインター速度（1〜20）を取得する。</summary>
    private static int GetSystemMouseSpeed()
    {
        int cur = 0;
        if (!SystemParametersInfo(SPI_GETMOUSESPEED, 0, ref cur, 0))
            return 10;
        return Math.Clamp(cur, 1, 20);
    }

    /// <summary>Windows 標準のマウスポインター速度（1〜20）を指定値に変更する。</summary>
    private static int SetSystemMouseSpeed(int speed)
    {
        int next = Math.Clamp(speed, 1, 20);
        // SPIF_SENDCHANGE は全トップレベルウィンドウへ同期ブロードキャストするため、
        // スライダーやボタン連続操作時の UI ブロックを避けるべく SPIF_UPDATEINIFILE のみ指定する。
        SystemParametersInfo(SPI_SETMOUSESPEED, 0, (IntPtr)next, SPIF_UPDATEINIFILE);
        return next;
    }

    /// <summary>Windows 標準のマウスポインター速度（1〜20）を delta 段階変更し、結果を 2 秒表示する。</summary>
    private static void ChangeSystemMouseSpeed(int delta)
    {
        int cur = GetSystemMouseSpeed();
        int next = SetSystemMouseSpeed(cur + delta);
        TrayIcon.ShowMessage($"マウス速度: {cur} → {next}" + (next == cur ? " (限界)" : ""), 2000);
    }
    private static void ResetMouseSpeedState()
    {
        _lastMousePtValid = false;
        _mouseRemainderX = 0;
        _mouseRemainderY = 0;
        _altMaskSent = false;
        leftCtrlPressed = false;
        rightCtrlPressed = false;
        leftAltPressed = false;
        rightAltPressed = false;
    }

    private static bool _isWaitingForNumpad;
    private static long _lastPauseTick;
    private static Keys _suppressNextKeyUp = Keys.None;

    private static LowLevelKeyboardProc _proc = HookCallback;
    private static LowLevelMouseProc _mouseProc = MouseHookCallback;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const string MutexName = "Global\\cs-mouse-ctrl-single-instance";

    // システムトレイ --------------------------------------------------------

    private static TrayIcon? _trayIcon;
    private static bool _restartRequested;

    // Window Resize Mode ----------------------------------------------------

    private enum ResizePhase { None, TopLeft, BottomRight }
    private static ResizePhase _resizePhase = ResizePhase.None;
    private static IntPtr _resizeTarget = IntPtr.Zero;
    private static POINT _resizeTopLeft;
    private static POINT _resizeBottomRight;
    private static OverlayForm? _overlay;

    // Window Move Mode ------------------------------------------------------

    private static bool _isMoveMode;
    private static IntPtr _moveTarget = IntPtr.Zero;
    private static RECT _moveOriginalRect;
    private static bool _moveWasZoomed;
    private static POINT _moveStartMousePt;
    private static int _lastAppliedMoveX;
    private static int _lastAppliedMoveY;
    private static bool _suppressNextLButtonUp;
    private static bool _suppressNextRButtonUp;
    private static bool _suppressNextMButtonUp;

    // Move Mode 用カーソル（手のひら形状）
    private static IntPtr _moveCursorHandle = IntPtr.Zero;
    private static bool _moveCursorApplied;

    // システムカーソルの種類（SetSystemCursor の id 引数）
    private const uint OCR_NORMAL = 32512;
    private const uint OCR_IBEAM = 32513;
    private const uint OCR_WAIT = 32514;
    private const uint OCR_CROSS = 32515;
    private const uint OCR_UP = 32516;
    private const uint OCR_SIZE = 32640;
    private const uint OCR_ICON = 32641;
    private const uint OCR_SIZENWSE = 32642;
    private const uint OCR_SIZENESW = 32643;
    private const uint OCR_SIZEWE = 32644;
    private const uint OCR_SIZENS = 32645;
    private const uint OCR_SIZEALL = 32646;
    private const uint OCR_ICOCUR = 32647;
    private const uint OCR_NO = 32648;
    private const uint OCR_HAND = 32649;
    private const uint OCR_APPSTARTING = 32650;
    private const uint OCR_HELP = 32651;

    private const uint SPI_SETCURSORS = 0x0057;
    private const uint SPIF_SENDCHANGE = 0x02;

    /// <summary>
    /// 移動モード中に手のひらへ置き換えるシステムカーソルの種類。
    /// 矢印・Iビーム（キャレット）・サイズ変更・待機など、その時点で
    /// 表示され得るカーソルをすべて手のひらに統一する。
    /// </summary>
    private static readonly uint[] _cursorTypes =
    {
        OCR_NORMAL, OCR_IBEAM, OCR_WAIT, OCR_CROSS, OCR_UP,
        OCR_SIZE, OCR_ICON, OCR_SIZENWSE, OCR_SIZENESW, OCR_SIZEWE,
        OCR_SIZENS, OCR_SIZEALL, OCR_ICOCUR, OCR_NO, OCR_HAND,
        OCR_APPSTARTING, OCR_HELP,
    };

    // Dim Overlay (Pause 押下時にデスクトップ全体を黒半透明で覆う) ----------

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private static DimOverlayForm? _dimOverlay;
    private static IntPtr _dimTarget = IntPtr.Zero;
    private static bool _dimTargetWasTopMost;

    // Window Snap Mode -------------------------------------------------------

    private static WindowSnapForm? _snapForm;
    private static IntPtr _snapTarget = IntPtr.Zero;

    // Window Swap Mode (2つのウィンドウの位置とサイズを入れ替え) -------------

    private static readonly uint _currentProcessId = (uint)Environment.ProcessId;
    private static bool _isSwapMode;
    private static IntPtr _swapFirstTarget = IntPtr.Zero;
    private static OverlayForm? _swapOverlay;

    // Window Set Mode (複数アプリの登録・一括起動＆配置復元) -----------------

    private enum WindowSetPhase { None, Menu, Registering, Deleting }
    private static WindowSetPhase _windowSetPhase = WindowSetPhase.None;
    private static bool _windowSetTriggerReleased = true;
    private static readonly System.Collections.Generic.List<(IntPtr HWnd, OverlayForm Overlay)> _windowSetSelections = new();
    private static SynchronizationContext? _uiContext;

    // Edge Extend Mode (辺拡張) ---------------------------------------------

    [DllImport("user32.dll")]
    private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    private const int EDGE_MIN_SIZE = 50;

    private static bool _edgeSelecting;      // Pause → E 後、矢印キーで辺を選択中
    private static bool _isEdgeMode;         // 辺をマウスで移動中
    private static IntPtr _edgeTarget = IntPtr.Zero;
    private static Keys _edgeKey = Keys.None; // 選択に使った矢印キー
    private static bool _edgeKeyReleased;    // 選択キーが一度離されたか（リピート除外）
    private static RECT _edgeRect;
    private static System.Drawing.Rectangle _edgeLimit; // 拡張先（モニタ作業領域）

    private static bool IsArrowKey(Keys k) =>
        k == Keys.Left || k == Keys.Right || k == Keys.Up || k == Keys.Down;

    private static Keys EdgeAlias(Keys k) => k switch
    {
        Keys.W => Keys.Up,
        Keys.S => Keys.Down,
        Keys.A => Keys.Left,
        Keys.D => Keys.Right,
        _ => k,
    };

    private static void StartEdgeSelecting()
    {
        if (activeWindow == IntPtr.Zero || IsSystemWindow(activeWindow)) { CancelNumpadWaiting(); return; }
        _edgeSelecting = true;
        TrayIcon.ShowMessage("辺拡張: 矢印キー または WASD で拡張する辺を選択してください (Esc: キャンセル)", 0, clickThrough: true);
    }

    private static void StartEdgeMode(Keys arrow)
    {
        IntPtr target = activeWindow;
        _edgeSelecting = false;
        CancelNumpadWaiting();
        if (target == IntPtr.Zero || IsSystemWindow(target)) return;

        if (IsZoomed(target)) ShowWindow(target, SW_RESTORE);
        if (!GetWindowRect(target, out RECT rect)) return;
        if (rect.Right <= rect.Left || rect.Bottom <= rect.Top) return;

        SetForegroundWindow(target);
        _edgeTarget = target;
        _edgeRect = rect;
        _edgeKey = arrow;
        _edgeKeyReleased = false;
        _edgeLimit = Screen.FromHandle(target).WorkingArea;
        _isEdgeMode = true;

        // カーソルを対象辺の中央へ移動
        int cx = rect.Left + (rect.Right - rect.Left) / 2;
        int cy = rect.Top + (rect.Bottom - rect.Top) / 2;
        switch (arrow)
        {
            case Keys.Left:  SetCursorPos(rect.Left, cy); break;
            case Keys.Right: SetCursorPos(rect.Right - 1, cy); break;
            case Keys.Up:    SetCursorPos(cx, rect.Top); break;
            default:         SetCursorPos(cx, rect.Bottom - 1); break;
        }

        ApplyEdgeCursor(arrow == Keys.Left || arrow == Keys.Right);
    }

    /// <summary>辺拡張モード中のカーソルを <-> または上下矢印にする。</summary>
    private static void ApplyEdgeCursor(bool horizontal)
    {
        IntPtr shared = LoadCursor(IntPtr.Zero, (int)(horizontal ? OCR_SIZEWE : OCR_SIZENS));
        if (shared == IntPtr.Zero) return;
        var copies = new IntPtr[_cursorTypes.Length];
        for (int i = 0; i < copies.Length; i++) copies[i] = CopyIcon(shared);
        for (int i = 0; i < copies.Length; i++)
            if (copies[i] != IntPtr.Zero) SetSystemCursor(copies[i], _cursorTypes[i]);
        _moveCursorApplied = true;
    }

    private static void ApplyEdgeRect(RECT r)
    {
        _edgeRect = r;
        SetWindowPos(_edgeTarget, IntPtr.Zero, r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top,
            SWP_NOZORDER | SWP_NOACTIVATE);
    }

    /// <summary>マウス位置に合わせて対象辺を移動する。</summary>
    private static void UpdateEdgeFromMouse(POINT pt)
    {
        if (!GetWindowRect(_edgeTarget, out RECT _)) { EndEdgeMode(); return; }
        RECT r = _edgeRect;
        switch (_edgeKey)
        {
            case Keys.Left:  r.Left = Math.Min(pt.X, r.Right - EDGE_MIN_SIZE); break;
            case Keys.Right: r.Right = Math.Max(pt.X + 1, r.Left + EDGE_MIN_SIZE); break;
            case Keys.Up:    r.Top = Math.Min(pt.Y, r.Bottom - EDGE_MIN_SIZE); break;
            default:         r.Bottom = Math.Max(pt.Y + 1, r.Top + EDGE_MIN_SIZE); break;
        }
        if (r.Left != _edgeRect.Left || r.Right != _edgeRect.Right
            || r.Top != _edgeRect.Top || r.Bottom != _edgeRect.Bottom)
            ApplyEdgeRect(r);
    }

    /// <summary>対象辺を Desktop（モニタ作業領域）の境界まで拡張する。</summary>
    private static void ExtendEdgeToBoundary()
    {
        RECT r = _edgeRect;
        Keys key = _edgeKey;
        int[] outer = AppConfig.OuterSpacing; // 上, 右, 下, 左
        switch (key)
        {
            case Keys.Left:  r.Left = _edgeLimit.Left + outer[3]; break;
            case Keys.Right: r.Right = _edgeLimit.Right - outer[1]; break;
            case Keys.Up:    r.Top = _edgeLimit.Top + outer[0]; break;
            default:         r.Bottom = _edgeLimit.Bottom - outer[2]; break;
        }
        if (r.Right - r.Left < EDGE_MIN_SIZE || r.Bottom - r.Top < EDGE_MIN_SIZE) { EndEdgeMode(); return; }
        ApplyEdgeRect(r);
        // 先にモード終了（SetCursorPos が発生させるマウス移動で辺が再計算されないように）
        EndEdgeMode();
        GetCursorPos(out POINT p);
        switch (key)
        {
            case Keys.Left:  SetCursorPos(r.Left, p.Y); break;
            case Keys.Right: SetCursorPos(r.Right - 1, p.Y); break;
            case Keys.Up:    SetCursorPos(p.X, r.Top); break;
            default:         SetCursorPos(p.X, r.Bottom - 1); break;
        }
    }

    private static void EndEdgeMode()
    {
        _edgeSelecting = false;
        if (!_isEdgeMode) return;
        _isEdgeMode = false;
        _edgeTarget = IntPtr.Zero;
        RestoreMoveCursor();
        ResetMouseSpeedState();
    }

    static void Main()
    {
        // 設定ファイルを読み込む（存在しない場合は自動生成）
        AppConfig.Load();

        // 後優先: 既存インスタンスを終了させてから自身が起動する
        KillExistingInstances();

        // 前回の異常終了などでシステムカーソルが置き換わったまま残っている
        // 場合に備え、起動時にレジストリの設定から復元しておく。
        SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDCHANGE);

        using Mutex mutex = new Mutex(true, MutexName, out bool createdNew);
        // createdNew が false になるケースはほぼないが念のため待機
        if (!createdNew)
            mutex.WaitOne();

        _hookID = SetHook(_proc);
        _mouseHookID = SetMouseHook(_mouseProc);

        // UI スレッド用の SynchronizationContext を確保（バックグラウンド通知用）
        var uiCtx = new WindowsFormsSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(uiCtx);
        _uiContext = uiCtx;

        // システムトレイに常駐アイコンを表示
        _trayIcon = new TrayIcon(
            RequestRestart,
            RequestExit,
            ExecuteHelpCommand,
            GetSystemMouseSpeed,
            SetSystemMouseSpeed);

        // 10ms タイマーでカーソル監視
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        timer.Interval = 10;
        timer.Tick += Timer_Tick;
        timer.Start();

        try
        {
            Application.Run();
        }
        finally
        {
            ClipCursor(IntPtr.Zero);
            UnhookWindowsHookEx(_hookID);
            if (_mouseHookID != IntPtr.Zero)
                UnhookWindowsHookEx(_mouseHookID);
            _overlay?.Close();
            CancelMoveMode();
            CancelSwapMode();
            CancelStackMode();
            CloseAllStackTabForms();
            CancelWindowSetMode();
            CloseDimOverlay();
            EndEdgeMode();
            CloseSnapForm();
            TrayIcon.CloseHelpPopup();

            // 生成したカーソルリソースを解放
            if (_moveCursorHandle != IntPtr.Zero)
            {
                DestroyCursor(_moveCursorHandle);
                _moveCursorHandle = IntPtr.Zero;
            }

            _trayIcon?.Dispose();
            _trayIcon = null;

            // トレイメニューから再起動が要求されていれば新しいインスタンスを起動
            if (_restartRequested)
                StartNewInstance();
        }
    }

    /// <summary>
    /// トレイメニューの「停止」: トレイアイコンを片付けてアプリを終了する。
    /// </summary>
    internal static void RequestExit()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
        Application.Exit();
    }

    /// <summary>
    /// トレイメニューの「再起動」: 終了処理後に新しいインスタンスを起動する。
    /// </summary>
    internal static void RequestRestart()
    {
        _restartRequested = true;
        RequestExit();
    }

    /// <summary>
    /// 現在の実行ファイルを新しいプロセスとして起動する。
    /// </summary>
    private static void StartNewInstance()
    {
        try
        {
            string? exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe))
                exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exe)) return;

            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Program] 再起動に失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// 自身と同じ実行ファイル名を持つ既存プロセスをすべて終了させる（後優先）。
    /// </summary>
    private static void KillExistingInstances()
    {
        string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        int currentId  = Process.GetCurrentProcess().Id;

        foreach (Process p in Process.GetProcessesByName(
            Process.GetCurrentProcess().ProcessName))
        {
            if (p.Id == currentId) continue;
            try
            {
                // 同じ実行ファイルのインスタンスのみ終了
                if (string.Equals(p.MainModule?.FileName, currentExe,
                        StringComparison.OrdinalIgnoreCase))
                {
                    p.Kill();
                    p.WaitForExit(3000);
                }
            }
            catch { /* アクセス拒否などは無視 */ }
        }
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule
            ?? throw new InvalidOperationException("現在のプロセスのモジュールを取得できません。"))
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private static IntPtr SetMouseHook(LowLevelMouseProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule
            ?? throw new InvalidOperationException("現在のプロセスのモジュールを取得できません。"))
        {
            return SetWindowsHookExMouse(WH_MOUSE_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
            bool isKeyUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

            if (isKeyDown || isKeyUp)
            {
                bool pressed = isKeyDown;
                switch ((Keys)vkCode)
                {
                    case Keys.LControlKey:
                        leftCtrlPressed = pressed;
                        if (!pressed) { _lastMousePtValid = false; _mouseRemainderX = 0; _mouseRemainderY = 0; }
                        break;
                    case Keys.RControlKey:
                        rightCtrlPressed = pressed;
                        if (!pressed) { _lastMousePtValid = false; _mouseRemainderX = 0; _mouseRemainderY = 0; }
                        break;
                    case Keys.ControlKey:
                        leftCtrlPressed = pressed;
                        rightCtrlPressed = pressed;
                        if (!pressed) { _lastMousePtValid = false; _mouseRemainderX = 0; _mouseRemainderY = 0; }
                        break;
                    case Keys.LMenu:
                        leftAltPressed = pressed;
                        if (!pressed)
                        {
                            _lastMousePtValid = false;
                            _mouseRemainderX = 0;
                            _mouseRemainderY = 0;
                            _altMaskSent = false;
                        }
                        break;
                    case Keys.RMenu:
                        rightAltPressed = pressed;
                        if (!pressed)
                        {
                            _lastMousePtValid = false;
                            _mouseRemainderX = 0;
                            _mouseRemainderY = 0;
                            _altMaskSent = false;
                        }
                        break;
                    case Keys.Menu:
                        leftAltPressed = pressed;
                        rightAltPressed = pressed;
                        if (!pressed)
                        {
                            _lastMousePtValid = false;
                            _mouseRemainderX = 0;
                            _mouseRemainderY = 0;
                            _altMaskSent = false;
                        }
                        break;
                    case Keys.LWin:
                        leftWinPressed = pressed;
                        break;
                    case Keys.RWin:
                        rightWinPressed = pressed;
                        break;
                    case Keys.LShiftKey:
                        leftShiftPressed = pressed;
                        break;
                    case Keys.RShiftKey:
                        rightShiftPressed = pressed;
                        break;
                    case Keys.ShiftKey:
                        leftShiftPressed = pressed;
                        rightShiftPressed = pressed;
                        break;
                }
            }

            // Window Set トリガーキーの解放状態を追跡（キーリピートによる誤遷移を防止）
            if (isKeyUp && (Keys)vkCode == AppConfig.WindowSetKey)
                _windowSetTriggerReleased = true;

            // 直前に消費したキーの KeyUp を抑制
            if (isKeyUp && _suppressNextKeyUp != Keys.None && (Keys)vkCode == _suppressNextKeyUp)
            {
                _suppressNextKeyUp = Keys.None;
                return (IntPtr)1;
            }

            // スナップ UI 表示中: テンキーで即スナップ
            if (_snapForm != null && isKeyDown && TryGetNumpadKey(vkCode, lParam, out Keys snapNumpad))
            {
                var sf = _snapForm;
                if (sf.TrySelectByKey(snapNumpad))
                {
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }
            }

            // ウィンドウ移動モード中のキー処理
            if (_isMoveMode)
            {
                if (isKeyDown)
                {
                    if ((Keys)vkCode == Keys.Enter)
                    {
                        // Enterキー押下にて移動を決定
                        CommitMoveMode();
                    }
                    else
                    {
                        // 他の入力があった場合は、移動前の位置に戻す
                        CancelMoveMode();
                    }
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }
                else if (isKeyUp)
                {
                    return (IntPtr)1;
                }
            }

            // 辺拡張モード中のキー処理
            if (_isEdgeMode)
            {
                if (isKeyDown)
                {
                    Keys k = (Keys)vkCode;
                    if (EdgeAlias(k) == _edgeKey)
                    {
                        if (!_edgeKeyReleased) return (IntPtr)1; // キーリピートは無視
                        ExtendEdgeToBoundary();
                    }
                    EndEdgeMode();
                    _suppressNextKeyUp = k;
                    return (IntPtr)1;
                }
                else if (isKeyUp)
                {
                    if (EdgeAlias((Keys)vkCode) == _edgeKey) _edgeKeyReleased = true;
                    return (IntPtr)1;
                }
            }

            // Window Stack モード中のキー処理（Enter で確定、それ以外は中断）
            if (_isStackMode)
            {
                Keys k = (Keys)vkCode;
                if (!IsModifierKey(k))
                {
                    if (isKeyDown)
                    {
                        if (k == Keys.Enter) CompleteStackMode();
                        else CancelStackMode();
                        _suppressNextKeyUp = k;
                        return (IntPtr)1;
                    }
                    else if (isKeyUp)
                    {
                        return (IntPtr)1;
                    }
                }
            }

            // ウィンドウ位置入れ替えモード中のキー処理
            if (_isSwapMode)
            {
                Keys k = (Keys)vkCode;
                if (!IsModifierKey(k))
                {
                    if (isKeyDown)
                    {
                        CancelSwapMode();
                        _suppressNextKeyUp = k;
                        return (IntPtr)1;
                    }
                    else if (isKeyUp)
                    {
                        return (IntPtr)1;
                    }
                }
            }

            // Window Set モード中のキー処理（一覧待ち / 登録モード / 削除モード）
            if (_windowSetPhase != WindowSetPhase.None)
            {
                Keys k = (Keys)vkCode;
                if (!IsModifierKey(k))
                {
                    if (isKeyDown)
                    {
                        _suppressNextKeyUp = k;
                        if (_windowSetPhase == WindowSetPhase.Menu)
                        {
                            if (k == Keys.Escape || IsPauseOrBreakKey(k))
                            {
                                CancelWindowSetMode();
                            }
                            else if (k == AppConfig.WindowSetKey)
                            {
                                if (!_windowSetTriggerReleased) return (IntPtr)1;
                                StartWindowSetRegisterMode(fromKeyboard: true);
                            }
                            else if (k == Keys.Delete || k == Keys.Back)
                            {
                                StartWindowSetDeleteMode();
                            }
                            else
                            {
                                ExecuteWindowSet(k);
                            }
                        }
                        else if (_windowSetPhase == WindowSetPhase.Deleting)
                        {
                            if (k == Keys.Escape || IsPauseOrBreakKey(k))
                            {
                                CancelWindowSetMode();
                            }
                            else
                            {
                                DeleteWindowSet(k);
                            }
                        }
                        else if (_windowSetPhase == WindowSetPhase.Registering)
                        {
                            if (k == Keys.Escape || IsPauseOrBreakKey(k))
                            {
                                CancelWindowSetMode(showCancelToast: true);
                            }
                            else if (k == AppConfig.WindowSetKey || k == Keys.Delete || k == Keys.Back)
                            {
                                if (k == AppConfig.WindowSetKey && !_windowSetTriggerReleased) return (IntPtr)1;
                                string trigName = AppConfig.KeysToWindowSetName(AppConfig.WindowSetKey);
                                UpdateWindowSetRegisterStatus(
                                    hintOverride: $"'{trigName}' / Del / BackSpace 以外のキーを押して登録してください");
                            }
                            else
                            {
                                CompleteWindowSetRegistration(k);
                            }
                        }
                        return (IntPtr)1;
                    }
                    else if (isKeyUp)
                    {
                        return (IntPtr)1;
                    }
                }
            }

            // Pause / Break キーの処理（NumPad待ちのトグル・キャンセル）
            if (IsPauseOrBreakKey((Keys)vkCode))
            {
                if (isKeyDown && _resizePhase == ResizePhase.None && !_isMoveMode && !_isEdgeMode && !_isSwapMode && _windowSetPhase == WindowSetPhase.None)
                {
                    long now = Stopwatch.GetTimestamp();
                    if (_lastPauseTick == 0 || Stopwatch.GetElapsedTime(_lastPauseTick, now).TotalMilliseconds >= 150)
                    {
                        _lastPauseTick = now;
                        if (_isWaitingForNumpad)
                        {
                            CancelNumpadWaiting();
                        }
                        else
                        {
                            StartNumpadWaiting();
                        }
                    }
                }
                return (IntPtr)1;
            }

            // NumPad 待ち受中のキー処理
            if (_isWaitingForNumpad)
            {
                if ((Keys)vkCode == Keys.Escape)
                {
                    if (isKeyDown)
                    {
                        CancelNumpadWaiting();
                        _suppressNextKeyUp = Keys.Escape;
                    }
                    return (IntPtr)1;
                }

                if (TryGetNumpadKey(vkCode, lParam, out Keys pauseNumpadKey))
                {
                    if (isKeyDown)
                    {
                        MoveCursorToWindowPosition(pauseNumpadKey);
                        CancelNumpadWaiting();
                        _suppressNextKeyUp = (Keys)vkCode;
                    }
                    return (IntPtr)1;
                }

                // 辺拡張: E の後、矢印キーで対象の辺を選択
                if (_edgeSelecting)
                {
                    if (isKeyDown)
                    {
                        Keys k = (Keys)vkCode;
                        Keys arrow = EdgeAlias(k);
                        if (IsArrowKey(arrow)) StartEdgeMode(arrow);
                        else { CancelNumpadWaiting(); _suppressNextKeyUp = k; }
                    }
                    return (IntPtr)1;
                }

                // pause_key → edge_key シーケンス: 辺拡張モード（矢印キー選択待ち）へ
                if (isKeyDown && (Keys)vkCode == AppConfig.EdgeKey)
                {
                    StartEdgeSelecting();
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }

                // pause_key → move_key シーケンス: NumPad 待ち中に move_key が押されたらウィンドウ移動モードへ
                if (isKeyDown && (Keys)vkCode == AppConfig.MoveKey)
                {
                    IntPtr target = activeWindow;
                    CancelNumpadWaiting();
                    TryStartMoveMode(target);
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }

                // pause_key → resize_key シーケンス: NumPad 待ち中に resize_key が押されたらリサイズモードへ
                if (isKeyDown && (Keys)vkCode == AppConfig.ResizeKey)
                {
                    IntPtr target = activeWindow;
                    CancelNumpadWaiting();
                    TryStartResizeMode(target);
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }

                // pause_key → fit_key シーケンス: NumPad 待ち中に fit_key が押されたら空きエリアへフィット
                if (isKeyDown && (Keys)vkCode == AppConfig.FitKey)
                {
                    IntPtr target = activeWindow;
                    CancelNumpadWaiting();
                    FitWindowToLargestFreeArea(target);
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }

                // pause_key → '.' / ',' シーケンス: マウス移動速度（Alt/Ctrl 倍率）の増減
                if (isKeyDown && ((Keys)vkCode == Keys.OemPeriod || (Keys)vkCode == Keys.Oemcomma))
                {
                    bool up = (Keys)vkCode == Keys.OemPeriod;
                    CancelNumpadWaiting();
                    _suppressNextKeyUp = (Keys)vkCode;
                    ChangeSystemMouseSpeed(up ? 1 : -1);
                    return (IntPtr)1;
                }
                // pause_key → topmost_key シーケンス: NumPad 待ち中に topmost_key が押されたら最前面固定/解除
                if (isKeyDown && (Keys)vkCode == AppConfig.TopmostKey)
                {
                    IntPtr target = activeWindow;
                    if (target != IntPtr.Zero && !IsSystemWindow(target))
                    {
                        bool isTopmost = ToggleTopmost(target);
                        CancelNumpadWaiting();
                        TrayIcon.ShowMessage(isTopmost ? "最前面に固定しました (TopMost: ON)" : "最前面固定を解除しました (TopMost: OFF)", 1500);
                    }
                    else
                    {
                        CancelNumpadWaiting();
                    }
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }

                // pause_key → bottom_key シーケンス: NumPad 待ち中に bottom_key が押されたら最下位（Z-order 最背面）へ移動
                if (isKeyDown && (Keys)vkCode == AppConfig.BottomKey)
                {
                    IntPtr target = activeWindow;
                    if (target != IntPtr.Zero && !IsSystemWindow(target))
                    {
                        CancelNumpadWaiting();
                        SendToBottom(target);
                        TrayIcon.ShowMessage("最背面に移動しました (Send to Bottom)", 1500);
                    }
                    else
                    {
                        CancelNumpadWaiting();
                    }
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }

                // pause_key → minimize_key シーケンス: NumPad 待ち中に minimize_key が押されたら最小化
                if (isKeyDown && (Keys)vkCode == AppConfig.MinimizeKey)
                {
                    IntPtr target = activeWindow;
                    if (target != IntPtr.Zero && !IsSystemWindow(target))
                    {
                        CancelNumpadWaiting();
                        ShowWindow(target, SW_MINIMIZE);
                        TrayIcon.ShowMessage("最小化しました (Minimize)", 1500);
                    }
                    else
                    {
                        CancelNumpadWaiting();
                    }
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }

                // pause_key → maximize_key シーケンス: NumPad 待ち中に maximize_key が押されたら最大化（トグル）
                if (isKeyDown && (Keys)vkCode == AppConfig.MaximizeKey)
                {
                    IntPtr target = activeWindow;
                    if (target != IntPtr.Zero && !IsSystemWindow(target))
                    {
                        CancelNumpadWaiting();
                        bool wasZoomed = IsZoomed(target);
                        ShowWindow(target, wasZoomed ? SW_RESTORE : SW_MAXIMIZE);
                        TrayIcon.ShowMessage(wasZoomed ? "元のサイズに戻しました (Restore)" : "最大化しました (Maximize)", 1500);
                    }
                    else
                    {
                        CancelNumpadWaiting();
                    }
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }

                // pause_key → close_key シーケンス: NumPad 待ち中に close_key が押されたらウィンドウを閉じる
                if (isKeyDown && (Keys)vkCode == AppConfig.CloseKey)
                {
                    IntPtr target = activeWindow;
                    if (target != IntPtr.Zero && !IsSystemWindow(target))
                    {
                        CancelNumpadWaiting();
                        PostMessage(target, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        TrayIcon.ShowMessage("ウィンドウを閉じました (Close)", 1500);
                    }
                    else
                    {
                        CancelNumpadWaiting();
                    }
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }

                // pause_key → snap_key シーケンス: NumPad 待ち中に snap_key が押されたらスナップ UI を表示
                if (isKeyDown && (Keys)vkCode == AppConfig.SnapKey)
                {
                    IntPtr target = activeWindow;
                    if (target != IntPtr.Zero && !IsSystemWindow(target))
                    {
                        CancelNumpadWaiting();
                        TryStartSnapMode(target);
                    }
                    else
                    {
                        CancelNumpadWaiting();
                    }
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }

                // pause_key → stack_key シーケンス: Window Stack モードへ
                if (isKeyDown && (Keys)vkCode == AppConfig.StackKey)
                {
                    IntPtr target = activeWindow;
                    CancelNumpadWaiting();
                    TryStartStackMode(target);
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }

                // pause_key → swap_key シーケンス: NumPad 待ち中に swap_key が押されたらウィンドウ位置入れ替えモードへ
                if (isKeyDown && (Keys)vkCode == AppConfig.SwapKey)
                {
                    IntPtr target = activeWindow;
                    CancelNumpadWaiting();
                    TryStartSwapMode(target);
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }

                // pause_key → window_set_key シーケンス: NumPad 待ち中に window_set_key が押されたら Window Set 機能へ
                if (isKeyDown && (Keys)vkCode == AppConfig.WindowSetKey)
                {
                    CancelNumpadWaiting();
                    StartWindowSetMenuMode(fromKeyboard: true);
                    _suppressNextKeyUp = (Keys)vkCode;
                    return (IntPtr)1;
                }
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    // -----------------------------------------------------------------------
    // Window Move Mode
    // -----------------------------------------------------------------------

    private static void TryStartMoveMode(IntPtr preferredTarget = default)
    {
        // 既に移動モード・リサイズモード・入れ替えモード・Window Set モード中なら無視
        if (_isMoveMode || _resizePhase != ResizePhase.None || _isSwapMode || _windowSetPhase != WindowSetPhase.None) return;

        IntPtr target = preferredTarget != IntPtr.Zero ? preferredTarget : activeWindow;

        if (_isWaitingForNumpad)
            CancelNumpadWaiting();

        if (target == IntPtr.Zero)
            target = GetForegroundWindow();

        if (target == IntPtr.Zero) return;
        IntPtr rootWnd = GetAncestor(target, GA_ROOT);
        if (rootWnd == IntPtr.Zero) rootWnd = target;

        // デスクトップ・タスクバーは除外
        if (IsSystemWindow(rootWnd)) return;

        if (!GetWindowRect(rootWnd, out RECT rect)) return;
        if (rect.Right <= rect.Left || rect.Bottom <= rect.Top) return;

        if (!GetCursorPos(out POINT cursorPt)) return;

        // 最大化されている場合は復元してから移動する
        bool wasZoomed = IsZoomed(rootWnd);
        if (wasZoomed)
        {
            ShowWindow(rootWnd, SW_RESTORE);
            if (!GetWindowRect(rootWnd, out rect)) return;
        }

        SetForegroundWindow(rootWnd);

        _moveTarget = rootWnd;
        _moveOriginalRect = rect;
        _moveWasZoomed = wasZoomed;
        _moveStartMousePt = cursorPt;
        _lastAppliedMoveX = rect.Left;
        _lastAppliedMoveY = rect.Top;
        _isMoveMode = true;

        // カーソルを手のひら形状へ変更
        ApplyMoveCursor();
    }

    private static void CommitMoveMode()
    {
        if (!_isMoveMode) return;
        _isMoveMode = false;
        _moveTarget = IntPtr.Zero;
        RestoreMoveCursor();
        ResetMouseSpeedState();
    }

    private static void CancelMoveMode()
    {
        if (!_isMoveMode) return;
        _isMoveMode = false;
        RestoreMoveCursor();
        ResetMouseSpeedState();
        if (_moveTarget != IntPtr.Zero)
        {
            if (_moveWasZoomed)
            {
                ShowWindow(_moveTarget, SW_MAXIMIZE);
            }
            else
            {
                SetWindowPos(_moveTarget, IntPtr.Zero,
                    _moveOriginalRect.Left, _moveOriginalRect.Top, 0, 0,
                    SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            }
            _moveTarget = IntPtr.Zero;
        }
    }

    // -----------------------------------------------------------------------
    // Move Mode 用カーソル（手のひら形状）
    // -----------------------------------------------------------------------

    /// <summary>
    /// ウィンドウ移動モード用の「手のひら」カーソルを生成する（初回のみ）。
    /// </summary>
    private static void EnsureMoveCursor()
    {
        if (_moveCursorHandle != IntPtr.Zero) return;

        try
        {
            int size = Math.Max(32, SystemInformation.CursorSize.Width);
            using var bmp = new System.Drawing.Bitmap(size, size,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                DrawOpenHand(g, size);
            }

            // ホットスポットは画像中央（手のひら付近）
            _moveCursorHandle = CreateCursorFromBitmap(bmp, size / 2, size / 2);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Program] カーソル生成に失敗: {ex.Message}");
            _moveCursorHandle = IntPtr.Zero;
        }
    }

    /// <summary>
    /// 手のひら（開いた手）の形状を描画する。
    /// まず Segoe UI Symbol の ✋ (U+270B) グリフを輪郭として利用し、
    /// 取得できない場合は図形で代替する。
    /// </summary>
    private static void DrawOpenHand(System.Drawing.Graphics g, int size)
    {
        float pad = size * 0.06f;
        float target = size - pad * 2f;

        System.Drawing.Drawing2D.GraphicsPath? path = null;
        try
        {
            using var family = new System.Drawing.FontFamily("Segoe UI Symbol");
            path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddString("\u270B", family,
                (int)System.Drawing.FontStyle.Regular, 100f,
                System.Drawing.PointF.Empty,
                System.Drawing.StringFormat.GenericTypographic);
        }
        catch
        {
            path?.Dispose();
            path = null;
        }

        if (path == null || path.PointCount == 0)
        {
            path?.Dispose();
            path = BuildHandPath();
        }

        System.Drawing.RectangleF b = path.GetBounds();
        if (b.Width <= 0f || b.Height <= 0f)
        {
            path.Dispose();
            return;
        }

        // 目標サイズにフィットさせて中央へ配置
        float scale = Math.Min(target / b.Width, target / b.Height);
        using (var m = new System.Drawing.Drawing2D.Matrix())
        {
            m.Translate(-b.X, -b.Y, System.Drawing.Drawing2D.MatrixOrder.Append);
            m.Scale(scale, scale, System.Drawing.Drawing2D.MatrixOrder.Append);
            m.Translate(
                pad + (target - b.Width * scale) / 2f,
                pad + (target - b.Height * scale) / 2f,
                System.Drawing.Drawing2D.MatrixOrder.Append);
            path.Transform(m);
        }

        using (var fill = new System.Drawing.SolidBrush(System.Drawing.Color.White))
        using (var outline = new System.Drawing.Pen(System.Drawing.Color.Black,
            Math.Max(1f, size / 24f)))
        {
            outline.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
            g.FillPath(fill, path);
            g.DrawPath(outline, path);
        }

        path.Dispose();
    }

    /// <summary>
    /// グリフが利用できない場合の代替: 手のひら・指・親指を図形で組み立てる。
    /// </summary>
    private static System.Drawing.Drawing2D.GraphicsPath BuildHandPath()
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();

        // 手のひら
        AddRoundedRect(path, 8f, 14f, 16f, 15f, 5f);

        // 指（4本）
        float[] fingerX = { 8.8f, 12.6f, 16.4f, 20.2f };
        foreach (float x in fingerX)
            AddRoundedRect(path, x, 5f, 3f, 12f, 1.5f);

        // 親指（左下へ傾ける）
        var thumb = new System.Drawing.Drawing2D.GraphicsPath();
        AddRoundedRect(thumb, 0f, 0f, 3.5f, 8f, 1.6f);
        using (var m = new System.Drawing.Drawing2D.Matrix())
        {
            m.RotateAt(-35f, new System.Drawing.PointF(1.75f, 8f));
            m.Translate(5f, 15f, System.Drawing.Drawing2D.MatrixOrder.Append);
            thumb.Transform(m);
        }
        path.AddPath(thumb, false);
        thumb.Dispose();

        return path;
    }

    private static void AddRoundedRect(System.Drawing.Drawing2D.GraphicsPath path,
        float x, float y, float w, float h, float r)
    {
        float d = r * 2f;
        path.AddArc(x, y, d, d, 180f, 90f);
        path.AddArc(x + w - d, y, d, d, 270f, 90f);
        path.AddArc(x + w - d, y + h - d, d, d, 0f, 90f);
        path.AddArc(x, y + h - d, d, d, 90f, 90f);
        path.CloseFigure();
    }

    /// <summary>
    /// 32bpp ARGB ビットマップから、指定ホットスポットのカーソルを生成する。
    /// </summary>
    private static IntPtr CreateCursorFromBitmap(System.Drawing.Bitmap bmp,
        int hotX, int hotY)
    {
        int w = bmp.Width;
        int h = bmp.Height;

        var bmi = new BITMAPINFO();
        bmi.bmiHeader.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
        bmi.bmiHeader.biWidth = w;
        bmi.bmiHeader.biHeight = -h; // トップダウン
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = 0; // BI_RGB

        IntPtr hColor = CreateDIBSection(IntPtr.Zero, ref bmi, 0,
            out IntPtr bits, IntPtr.Zero, 0);
        if (hColor == IntPtr.Zero) return IntPtr.Zero;

        // ピクセルをコピー（アルファチャンネルを保持）
        var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, w, h),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            for (int y = 0; y < h; y++)
            {
                CopyMemory(IntPtr.Add(bits, y * w * 4),
                    IntPtr.Add(data.Scan0, y * data.Stride),
                    (UIntPtr)(uint)(w * 4));
            }
        }
        finally
        {
            bmp.UnlockBits(data);
        }

        // AND マスク（全 0 = カラービットマップのアルファを使用）
        int maskStride = ((w + 15) / 16) * 2;
        IntPtr hMask = CreateBitmap(w, h, 1, 1, new byte[maskStride * h]);

        var ii = new ICONINFO
        {
            fIcon = false,
            xHotspot = hotX,
            yHotspot = hotY,
            hbmMask = hMask,
            hbmColor = hColor,
        };
        IntPtr hCursor = CreateIconIndirect(ref ii);

        DeleteObject(hColor);
        DeleteObject(hMask);
        return hCursor;
    }

    /// <summary>移動モード中のカーソルを手のひら形状にする。</summary>
    private static void ApplyMoveCursor()
    {
        EnsureMoveCursor();
        if (_moveCursorHandle == IntPtr.Zero) return;

        // SetCursor はスレッド単位で、対象ウィンドウ（別スレッド）の
        // WM_SETCURSOR に上書きされる。そのため、システムカーソル自体を
        // 手のひらに置き換え、どのウィンドウの上でも手のひらを表示する。
        // 矢印だけでなく Iビーム（キャレット）等も含めて置き換える。
        foreach (uint cursorType in _cursorTypes)
        {
            IntPtr copy = CopyIcon(_moveCursorHandle);
            if (copy != IntPtr.Zero)
                SetSystemCursor(copy, cursorType);
        }

        _moveCursorApplied = true;
    }

    /// <summary>移動モード終了時にカーソルを標準の矢印へ戻す。</summary>
    private static void RestoreMoveCursor()
    {
        if (!_moveCursorApplied) return;
        _moveCursorApplied = false;

        // システムカーソルをレジストリの設定から再読み込みして元に戻す
        SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDCHANGE);
    }

    // -----------------------------------------------------------------------
    // Window Resize Mode
    // -----------------------------------------------------------------------

    private static bool IsSystemWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return true;
        if (hWnd == GetDesktopWindow() || hWnd == GetShellWindow()) return true;
        // タスクバー
        IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
        if (hWnd == taskbar) return true;
        // サブモニタのタスクバー
        IntPtr secondTaskbar = FindWindow("Shell_SecondaryTrayWnd", null);
        if (hWnd == secondTaskbar) return true;
        // デスクトップ背景（Progman / WorkerW）およびシェル系ウィンドウ
        var sb = new System.Text.StringBuilder(256);
        if (GetClassName(hWnd, sb, sb.Capacity) > 0)
        {
            string cls = sb.ToString();
            if (cls.Equals("Progman", StringComparison.OrdinalIgnoreCase)
                || cls.Equals("WorkerW", StringComparison.OrdinalIgnoreCase)
                || cls.Equals("Shell_TrayWnd", StringComparison.OrdinalIgnoreCase)
                || cls.Equals("Shell_SecondaryTrayWnd", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsOwnProcessWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return false;
        GetWindowThreadProcessId(hWnd, out uint pid);
        return pid == _currentProcessId;
    }

    private static bool IsModifierKey(Keys k) =>
        k is Keys.LControlKey or Keys.RControlKey or Keys.ControlKey
            or Keys.LMenu or Keys.RMenu or Keys.Menu
            or Keys.LShiftKey or Keys.RShiftKey or Keys.ShiftKey
            or Keys.LWin or Keys.RWin;

    // -----------------------------------------------------------------------
    // Window Swap Mode
    // -----------------------------------------------------------------------

    private static void TryStartSwapMode(IntPtr target)
    {
        if (_isSwapMode || _isMoveMode || _resizePhase != ResizePhase.None || _isEdgeMode || _windowSetPhase != WindowSetPhase.None) return;
        if (target == IntPtr.Zero) return;

        IntPtr rootWnd = GetAncestor(target, GA_ROOT);
        if (rootWnd == IntPtr.Zero) rootWnd = target;

        // デスクトップ背景・タスクバー・自プロセスウィンドウは対象外
        if (IsSystemWindow(rootWnd) || IsOwnProcessWindow(rootWnd)) return;

        // 最大化・最小化状態のウィンドウは操作対象外
        if (IsZoomed(rootWnd))
        {
            TrayIcon.ShowMessage("最大化ウィンドウは入れ替え対象外です", 1500);
            return;
        }
        if (IsIconic(rootWnd)) return;

        if (!GetWindowRect(rootWnd, out RECT rect)) return;
        if (rect.Right <= rect.Left || rect.Bottom <= rect.Top) return;

        _swapFirstTarget = rootWnd;
        _isSwapMode = true;

        // 1つ目のウィンドウを半透明のアクセント色で強調表示
        CloseSwapOverlay();
        _swapOverlay = new OverlayForm();
        _swapOverlay.UpdateRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        _swapOverlay.Show();
        _swapOverlay.BringToTopMost();

        // 入れ替え先選択ポップアップを表示し、マウスポインタを人差し指形状へ変更
        TrayIcon.ShowMessage("入れ替えるWindowをクリックしてください", durationMs: 0, clickThrough: true);
        ApplySwapCursor();
    }

    /// <summary>入れ替え先ウィンドウ選択中のカーソルを人差し指（ハンド）形状にする。</summary>
    private static void ApplySwapCursor()
    {
        IntPtr shared = LoadCursor(IntPtr.Zero, (int)OCR_HAND);
        if (shared == IntPtr.Zero) return;
        var copies = new IntPtr[_cursorTypes.Length];
        for (int i = 0; i < copies.Length; i++) copies[i] = CopyIcon(shared);
        for (int i = 0; i < copies.Length; i++)
            if (copies[i] != IntPtr.Zero) SetSystemCursor(copies[i], _cursorTypes[i]);
        _moveCursorApplied = true;
    }

    private static void CloseSwapOverlay()
    {
        if (_swapOverlay != null)
        {
            var ov = _swapOverlay;
            _swapOverlay = null;
            try { ov.Hide(); } catch { }
            if (!ov.IsDisposed && ov.IsHandleCreated)
                ov.BeginInvoke(() => ov.Close());
            else
                ov.Close();
        }
    }

    private static void CompleteSwapMode(POINT pt)
    {
        if (!_isSwapMode) return;

        IntPtr firstTarget = _swapFirstTarget;
        _isSwapMode = false;
        _swapFirstTarget = IntPtr.Zero;

        // 自アプリのオーバーレイ・案内ポップアップが WindowFromPoint にヒットしないよう先に閉じ、カーソルを復元
        CloseSwapOverlay();
        TrayIcon.CloseHelpPopup();
        RestoreMoveCursor();
        ResetMouseSpeedState();

        IntPtr clicked = WindowFromPoint(pt);
        if (clicked != IntPtr.Zero && IsSystemWindow(clicked))
        {
            TrayIcon.ShowMessage("対象外のウィンドウのためキャンセルしました", 1500);
            return;
        }

        IntPtr secondTarget = clicked == IntPtr.Zero ? IntPtr.Zero : GetAncestor(clicked, GA_ROOT);
        if (secondTarget == IntPtr.Zero) secondTarget = clicked;

        // デスクトップ背景・タスクバー・自プロセスウィンドウは対象外
        if (secondTarget == IntPtr.Zero || IsSystemWindow(secondTarget) || IsOwnProcessWindow(secondTarget))
        {
            TrayIcon.ShowMessage("対象外のウィンドウのためキャンセルしました", 1500);
            return;
        }

        // 最大化・最小化状態のウィンドウは操作対象外
        if (IsZoomed(secondTarget))
        {
            TrayIcon.ShowMessage("最大化ウィンドウは入れ替え対象外です", 1500);
            return;
        }
        if (IsIconic(secondTarget)) return;

        // 同じウィンドウが選択された場合は何もしない
        if (secondTarget == firstTarget) return;

        // 1つ目のウィンドウが依然有効かつ最大化・最小化されていないことを再確認
        if (firstTarget == IntPtr.Zero || !IsWindow(firstTarget)
            || IsSystemWindow(firstTarget) || IsZoomed(firstTarget) || IsIconic(firstTarget))
        {
            return;
        }

        if (!GetWindowRect(firstTarget, out RECT rect1) || !GetWindowRect(secondTarget, out RECT rect2))
            return;

        int w1 = rect1.Right - rect1.Left;
        int h1 = rect1.Bottom - rect1.Top;
        int w2 = rect2.Right - rect2.Left;
        int h2 = rect2.Bottom - rect2.Top;
        if (w1 <= 0 || h1 <= 0 || w2 <= 0 || h2 <= 0) return;

        bool crossMonitor = !string.Equals(
            Screen.FromHandle(firstTarget).DeviceName,
            Screen.FromHandle(secondTarget).DeviceName,
            StringComparison.OrdinalIgnoreCase);

        MoveWindow(firstTarget, rect2.Left, rect2.Top, w2, h2, true);
        MoveWindow(secondTarget, rect1.Left, rect1.Top, w1, h1, true);

        // DPI の異なるモニタ間移動時のスケーリング補正を再適用
        if (crossMonitor)
        {
            MoveWindow(firstTarget, rect2.Left, rect2.Top, w2, h2, true);
            MoveWindow(secondTarget, rect1.Left, rect1.Top, w1, h1, true);
        }
    }

    private static void CancelSwapMode()
    {
        if (!_isSwapMode) return;
        _isSwapMode = false;
        _swapFirstTarget = IntPtr.Zero;
        CloseSwapOverlay();
        TrayIcon.CloseHelpPopup();
        RestoreMoveCursor();
        ResetMouseSpeedState();
    }

    // -----------------------------------------------------------------------
    // Window Stack Mode
    // -----------------------------------------------------------------------

    private static bool _isStackMode;
    private static IntPtr _stackBase = IntPtr.Zero;
    private static readonly List<IntPtr> _stackTargets = new();
    private static OverlayForm? _stackBaseOverlay;
    private static readonly List<OverlayForm> _stackOverlays = new();
    private static readonly List<WindowStackTabForm> _stackTabForms = new();

    private static void TryStartStackMode(IntPtr target)
    {
        if (_isStackMode || _isSwapMode || _isMoveMode || _resizePhase != ResizePhase.None || _isEdgeMode || _windowSetPhase != WindowSetPhase.None) return;

        // 指定が無い場合はマウスポインタ下のウィンドウ
        if (target == IntPtr.Zero && GetCursorPos(out POINT cp))
            target = WindowFromPoint(cp);
        if (target == IntPtr.Zero) return;

        IntPtr rootWnd = GetAncestor(target, GA_ROOT);
        if (rootWnd == IntPtr.Zero) rootWnd = target;
        if (IsSystemWindow(rootWnd) || IsOwnProcessWindow(rootWnd) || IsIconic(rootWnd)) return;
        if (!GetWindowRect(rootWnd, out RECT rect)) return;
        if (rect.Right <= rect.Left || rect.Bottom <= rect.Top) return;

        _stackBase = rootWnd;
        _stackTargets.Clear();
        _isStackMode = true;
        RebuildStackOverlays();

        TrayIcon.ShowMessage("click window to select, press Enter to stack them", durationMs: 0, clickThrough: true);
        ApplySwapCursor();
    }

    private static void CloseStackOverlays()
    {
        var all = new List<OverlayForm>(_stackOverlays);
        if (_stackBaseOverlay != null) all.Add(_stackBaseOverlay);
        _stackOverlays.Clear();
        _stackBaseOverlay = null;
        foreach (var ov in all)
        {
            try { ov.Hide(); } catch { }
            if (!ov.IsDisposed && ov.IsHandleCreated)
                ov.BeginInvoke(() => ov.Close());
            else
                ov.Close();
        }
    }

    private static void RebuildStackOverlays()
    {
        CloseStackOverlays();
        if (GetWindowRect(_stackBase, out RECT br))
        {
            _stackBaseOverlay = new OverlayForm();
            _stackBaseOverlay.UpdateRect(br.Left, br.Top, br.Right, br.Bottom);
            _stackBaseOverlay.Show();
            _stackBaseOverlay.BringToTopMost();
        }
        foreach (var w in _stackTargets)
        {
            if (!GetWindowRect(w, out RECT r)) continue;
            var ov = new OverlayForm();
            ov.UpdateRect(r.Left, r.Top, r.Right, r.Bottom);
            ov.Show();
            ov.BringToTopMost();
            _stackOverlays.Add(ov);
        }
    }

    private static void HandleStackClick(POINT pt)
    {
        if (!_isStackMode) return;

        // オーバーレイ・ポップアップが WindowFromPoint にヒットしないよう一旦閉じる
        CloseStackOverlays();
        IntPtr clicked = WindowFromPoint(pt);
        IntPtr root = clicked == IntPtr.Zero ? IntPtr.Zero : GetAncestor(clicked, GA_ROOT);
        if (root == IntPtr.Zero) root = clicked;

        if (root != IntPtr.Zero && root != _stackBase
            && !IsSystemWindow(root) && !IsOwnProcessWindow(root) && !IsIconic(root))
        {
            // 既に選択済みならトグルで解除
            if (!_stackTargets.Remove(root))
                _stackTargets.Add(root);
        }
        _stackTargets.RemoveAll(w => !IsWindow(w));
        RebuildStackOverlays();
    }

    private static void CompleteStackMode()
    {
        if (!_isStackMode) return;
        IntPtr baseWnd = _stackBase;
        var targets = new List<IntPtr>(_stackTargets);
        EndStackMode();

        if (baseWnd == IntPtr.Zero || !IsWindow(baseWnd) || !GetWindowRect(baseWnd, out RECT rect)) return;

        var validTargets = new List<IntPtr>();
        foreach (var t in targets)
        {
            if (t != baseWnd && IsWindow(t) && !IsIconic(t))
                validTargets.Add(t);
        }
        if (validTargets.Count == 0)
        {
            SetForegroundWindow(baseWnd);
            return;
        }

        // 基準ウィンドウが既に既存スタック直下に配置されていた場合は、そのタブ領域を含めた元の範囲を基準にする
        var allStackWindows = new List<IntPtr> { baseWnd };
        allStackWindows.AddRange(validTargets);
        foreach (var existing in _stackTabForms.ToArray())
        {
            if (existing.IsDisposed) continue;
            if (existing.ContainsWindow(baseWnd)
                && existing.Left == rect.Left
                && existing.Width == (rect.Right - rect.Left)
                && existing.Bottom == rect.Top)
            {
                rect.Top = existing.Top;
            }
            existing.RemoveWindows(allStackWindows);
        }

        int w = rect.Right - rect.Left;
        int h = rect.Bottom - rect.Top;
        if (w <= 0 || h <= 0) return;

        int tabH = WindowStackTabForm.TabBarHeight;
        int winTop = rect.Top + tabH;
        int winH = Math.Max(100, h - tabH);

        // 基準ウィンドウもタブ高さ分だけ下にずらして高さを調整
        if (IsZoomed(baseWnd)) ShowWindow(baseWnd, SW_RESTORE);
        MoveWindow(baseWnd, rect.Left, winTop, w, winH, true);

        foreach (var t in validTargets)
        {
            if (IsZoomed(t)) ShowWindow(t, SW_RESTORE);
            bool cross = !string.Equals(
                Screen.FromHandle(t).DeviceName, Screen.FromHandle(baseWnd).DeviceName,
                StringComparison.OrdinalIgnoreCase);
            MoveWindow(t, rect.Left, winTop, w, winH, true);
            if (cross) MoveWindow(t, rect.Left, winTop, w, winH, true);
        }

        // 基準ウィンドウを最前面（アクティブ）にする
        SetForegroundWindow(baseWnd);

        // 移動後の基準ウィンドウの実矩形に合わせてタブバー位置・幅を確定
        int tabLeft = rect.Left;
        int tabTop  = rect.Top;
        int tabW    = w;
        if (GetWindowRect(baseWnd, out RECT actualBase)
            && actualBase.Right > actualBase.Left && actualBase.Bottom > actualBase.Top)
        {
            Rectangle wa = Screen.FromHandle(baseWnd).WorkingArea;
            tabLeft = actualBase.Left;
            tabTop  = Math.Max(wa.Top, actualBase.Top - tabH);
            tabW    = actualBase.Right - actualBase.Left;
        }

        // タブ切り替えウィンドウをスタック領域の上辺（最上位）に表示
        var tabForm = new WindowStackTabForm(allStackWindows, tabLeft, tabTop, tabW, baseWnd);
        tabForm.FormClosed += (_, _) => _stackTabForms.Remove(tabForm);
        _stackTabForms.Add(tabForm);
        tabForm.Show();
    }

    private static void CloseAllStackTabForms()
    {
        foreach (var form in _stackTabForms.ToArray())
            form.CloseSafe();
        _stackTabForms.Clear();
    }

    private static int _stackWheelAccum;
    private static long _stackWheelLastTick;

    // タイトルバードラッグによる既存スタックへの追加（ドロップ）追跡
    private static IntPtr _stackDropCandidateHwnd = IntPtr.Zero;
    private static RECT _stackDropStartRect;
    private static bool _stackDropStartZoomed;
    private static bool _stackDropIsDragging;
    private static WindowStackTabForm? _stackDropHighlightedForm;

    private static bool IsManagedByAnyStack(IntPtr hwnd)
    {
        foreach (var form in _stackTabForms)
        {
            if (!form.IsDisposed && form.ContainsWindow(hwnd))
                return true;
        }
        return false;
    }

    private static void ClearStackDropHighlight()
    {
        if (_stackDropHighlightedForm != null)
        {
            var prev = _stackDropHighlightedForm;
            _stackDropHighlightedForm = null;
            if (!prev.IsDisposed && prev.IsHandleCreated)
                prev.BeginInvoke(() => prev.SetDropTargetHighlight(false));
        }
    }

    private static void ResetStackDropState()
    {
        _stackDropCandidateHwnd = IntPtr.Zero;
        _stackDropStartZoomed = false;
        _stackDropIsDragging = false;
        ClearStackDropHighlight();
    }

    private static WindowStackTabForm? FindStackTabFormAt(POINT pt)
    {
        for (int i = _stackTabForms.Count - 1; i >= 0; i--)
        {
            var form = _stackTabForms[i];
            if (!form.IsDisposed && form.ContainsScreenPoint(pt.X, pt.Y))
                return form;
        }
        return null;
    }

    private static bool HasWindowMovedForStackDrop(IntPtr hwnd, RECT startRect, bool startZoomed)
    {
        if (!GetWindowRect(hwnd, out RECT curRect))
            return false;

        // 辺・角リサイズ（対辺が固定）と区別しつつ、スナップ解除・最大化解除・異DPI間移動でも移動を検出
        bool movedX = curRect.Left != startRect.Left && curRect.Right != startRect.Right;
        bool movedY = curRect.Top != startRect.Top && curRect.Bottom != startRect.Bottom;
        if (movedX || movedY)
            return true;

        bool movedAny = curRect.Left != startRect.Left || curRect.Top != startRect.Top;
        return movedAny && startZoomed && !IsZoomed(hwnd);
    }

    private static void HandleStackDropMouseEvent(int msg, POINT pt)
    {
        if (_stackTabForms.Count == 0)
        {
            if (_stackDropCandidateHwnd != IntPtr.Zero || _stackDropHighlightedForm != null)
                ResetStackDropState();
            return;
        }

        if (msg == WM_LBUTTONDOWN)
        {
            ResetStackDropState();
            IntPtr clicked = WindowFromPoint(pt);
            IntPtr root = clicked == IntPtr.Zero ? IntPtr.Zero : GetAncestor(clicked, GA_ROOT);
            if (root == IntPtr.Zero) root = clicked;

            if (root != IntPtr.Zero
                && !IsSystemWindow(root)
                && !IsOwnProcessWindow(root)
                && !IsIconic(root)
                && !IsManagedByAnyStack(root)
                && GetWindowRect(root, out RECT startRect)
                && startRect.Right > startRect.Left && startRect.Bottom > startRect.Top)
            {
                _stackDropCandidateHwnd = root;
                _stackDropStartRect = startRect;
                _stackDropStartZoomed = IsZoomed(root);
                _stackDropIsDragging = false;
            }
            return;
        }

        if (msg == WM_MOUSEMOVE)
        {
            if (_stackDropCandidateHwnd == IntPtr.Zero) return;

            if (!IsWindow(_stackDropCandidateHwnd) || IsIconic(_stackDropCandidateHwnd))
            {
                ResetStackDropState();
                return;
            }

            if (!_stackDropIsDragging
                && HasWindowMovedForStackDrop(_stackDropCandidateHwnd, _stackDropStartRect, _stackDropStartZoomed))
            {
                _stackDropIsDragging = true;
            }

            if (_stackDropIsDragging)
            {
                var targetForm = FindStackTabFormAt(pt);
                if (targetForm != _stackDropHighlightedForm)
                {
                    ClearStackDropHighlight();
                    if (targetForm != null && targetForm.IsHandleCreated)
                    {
                        _stackDropHighlightedForm = targetForm;
                        targetForm.BeginInvoke(() => targetForm.SetDropTargetHighlight(true));
                    }
                }
            }
            return;
        }

        if (msg == WM_LBUTTONUP)
        {
            IntPtr droppedHwnd = _stackDropCandidateHwnd;
            RECT startRect = _stackDropStartRect;
            bool startZoomed = _stackDropStartZoomed;
            bool wasDragging = _stackDropIsDragging;
            ResetStackDropState();

            if (droppedHwnd == IntPtr.Zero) return;
            if (!IsWindow(droppedHwnd) || IsIconic(droppedHwnd) || IsManagedByAnyStack(droppedHwnd)) return;

            if (!wasDragging)
                wasDragging = HasWindowMovedForStackDrop(droppedHwnd, startRect, startZoomed);
            if (!wasDragging) return;

            var targetForm = FindStackTabFormAt(pt);
            if (targetForm != null && targetForm.IsHandleCreated)
            {
                // ドロップ元ウィンドウが WM_LBUTTONUP / WM_EXITSIZEMOVE を完了した直後にスタックへ追加する
                targetForm.BeginInvoke(() =>
                {
                    if (targetForm.IsDisposed) return;
                    var delay = new System.Windows.Forms.Timer { Interval = 40 };
                    int waited = 0;
                    delay.Tick += (_, _) =>
                    {
                        // ドロップ元の移動モーダルループが終了するまで待機（最大 ~1.5 秒）
                        waited += delay.Interval;
                        if (waited < 1500 && IsWindow(droppedHwnd) && IsWindowInMoveSize(droppedHwnd))
                            return;
                        delay.Stop();
                        delay.Dispose();
                        if (!targetForm.IsDisposed && IsWindow(droppedHwnd) && !IsManagedByAnyStack(droppedHwnd))
                            targetForm.AddWindowAndActivate(droppedHwnd);
                    };
                    delay.Start();
                });
            }
        }
    }

    private static bool TryHandleStackTabWheel(POINT pt, short delta)
    {
        for (int i = _stackTabForms.Count - 1; i >= 0; i--)
        {
            var form = _stackTabForms[i];
            if (form.IsDisposed) continue;
            if (form.ContainsScreenPoint(pt.X, pt.Y))
            {
                long now = Environment.TickCount64;
                if (now - _stackWheelLastTick > 300 || (_stackWheelAccum != 0 && Math.Sign(_stackWheelAccum) != Math.Sign(delta)))
                    _stackWheelAccum = 0;
                _stackWheelLastTick = now;
                _stackWheelAccum += delta;

                const int WheelDelta = 120;
                if (Math.Abs(_stackWheelAccum) >= WheelDelta)
                {
                    int step = _stackWheelAccum > 0 ? -1 : 1;
                    _stackWheelAccum = 0;
                    if (form.IsHandleCreated)
                        form.BeginInvoke(() => form.SwitchTabByDelta(step));
                }
                return true;
            }
        }
        return false;
    }

    private static void EndStackMode()
    {
        _isStackMode = false;
        _stackBase = IntPtr.Zero;
        _stackTargets.Clear();
        CloseStackOverlays();
        TrayIcon.CloseHelpPopup();
        RestoreMoveCursor();
        ResetMouseSpeedState();
    }

    private static void CancelStackMode()
    {
        if (!_isStackMode) return;
        EndStackMode();
    }

    private static void TryStartResizeMode(IntPtr preferredTarget = default)
    {
        // 既にリサイズモード中・移動モード中・入れ替えモード・Window Set モード中なら無視
        if (_resizePhase != ResizePhase.None || _isMoveMode || _isSwapMode || _windowSetPhase != WindowSetPhase.None) return;

        IntPtr target = preferredTarget != IntPtr.Zero ? preferredTarget : activeWindow;

        if (_isWaitingForNumpad)
            CancelNumpadWaiting();

        if (target == IntPtr.Zero)
            target = GetForegroundWindow();

        if (target == IntPtr.Zero) return;
        IntPtr rootWnd = GetAncestor(target, GA_ROOT);
        if (rootWnd == IntPtr.Zero) return;

        // デスクトップ・タスクバーは除外
        if (IsSystemWindow(rootWnd)) return;

        if (!GetWindowRect(rootWnd, out RECT rect)) return;
        if (rect.Right <= rect.Left || rect.Bottom <= rect.Top) return;

        _resizeTarget = rootWnd;
        _resizeTopLeft = new POINT { X = rect.Left, Y = rect.Top };
        _resizeBottomRight = new POINT { X = rect.Right, Y = rect.Bottom };
        _resizePhase = ResizePhase.TopLeft;

        // オーバーレイ表示
        Application.OpenForms.OfType<OverlayForm>().ToList().ForEach(f => f.Close());
        _overlay = new OverlayForm();
        _overlay.UpdateRect(_resizeTopLeft.X, _resizeTopLeft.Y,
            _resizeBottomRight.X, _resizeBottomRight.Y);
        _overlay.Show();
        _overlay.BringToTopMost();

        // マウスをBOX左上へ移動
        SetCursorPos(_resizeTopLeft.X, _resizeTopLeft.Y);

        // マウスフック: 常時フック済みなので追加登録不要
    }

    private static void CancelResizeMode()
    {
        _resizePhase = ResizePhase.None;
        _resizeTarget = IntPtr.Zero;
        // マウスフックは常時維持するので解除しない
        CloseOverlay();
        ResetMouseSpeedState();
    }

    private static void CloseOverlay()
    {
        if (_overlay != null)
        {
            var ov = _overlay;
            _overlay = null;
            // UIスレッドで閉じる
            if (ov.IsHandleCreated)
                ov.BeginInvoke(() => ov.Close());
        }
    }

    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            MSLLHOOKSTRUCT ms = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            int msg = wParam.ToInt32();

            // 移動モード終了時の UP イベント抑制
            if (msg == WM_LBUTTONUP && _suppressNextLButtonUp)
            {
                _suppressNextLButtonUp = false;
                return (IntPtr)1;
            }
            if (msg == WM_RBUTTONUP && _suppressNextRButtonUp)
            {
                _suppressNextRButtonUp = false;
                return (IntPtr)1;
            }
            if (msg == WM_MBUTTONUP && _suppressNextMButtonUp)
            {
                _suppressNextMButtonUp = false;
                return (IntPtr)1;
            }

            if (msg == WM_MOUSEMOVE)
            {
                if (_isWaitingForNumpad)
                    TrayIcon.TryHandleHelpPopupMouse(msg, ms.pt.X, ms.pt.Y);

                // スタック未管理ウィンドウのタイトルバードラッグ追跡（モーダル外）
                if (_stackTabForms.Count > 0
                    && !_isEdgeMode && !_isStackMode && !_isSwapMode
                    && _windowSetPhase == WindowSetPhase.None
                    && !_isWaitingForNumpad
                    && _resizePhase == ResizePhase.None
                    && !_isMoveMode)
                {
                    HandleStackDropMouseEvent(WM_MOUSEMOVE, ms.pt);
                }

                // --- 高速移動 (Resize Mode, Move Mode 外のみ) ---
                if (_resizePhase == ResizePhase.None && !_isMoveMode && !_isEdgeMode)
                {
                    // 1. 合成イベント（自身が SetCursorPos で注入したものや他ツールによる注入）は除外
                    bool isInjected = (ms.flags & (LLMHF_INJECTED | LLMHF_LOWER_IL_INJECTED)) != 0;
                    if (isInjected)
                    {
                        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
                    }

                    // 2. マウスボタン（左・右・中央・拡張ボタン）押下中（ドラッグ中）は加速しない
                    //    PowerToys FancyZones や Window Snap によるウィンドウのドラッグ、
                    //    通常のドラッグ＆ドロップ、矩形選択等の操作を妨害しないようにする
                    bool isAnyMouseButtonDown = IsKeyDownAsync(VK_LBUTTON)
                        || IsKeyDownAsync(VK_RBUTTON)
                        || IsKeyDownAsync(VK_MBUTTON)
                        || IsKeyDownAsync(VK_XBUTTON1)
                        || IsKeyDownAsync(VK_XBUTTON2);

                    if (isAnyMouseButtonDown)
                    {
                        _lastMousePtValid = false;
                        _mouseRemainderX = 0;
                        _mouseRemainderY = 0;
                        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
                    }

                    // 3. 物理的なキー押下状態を GetAsyncKeyState で同期・取得
                    //    PowerToys や Window Snap、他アプリのフックによる KeyUp/KeyDown 取りこぼし（キー状態スタック）を解消する
                    bool ctrlDown = IsKeyDownAsync(VK_CONTROL);
                    bool altDown  = IsKeyDownAsync(VK_MENU);

                    // 内部フラグとの同期
                    if (!ctrlDown)
                    {
                        leftCtrlPressed = false;
                        rightCtrlPressed = false;
                    }
                    if (!altDown)
                    {
                        leftAltPressed = false;
                        rightAltPressed = false;
                        _altMaskSent = false;
                    }

                    if (altDown || ctrlDown)
                    {
                        double multiplier = ctrlDown ? AppConfig.CtrlMultiplier : AppConfig.AltMultiplier;

                        if (altDown && !_altMaskSent)
                        {
                            // Alt 単体押し離しによるアクティブウィンドウのメニューバー活性化を抑止
                            keybd_event(VK_MENU_MASK, 0, 0, UIntPtr.Zero);
                            keybd_event(VK_MENU_MASK, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                            _altMaskSent = true;
                        }

                        if (_lastMousePtValid)
                        {
                            int dx = ms.pt.X - _lastMousePt.X;
                            int dy = ms.pt.Y - _lastMousePt.Y;

                            // 異常に大きなジャンプ（マルチモニタ間のワープや外部ツールの急激な位置変更等）は破棄
                            if (Math.Abs(dx) > 1000 || Math.Abs(dy) > 1000)
                            {
                                _lastMousePt = ms.pt;
                                _mouseRemainderX = 0;
                                _mouseRemainderY = 0;
                                return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
                            }

                            if (dx != 0 || dy != 0)
                            {
                                double moveX = dx * multiplier + _mouseRemainderX;
                                double moveY = dy * multiplier + _mouseRemainderY;

                                int intMoveX = (int)Math.Round(moveX, MidpointRounding.AwayFromZero);
                                int intMoveY = (int)Math.Round(moveY, MidpointRounding.AwayFromZero);

                                _mouseRemainderX = moveX - intMoveX;
                                _mouseRemainderY = moveY - intMoveY;

                                int newX = _lastMousePt.X + intMoveX;
                                int newY = _lastMousePt.Y + intMoveY;

                                // 仮想スクリーン範囲にクランプ
                                newX = Math.Clamp(newX,
                                    SystemInformation.VirtualScreen.Left,
                                    SystemInformation.VirtualScreen.Right - 1);
                                newY = Math.Clamp(newY,
                                    SystemInformation.VirtualScreen.Top,
                                    SystemInformation.VirtualScreen.Bottom - 1);

                                _lastMousePt = new POINT { X = newX, Y = newY };
                                SetCursorPos(newX, newY);
                                return (IntPtr)1; // 元のイベントを抑制
                            }

                            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
                        }

                        _lastMousePt = ms.pt;
                        _lastMousePtValid = true;
                        _mouseRemainderX = 0;
                        _mouseRemainderY = 0;
                        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
                    }
                    else
                    {
                        _lastMousePtValid = false;
                        _mouseRemainderX = 0;
                        _mouseRemainderY = 0;
                    }
                }

                // --- Window Resize Mode ---
                if (_resizePhase != ResizePhase.None)
                {
                    if (_resizePhase == ResizePhase.TopLeft)
                    {
                        _resizeTopLeft = ms.pt;
                        UpdateOverlay();
                    }
                    else if (_resizePhase == ResizePhase.BottomRight)
                    {
                        _resizeBottomRight = ms.pt;
                        UpdateOverlay();
                    }
                }

                // --- Edge Extend Mode ---
                if (_isEdgeMode)
                    UpdateEdgeFromMouse(ms.pt);

                // --- Window Move Mode ---
                if (_isMoveMode)
                {
                    if (!GetWindowRect(_moveTarget, out RECT _))
                    {
                        _isMoveMode = false;
                        _moveTarget = IntPtr.Zero;
                    }
                    else
                    {
                        int dx = ms.pt.X - _moveStartMousePt.X;
                        int dy = ms.pt.Y - _moveStartMousePt.Y;
                        int newX = _moveOriginalRect.Left + dx;
                        int newY = _moveOriginalRect.Top + dy;

                        if (newX != _lastAppliedMoveX || newY != _lastAppliedMoveY)
                        {
                            _lastAppliedMoveX = newX;
                            _lastAppliedMoveY = newY;
                            SetWindowPos(_moveTarget, IntPtr.Zero, newX, newY, 0, 0,
                                SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                        }
                    }
                }
            }
            else if (_isEdgeMode)
            {
                // マウスクリック等で、その時点の位置で辺拡張モードを終了
                if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN)
                {
                    if (msg == WM_LBUTTONDOWN) _suppressNextLButtonUp = true;
                    else if (msg == WM_RBUTTONDOWN) _suppressNextRButtonUp = true;
                    else _suppressNextMButtonUp = true;
                    EndEdgeMode();
                    return (IntPtr)1;
                }
                if (msg != WM_LBUTTONUP && msg != WM_RBUTTONUP && msg != WM_MBUTTONUP)
                {
                    EndEdgeMode(); // ホイール等
                    return (IntPtr)1;
                }
            }
            else if (_isStackMode)
            {
                if (msg == WM_LBUTTONDOWN)
                {
                    _suppressNextLButtonUp = true;
                    HandleStackClick(ms.pt);
                    return (IntPtr)1;
                }
                if (msg == WM_LBUTTONUP || msg == WM_RBUTTONUP || msg == WM_MBUTTONUP)
                {
                    return (IntPtr)1;
                }
                if (msg == WM_RBUTTONDOWN) _suppressNextRButtonUp = true;
                else if (msg == WM_MBUTTONDOWN) _suppressNextMButtonUp = true;

                CancelStackMode();
                return (IntPtr)1;
            }
            else if (_isSwapMode)
            {
                if (msg == WM_LBUTTONDOWN)
                {
                    _suppressNextLButtonUp = true;
                    CompleteSwapMode(ms.pt);
                    return (IntPtr)1;
                }
                if (msg == WM_LBUTTONUP || msg == WM_RBUTTONUP || msg == WM_MBUTTONUP)
                {
                    return (IntPtr)1;
                }
                if (msg == WM_RBUTTONDOWN) _suppressNextRButtonUp = true;
                else if (msg == WM_MBUTTONDOWN) _suppressNextMButtonUp = true;

                CancelSwapMode();
                return (IntPtr)1;
            }
            else if (_windowSetPhase == WindowSetPhase.Registering)
            {
                if (msg == WM_LBUTTONDOWN)
                {
                    _suppressNextLButtonUp = true;
                    ToggleWindowSetSelectionAt(ms.pt);
                    return (IntPtr)1;
                }
                if (msg == WM_LBUTTONUP || msg == WM_RBUTTONUP || msg == WM_MBUTTONUP)
                {
                    return (IntPtr)1;
                }
                if (msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN)
                {
                    if (msg == WM_RBUTTONDOWN) _suppressNextRButtonUp = true;
                    else _suppressNextMButtonUp = true;
                    CancelWindowSetMode(showCancelToast: true);
                    return (IntPtr)1;
                }
            }
            else if (_windowSetPhase == WindowSetPhase.Menu || _windowSetPhase == WindowSetPhase.Deleting)
            {
                if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN)
                {
                    if (msg == WM_LBUTTONDOWN && TrayIcon.IsPointInHelpPopup(ms.pt.X, ms.pt.Y))
                        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);

                    if (msg == WM_LBUTTONDOWN) _suppressNextLButtonUp = true;
                    else if (msg == WM_RBUTTONDOWN) _suppressNextRButtonUp = true;
                    else if (msg == WM_MBUTTONDOWN) _suppressNextMButtonUp = true;

                    CancelWindowSetMode();
                    return (IntPtr)1;
                }
            }
            else if (_isWaitingForNumpad)
            {
                // ヘルプポップアップ内のマウス速度コントロール（- / スライダー / +）はフックから直接処理
                if ((msg == WM_LBUTTONDOWN || msg == WM_LBUTTONUP)
                    && TrayIcon.TryHandleHelpPopupMouse(msg, ms.pt.X, ms.pt.Y))
                {
                    return (IntPtr)1;
                }

                // NumPad 待ち受け中にクリックされたらキャンセル（ヘルプポップアップ上の左クリックは項目実行のため通過）
                if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN)
                {
                    if (msg == WM_LBUTTONDOWN && TrayIcon.IsPointInHelpPopup(ms.pt.X, ms.pt.Y))
                        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);

                    if (msg == WM_LBUTTONDOWN) _suppressNextLButtonUp = true;
                    else if (msg == WM_RBUTTONDOWN) _suppressNextRButtonUp = true;
                    else if (msg == WM_MBUTTONDOWN) _suppressNextMButtonUp = true;

                    CancelNumpadWaiting();
                    return (IntPtr)1;
                }
            }
            else if (_resizePhase != ResizePhase.None)
            {
                if (msg == WM_LBUTTONDOWN)
                {
                    if (_resizePhase == ResizePhase.TopLeft)
                    {
                        // 左上座標確定 → マウスをBOX右下へ移動
                        _resizeTopLeft = ms.pt;
                        _resizePhase = ResizePhase.BottomRight;
                        SetCursorPos(_resizeBottomRight.X, _resizeBottomRight.Y);
                        UpdateOverlay();
                        return (IntPtr)1;
                    }
                    else if (_resizePhase == ResizePhase.BottomRight)
                    {
                        // 右下座標確定 → ウィンドウをリサイズ
                        _resizeBottomRight = ms.pt;
                        ApplyResize();
                        CancelResizeMode();
                        return (IntPtr)1;
                    }
                }
                else if (msg == WM_LBUTTONUP)
                {
                    // LBUTTONUPはLBUTTONDOWNと対になるので抑制しない
                }
                else
                {
                    // 左クリック以外のマウスイベント（右クリック・ホイール等）→ モード終了
                    CancelResizeMode();
                }
            }
            else if (_isMoveMode)
            {
                if (msg == WM_LBUTTONDOWN)
                {
                    // マウスクリックにて移動を決定
                    CommitMoveMode();
                    _suppressNextLButtonUp = true;
                    return (IntPtr)1;
                }
                else if (msg == WM_LBUTTONUP)
                {
                    return (IntPtr)1;
                }
                else
                {
                    // 他の入力（右クリック、中クリック、ホイール等）があった場合は、移動前の位置に戻す
                    if (msg == WM_RBUTTONDOWN) _suppressNextRButtonUp = true;
                    else if (msg == WM_MBUTTONDOWN) _suppressNextMButtonUp = true;

                    CancelMoveMode();
                    return (IntPtr)1;
                }
            }
            else if (msg == WM_MOUSEWHEEL && _stackTabForms.Count > 0)
            {
                short delta = (short)((ms.mouseData >> 16) & 0xFFFF);
                if (delta != 0 && TryHandleStackTabWheel(ms.pt, delta))
                    return (IntPtr)1;
            }
            else if (_stackTabForms.Count > 0
                     && (msg == WM_LBUTTONDOWN || msg == WM_LBUTTONUP))
            {
                HandleStackDropMouseEvent(msg, ms.pt);
            }
        }

        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
    }

    private static void UpdateOverlay()
    {
        if (_overlay == null) return;
        var ov = _overlay;
        var tl = _resizeTopLeft;
        var br = _resizeBottomRight;
        if (!ov.IsDisposed && ov.IsHandleCreated)
        {
            if (ov.InvokeRequired)
                ov.BeginInvoke(() => ov.UpdateRect(tl.X, tl.Y, br.X, br.Y));
            else
                ov.UpdateRect(tl.X, tl.Y, br.X, br.Y);
        }
    }

    private static void ApplyResize()
    {
        if (_resizeTarget == IntPtr.Zero) return;

        int left   = Math.Min(_resizeTopLeft.X, _resizeBottomRight.X);
        int top    = Math.Min(_resizeTopLeft.Y, _resizeBottomRight.Y);
        int right  = Math.Max(_resizeTopLeft.X, _resizeBottomRight.X);
        int bottom = Math.Max(_resizeTopLeft.Y, _resizeBottomRight.Y);
        int width  = right - left;
        int height = bottom - top;

        if (width > 0 && height > 0)
            MoveWindow(_resizeTarget, left, top, width, height, true);
    }

    // -----------------------------------------------------------------------
    // Window Fit (空きエリアの最大矩形へ移動)
    // -----------------------------------------------------------------------

    /// <summary>
    /// 対象ウィンドウを、現在のモニタの作業領域内で最も面積が大きくなる
    /// 空き矩形へ移動・リサイズする。outer_spacing / inner_spacing を考慮する。
    /// </summary>
    private static void FitWindowToLargestFreeArea(IntPtr target)
    {
        if (target == IntPtr.Zero) return;

        IntPtr root = GetAncestor(target, GA_ROOT);
        if (root == IntPtr.Zero) root = target;
        if (IsSystemWindow(root)) return;

        // 対象ウィンドウが存在するモニタの作業領域（タスクバーを除く）
        System.Drawing.Rectangle wa = Screen.FromHandle(root).WorkingArea;

        int[] outer = AppConfig.OuterSpacing; // 上, 右, 下, 左
        int inner = AppConfig.InnerSpacing;

        // outer_spacing を適用した利用可能領域
        int areaLeft   = wa.Left + outer[3];
        int areaTop    = wa.Top + outer[0];
        int areaRight  = wa.Right - outer[1];
        int areaBottom = wa.Bottom - outer[2];
        if (areaRight <= areaLeft || areaBottom <= areaTop) return;

        // 他の可視ウィンドウを inner_spacing 分拡張して障害物として収集
        var obstacles = new System.Collections.Generic.List<System.Drawing.Rectangle>();
        EnumWindows((hWnd, lParam) =>
        {
            if (hWnd == root) return true;
            if (!IsWindowVisible(hWnd) || IsIconic(hWnd)) return true;
            if (IsIgnoredWindowForFit(hWnd)) return true;
            if (!GetWindowRect(hWnd, out RECT r)) return true;
            if (r.Right <= r.Left || r.Bottom <= r.Top) return true;

            // 利用可能領域と交差しないものは無視
            if (r.Right <= areaLeft || r.Left >= areaRight ||
                r.Bottom <= areaTop || r.Top >= areaBottom) return true;

            obstacles.Add(System.Drawing.Rectangle.FromLTRB(
                r.Left - inner, r.Top - inner, r.Right + inner, r.Bottom + inner));
            return true;
        }, IntPtr.Zero);

        // 座標圧縮用の x 候補座標（y は障害物の区間から直接求める）
        var xs = new System.Collections.Generic.SortedSet<int> { areaLeft, areaRight };
        foreach (var o in obstacles)
        {
            xs.Add(Math.Clamp(o.Left, areaLeft, areaRight));
            xs.Add(Math.Clamp(o.Right, areaLeft, areaRight));
        }
        int[] xArr = new System.Collections.Generic.List<int>(xs).ToArray();

        int bestArea = 0;
        int bestLeft = 0, bestTop = 0, bestRight = 0, bestBottom = 0;

        // 左右の x 候補の全組み合わせについて、最大の空き y 区間を求める
        for (int xi = 0; xi < xArr.Length - 1; xi++)
        {
            int x1 = xArr[xi];
            for (int xj = xi + 1; xj < xArr.Length; xj++)
            {
                int x2 = xArr[xj];
                if (x2 <= x1) continue;

                // この x 範囲と水平に重なる障害物の y 区間
                var blocked = new System.Collections.Generic.List<(int Top, int Bottom)>();
                foreach (var o in obstacles)
                {
                    if (o.Left < x2 && o.Right > x1)
                    {
                        int bt = Math.Max(o.Top, areaTop);
                        int bb = Math.Min(o.Bottom, areaBottom);
                        if (bt < bb) blocked.Add((bt, bb));
                    }
                }
                blocked.Sort((a, b) => a.Top.CompareTo(b.Top));

                int cursorY = areaTop;
                foreach (var b in blocked)
                {
                    if (b.Top > cursorY)
                        ConsiderRect(x1, cursorY, x2, b.Top,
                            ref bestArea, ref bestLeft, ref bestTop, ref bestRight, ref bestBottom);
                    if (b.Bottom > cursorY) cursorY = b.Bottom;
                }
                if (areaBottom > cursorY)
                    ConsiderRect(x1, cursorY, x2, areaBottom,
                        ref bestArea, ref bestLeft, ref bestTop, ref bestRight, ref bestBottom);
            }
        }

        if (bestArea <= 0) return;

        int width  = bestRight - bestLeft;
        int height = bestBottom - bestTop;
        if (width <= 0 || height <= 0) return;

        // 最大化されている場合は復元してから移動する
        if (IsZoomed(root))
            ShowWindow(root, SW_RESTORE);

        MoveWindow(root, bestLeft, bestTop, width, height, true);
    }

    /// <summary>
    /// Window Fit の障害物として扱わないウィンドウかどうかを判定する。
    /// デスクトップ・タスクバー・シェル系の全画面ウィンドウ・ツールウィンドウ・
    /// DWM でクロークされているウィンドウを除外する。
    /// </summary>
    private static bool IsIgnoredWindowForFit(IntPtr hWnd)
    {
        if (IsSystemWindow(hWnd)) return true;
        if ((GetWindowLong(hWnd, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) != 0) return true;

        var sb = new System.Text.StringBuilder(256);
        if (GetClassName(hWnd, sb, sb.Capacity) > 0
            && _fitIgnoredClasses.Contains(sb.ToString()))
        {
            return true;
        }

        // DWM でクローク（非表示扱い）されているウィンドウは除外
        if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0
            && cloaked != 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>Window Fit で障害物から除外するシェル系ウィンドウのクラス名。</summary>
    private static readonly System.Collections.Generic.HashSet<string> _fitIgnoredClasses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Progman",
            "WorkerW",
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            "Windows.UI.Core.CoreWindow",
            "EdgeUiInputTopWndClass",
            "CursorVisualClass",
            "Tao Thread Event Target",
            "WinUIDesktopWin32WindowClass",
        };

    private static void ConsiderRect(int left, int top, int right, int bottom,
        ref int bestArea, ref int bestLeft, ref int bestTop,
        ref int bestRight, ref int bestBottom)
    {
        int w = right - left;
        int h = bottom - top;
        if (w <= 0 || h <= 0) return;

        int area = w * h;
        if (area > bestArea)
        {
            bestArea = area;
            bestLeft = left;
            bestTop = top;
            bestRight = right;
            bestBottom = bottom;
        }
    }

    /// <summary>
    /// 対象ウィンドウの最前面（TopMost）固定・解除をトグルする。
    /// トグル後の最前面状態（true: 最前面固定, false: 解除）を返す。
    /// </summary>
    private static bool ToggleTopmost(IntPtr target)
    {
        if (target == IntPtr.Zero || IsSystemWindow(target)) return false;

        bool currentlyTopmost = _dimTarget == target
            ? _dimTargetWasTopMost
            : (GetWindowLong(target, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0;

        bool makeTopmost = !currentlyTopmost;
        if (_dimTarget == target)
            _dimTargetWasTopMost = makeTopmost;

        SetWindowPos(target, makeTopmost ? HWND_TOPMOST : HWND_NOTOPMOST,
            0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

        return makeTopmost;
    }

    /// <summary>
    /// 対象ウィンドウの Z-order を最下位（最背面）へ移動する。
    /// 最前面（TopMost）属性があれば解除した上で最背面に配置する。
    /// </summary>
    private static void SendToBottom(IntPtr target)
    {
        if (target == IntPtr.Zero || IsSystemWindow(target)) return;

        // TopMost 属性があれば解除してから最背面へ配置
        if ((GetWindowLong(target, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0)
        {
            SetWindowPos(target, HWND_NOTOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        SetWindowPos(target, HWND_BOTTOM, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    // メインロジック -------------------------------------------------------

    /// <summary>
    /// NumPad 待ち受けを起動するキーかどうかを判定する。
    /// config.toml の [hotkey] hotkeys に列挙されたキー
    /// （既定は Pause と Break）のいずれかであれば true を返す。
    /// </summary>
    private static bool IsPauseOrBreakKey(Keys key)
    {
        return Array.IndexOf(AppConfig.PauseKeys, key) >= 0;
    }

    private static void StartNumpadWaiting()
    {
        if (GetCursorPos(out POINT cursorPosition))
        {
            IntPtr window = WindowFromPoint(cursorPosition);
            if (window != IntPtr.Zero && IsSystemWindow(window))
            {
                activeWindow = IntPtr.Zero;
            }
            else
            {
                activeWindow = window == IntPtr.Zero ? IntPtr.Zero : GetAncestor(window, GA_ROOT);
                if (activeWindow == IntPtr.Zero)
                {
                    IntPtr fg = GetForegroundWindow();
                    if (fg != IntPtr.Zero)
                        activeWindow = GetAncestor(fg, GA_ROOT);
                }

                // デスクトップ・タスクバーは対象外
                if (IsSystemWindow(activeWindow))
                    activeWindow = IntPtr.Zero;
            }

            // デスクトップ全体を黒半透明で覆い、対象ウィンドウをその上に表示
            ShowDimOverlay(activeWindow);

            ClipActiveWindow();
        }

        // 対象windowが最前面に表示された後、次のキー入力待ちの直前にヘルプを表示（次のキー入力まで表示したままにするため autoClose: false）
        // （辺拡張の選択待ちへ移行するために閉じられた場合は NumPad 待ちを維持する）
        TrayIcon.ShowHelpPopup(autoClose: false, onClosed: () => { if (!_edgeSelecting) CancelNumpadWaiting(); });

        _isWaitingForNumpad = true;
        ClipActiveWindow();
    }

    private static void CancelNumpadWaiting()
    {
        if (!_isWaitingForNumpad) return;
        _isWaitingForNumpad = false;
        ClipCursor(IntPtr.Zero);
        _edgeSelecting = false;
        activeWindow = IntPtr.Zero;
        CloseDimOverlay();
        TrayIcon.CloseHelpPopup();
        ResetMouseSpeedState();
    }

    // -----------------------------------------------------------------------
    // Dim Overlay (デスクトップ全体の黒半透明表示)
    // -----------------------------------------------------------------------

    /// <summary>
    /// デスクトップ全体を覆う黒半透明ウィンドウを表示し、
    /// 対象ウィンドウをその上（最前面）に表示する。
    /// </summary>
    private static void ShowDimOverlay(IntPtr target)
    {
        CloseDimOverlay();

        _dimOverlay = new DimOverlayForm();
        _dimOverlay.Show();
        _dimOverlay.BringToTopMost();

        if (target != IntPtr.Zero)
        {
            _dimTarget = target;
            _dimTargetWasTopMost =
                (GetWindowLong(target, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0;

            // 対象ウィンドウを最前面へ（黒半透明ウィンドウより手前に表示）
            SetWindowPos(target, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
    }

    /// <summary>
    /// 黒半透明ウィンドウを削除し、対象ウィンドウの最前面状態を元に戻す。
    /// </summary>
    private static void CloseDimOverlay()
    {
        if (_dimTarget != IntPtr.Zero)
        {
            // 元々最前面でなかった場合のみ元に戻す
            if (!_dimTargetWasTopMost)
            {
                SetWindowPos(_dimTarget, HWND_NOTOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            _dimTarget = IntPtr.Zero;
            _dimTargetWasTopMost = false;
        }

        if (_dimOverlay != null)
        {
            var ov = _dimOverlay;
            _dimOverlay = null;
            try { ov.Hide(); } catch { }
            if (!ov.IsDisposed && ov.IsHandleCreated)
                ov.BeginInvoke(() => ov.Close());
            else
                ov.Close();
        }

        Application.OpenForms.OfType<DimOverlayForm>().ToList().ForEach(f =>
        {
            try { f.Hide(); } catch { }
            if (!f.IsDisposed) f.Close();
        });
    }

    private static void Timer_Tick(object? sender, EventArgs e)
    {
        if (_isWaitingForNumpad)
            ClipActiveWindow();
    }

    private static void ClipActiveWindow()
    {
        if (activeWindow == IntPtr.Zero) return;
        if (!GetWindowRect(activeWindow, out RECT rect)
            || rect.Right <= rect.Left || rect.Bottom <= rect.Top)
        {
            // 対象ウィンドウが消失した場合は待機状態を終了し、黒半透明ウィンドウも削除
            CancelNumpadWaiting();
            return;
        }

        if (TrayIcon.GetHelpPopupBounds() is Rectangle popup)
        {
            rect.Left = Math.Min(rect.Left, popup.Left);
            rect.Top = Math.Min(rect.Top, popup.Top);
            rect.Right = Math.Max(rect.Right, popup.Right);
            rect.Bottom = Math.Max(rect.Bottom, popup.Bottom);
        }

        ClipCursor(ref rect);
    }

    private static void ExecuteHelpCommand(TrayIcon.HelpCommand cmd)
    {
        IntPtr target = activeWindow;
        if (target == IntPtr.Zero)
        {
            IntPtr fg = GetForegroundWindow();
            if (fg != IntPtr.Zero)
                target = GetAncestor(fg, GA_ROOT);
        }
        if (target != IntPtr.Zero && IsSystemWindow(target))
            target = IntPtr.Zero;

        void EndWaitingOrClosePopup()
        {
            if (_isWaitingForNumpad)
                CancelNumpadWaiting();
            else
                TrayIcon.CloseHelpPopup();
        }

        switch (cmd)
        {
            case TrayIcon.HelpCommand.Move:
                EndWaitingOrClosePopup();
                if (target != IntPtr.Zero)
                    TryStartMoveMode(target);
                break;

            case TrayIcon.HelpCommand.Resize:
                EndWaitingOrClosePopup();
                if (target != IntPtr.Zero)
                    TryStartResizeMode(target);
                break;

            case TrayIcon.HelpCommand.Edge:
                if (target == IntPtr.Zero)
                {
                    EndWaitingOrClosePopup();
                    break;
                }
                if (!_isWaitingForNumpad)
                {
                    activeWindow = target;
                    _isWaitingForNumpad = true;
                }
                StartEdgeSelecting();
                break;

            case TrayIcon.HelpCommand.Fit:
                EndWaitingOrClosePopup();
                if (target != IntPtr.Zero)
                    FitWindowToLargestFreeArea(target);
                break;

            case TrayIcon.HelpCommand.Topmost:
                if (target != IntPtr.Zero)
                {
                    bool isTopmost = ToggleTopmost(target);
                    EndWaitingOrClosePopup();
                    TrayIcon.ShowMessage(isTopmost ? "最前面に固定しました (TopMost: ON)" : "最前面固定を解除しました (TopMost: OFF)", 1500);
                }
                else
                {
                    EndWaitingOrClosePopup();
                }
                break;

            case TrayIcon.HelpCommand.Bottom:
                if (target != IntPtr.Zero)
                {
                    EndWaitingOrClosePopup();
                    SendToBottom(target);
                    TrayIcon.ShowMessage("最背面に移動しました (Send to Bottom)", 1500);
                }
                else
                {
                    EndWaitingOrClosePopup();
                }
                break;

            case TrayIcon.HelpCommand.Minimize:
                if (target != IntPtr.Zero)
                {
                    EndWaitingOrClosePopup();
                    ShowWindow(target, SW_MINIMIZE);
                    TrayIcon.ShowMessage("最小化しました (Minimize)", 1500);
                }
                else
                {
                    EndWaitingOrClosePopup();
                }
                break;

            case TrayIcon.HelpCommand.Maximize:
                if (target != IntPtr.Zero)
                {
                    EndWaitingOrClosePopup();
                    bool wasZoomed = IsZoomed(target);
                    ShowWindow(target, wasZoomed ? SW_RESTORE : SW_MAXIMIZE);
                    TrayIcon.ShowMessage(wasZoomed ? "元のサイズに戻しました (Restore)" : "最大化しました (Maximize)", 1500);
                }
                else
                {
                    EndWaitingOrClosePopup();
                }
                break;

            case TrayIcon.HelpCommand.Close:
                if (target != IntPtr.Zero)
                {
                    EndWaitingOrClosePopup();
                    PostMessage(target, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    TrayIcon.ShowMessage("ウィンドウを閉じました (Close)", 1500);
                }
                else
                {
                    EndWaitingOrClosePopup();
                }
                break;

            case TrayIcon.HelpCommand.Snap:
                if (target != IntPtr.Zero)
                {
                    EndWaitingOrClosePopup();
                    TryStartSnapMode(target);
                }
                else
                {
                    EndWaitingOrClosePopup();
                }
                break;

            case TrayIcon.HelpCommand.Swap:
                {
                    IntPtr swapTarget = _isWaitingForNumpad ? activeWindow : target;
                    EndWaitingOrClosePopup();
                    if (swapTarget != IntPtr.Zero)
                        TryStartSwapMode(swapTarget);
                }
                break;

            case TrayIcon.HelpCommand.Stack:
                {
                    IntPtr stackTarget = _isWaitingForNumpad ? activeWindow : target;
                    EndWaitingOrClosePopup();
                    if (stackTarget != IntPtr.Zero)
                        TryStartStackMode(stackTarget);
                }
                break;

            case TrayIcon.HelpCommand.WindowSet:
                EndWaitingOrClosePopup();
                StartWindowSetMenuMode();
                break;
        }
    }

    private static void MoveCursorToWindowPosition(Keys key)
    {
        if (activeWindow == IntPtr.Zero || !GetWindowRect(activeWindow, out RECT rect)
            || rect.Right <= rect.Left || rect.Bottom <= rect.Top)
        {
            return;
        }

        int left = Math.Clamp(rect.Left + 10, rect.Left, rect.Right - 1);
        int right = Math.Clamp(rect.Right - 10, rect.Left, rect.Right - 1);
        int top = Math.Clamp(rect.Top + 10, rect.Top, rect.Bottom - 1);
        int bottom = Math.Clamp(rect.Bottom - 10, rect.Top, rect.Bottom - 1);
        int centerX = left + (right - left) / 2;
        int centerY = top + (bottom - top) / 2;

        POINT position = key switch
        {
            Keys.NumPad7 => new POINT { X = left, Y = top },
            Keys.NumPad8 => new POINT { X = centerX, Y = top },
            Keys.NumPad9 => new POINT { X = right, Y = top },
            Keys.NumPad4 => new POINT { X = left, Y = centerY },
            Keys.NumPad5 => new POINT { X = centerX, Y = centerY },
            Keys.NumPad6 => new POINT { X = right, Y = centerY },
            Keys.NumPad1 => new POINT { X = left, Y = bottom },
            Keys.NumPad2 => new POINT { X = centerX, Y = bottom },
            Keys.NumPad3 => new POINT { X = right, Y = bottom },
            _ => default
        };

        SetCursorPos(position.X, position.Y);
    }

    private static bool TryGetNumpadKey(int vkCode, IntPtr lParam, out Keys key)
    {
        int scanCode = Marshal.ReadInt32(lParam, sizeof(int));
        int flags = Marshal.ReadInt32(lParam, sizeof(int) * 2);
        key = (flags & LLKHF_EXTENDED) == 0
            ? scanCode switch
            {
                0x47 => Keys.NumPad7,
                0x48 => Keys.NumPad8,
                0x49 => Keys.NumPad9,
                0x4B => Keys.NumPad4,
                0x4C => Keys.NumPad5,
                0x4D => Keys.NumPad6,
                0x4F => Keys.NumPad1,
                0x50 => Keys.NumPad2,
                0x51 => Keys.NumPad3,
                _ => Keys.None
            }
            : (Keys)vkCode;

        return key is Keys.NumPad1 or Keys.NumPad2 or Keys.NumPad3
            or Keys.NumPad4 or Keys.NumPad5 or Keys.NumPad6
            or Keys.NumPad7 or Keys.NumPad8 or Keys.NumPad9;
    }

    private static double Dist(POINT a, POINT b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    // -----------------------------------------------------------------------
    // Window Snap Mode
    // -----------------------------------------------------------------------

    /// <summary>
    /// スナップ選択 UI を表示する。
    /// </summary>
    private static void TryStartSnapMode(IntPtr target)
    {
        if (target == IntPtr.Zero || IsSystemWindow(target)) return;

        IntPtr root = GetAncestor(target, GA_ROOT);
        if (root == IntPtr.Zero) root = target;
        if (IsSystemWindow(root)) return;
        if (!GetWindowRect(root, out RECT _)) return;

        _snapTarget = root;

        CloseSnapForm();
        _snapForm = new WindowSnapForm(root, OnSnapSelected, OnSnapCancelled);
        _snapForm.Show();
    }

    /// <summary>
    /// スナップ選択 UI を閉じる（キャンセルも兼ねる）。
    /// </summary>
    private static void CloseSnapForm()
    {
        if (_snapForm != null)
        {
            var sf = _snapForm;
            _snapForm = null;
            if (sf.IsHandleCreated)
                sf.BeginInvoke(() => sf.Close());
            else
                sf.Close();
        }
        _snapTarget = IntPtr.Zero;
        ResetMouseSpeedState();
    }

    /// <summary>
    /// スナップ選択が確定したときのコールバック。
    /// cols/rows: グリッドの列数/行数, col/row: 選択セルの左上インデックス（0始まり）
    /// </summary>
    private static void OnSnapSelected(IntPtr target, int cols, int rows, int col, int row)
    {
        CloseSnapForm();

        if (target == IntPtr.Zero || IsSystemWindow(target)) return;

        // 対象ウィンドウが存在するモニタの作業領域
        System.Drawing.Rectangle wa = Screen.FromHandle(target).WorkingArea;

        int[] outer = AppConfig.OuterSpacing; // 上, 右, 下, 左
        int inner   = AppConfig.InnerSpacing;

        // outer_spacing を適用した配置可能領域
        int areaLeft   = wa.Left   + outer[3];
        int areaTop    = wa.Top    + outer[0];
        int areaRight  = wa.Right  - outer[1];
        int areaBottom = wa.Bottom - outer[2];

        int totalW = areaRight  - areaLeft;
        int totalH = areaBottom - areaTop;

        if (totalW <= 0 || totalH <= 0) return;

        // inner_spacing をセル間に配分（左右に (cols-1) 個, 上下に (rows-1) 個）
        int cellW = (totalW - inner * (cols - 1)) / cols;
        int cellH = (totalH - inner * (rows - 1)) / rows;

        if (cellW <= 0 || cellH <= 0) return;

        int winLeft   = areaLeft + col * (cellW + inner);
        int winTop    = areaTop  + row * (cellH + inner);
        int winWidth  = cellW;
        int winHeight = cellH;

        // 最大化されている場合は復元してからスナップ
        if (IsZoomed(target))
            ShowWindow(target, SW_RESTORE);

        MoveWindow(target, winLeft, winTop, winWidth, winHeight, true);
        SetForegroundWindow(target);
    }

    /// <summary>
    /// スナップ選択がキャンセルされたときのコールバック。
    /// </summary>
    private static void OnSnapCancelled()
    {
        CloseSnapForm();
    }

    // -----------------------------------------------------------------------
    // Window Set Mode (複数アプリの登録・一覧表示・一括起動＆復元)
    // -----------------------------------------------------------------------

    private static void StartWindowSetMenuMode(bool fromKeyboard = false)
    {
        if (_windowSetPhase != WindowSetPhase.None || _isMoveMode || _resizePhase != ResizePhase.None || _isEdgeMode || _isSwapMode)
            return;

        _windowSetTriggerReleased = !fromKeyboard;
        _windowSetPhase = WindowSetPhase.Menu;
        TrayIcon.ShowWindowSetPopup(
            AppConfig.WindowSets,
            AppConfig.WindowSetKey,
            onRegister: () => StartWindowSetRegisterMode(fromKeyboard: false),
            onDeleteMode: StartWindowSetDeleteMode,
            onLaunch: ExecuteWindowSet,
            onDeleteKey: DeleteWindowSet,
            onClosed: () =>
            {
                if (_windowSetPhase == WindowSetPhase.Menu)
                    CancelWindowSetMode();
            });
    }

    private static void StartWindowSetDeleteMode()
    {
        if (_windowSetPhase == WindowSetPhase.Deleting) return;

        if (AppConfig.WindowSets.Count == 0)
        {
            _windowSetPhase = WindowSetPhase.None;
            TrayIcon.CloseHelpPopup();
            ResetMouseSpeedState();
            TrayIcon.ShowMessage("削除できる Window Set はありません", 1500);
            return;
        }

        _windowSetPhase = WindowSetPhase.Deleting;
        TrayIcon.ShowWindowSetDeletePopup(
            AppConfig.WindowSets,
            onDeleteKey: DeleteWindowSet,
            onClosed: () =>
            {
                if (_windowSetPhase == WindowSetPhase.Deleting)
                    CancelWindowSetMode();
            });
    }

    private static void DeleteWindowSet(Keys recallKey)
    {
        if (_windowSetPhase != WindowSetPhase.Menu && _windowSetPhase != WindowSetPhase.Deleting)
            return;

        _windowSetPhase = WindowSetPhase.None;
        TrayIcon.CloseHelpPopup();
        ResetMouseSpeedState();

        string keyName = AppConfig.KeysToWindowSetName(recallKey);
        if (!AppConfig.WindowSets.ContainsKey(recallKey))
        {
            TrayIcon.ShowMessage($"Window Set [{keyName}] は未登録です", 1500);
            return;
        }

        bool deleted = AppConfig.DeleteWindowSet(recallKey);
        if (deleted)
            TrayIcon.ShowMessage($"Window Set [{keyName}] を削除しました", 1800);
        else
            TrayIcon.ShowMessage($"Window Set [{keyName}] の削除に失敗しました", 2000);
    }

    private static void StartWindowSetRegisterMode(bool fromKeyboard = false)
    {
        if (_windowSetPhase == WindowSetPhase.Registering) return;

        ClearWindowSetOverlays();
        _windowSetTriggerReleased = !fromKeyboard;
        _windowSetPhase = WindowSetPhase.Registering;
        ApplySwapCursor();
        UpdateWindowSetRegisterStatus();
    }

    private static void ToggleWindowSetSelectionAt(POINT pt)
    {
        if (_windowSetPhase != WindowSetPhase.Registering) return;

        IntPtr target = FindTopmostSelectableWindowAt(pt);
        if (target == IntPtr.Zero)
            return;

        // すでに選択済みなら解除（トグル）
        int existingIdx = _windowSetSelections.FindIndex(x => x.HWnd == target);
        if (existingIdx >= 0)
        {
            var ov = _windowSetSelections[existingIdx].Overlay;
            _windowSetSelections.RemoveAt(existingIdx);
            CloseSingleOverlay(ov);
            UpdateWindowSetRegisterStatus();
            return;
        }

        if (!GetWindowRect(target, out RECT rect)) return;
        if (rect.Right <= rect.Left || rect.Bottom <= rect.Top) return;

        var overlay = new OverlayForm(fillColor: WindowSetManager.GetAccentColor());
        overlay.UpdateRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        overlay.Show();
        overlay.BringToTopMost();

        _windowSetSelections.Add((target, overlay));
        UpdateWindowSetRegisterStatus();
    }

    /// <summary>
    /// 指定スクリーン座標で最前面にある選択可能なトップレベルウィンドウを Z-order 順に探索する。
    /// 自アプリのオーバーレイやポップアップは無視し、最前面がタスクバーやデスクトップなら IntPtr.Zero を返す。
    /// </summary>
    private static IntPtr FindTopmostSelectableWindowAt(POINT pt)
    {
        IntPtr found = IntPtr.Zero;

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd) || IsIconic(hWnd))
                return true;
            if (IsOwnProcessWindow(hWnd))
                return true;

            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            // 他アプリのクリック透過オーバーレイは素通り
            if ((exStyle & (WS_EX_LAYERED | WS_EX_TRANSPARENT)) == (WS_EX_LAYERED | WS_EX_TRANSPARENT))
                return true;

            if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0
                && cloaked != 0)
                return true;

            if (!GetWindowRect(hWnd, out RECT rect))
                return true;
            if (rect.Right <= rect.Left || rect.Bottom <= rect.Top)
                return true;

            if (pt.X < rect.Left || pt.X >= rect.Right || pt.Y < rect.Top || pt.Y >= rect.Bottom)
                return true;

            // タスクバーやデスクトップ背景に当たった場合はそこで探索終了
            if (IsSystemWindow(hWnd))
                return false;

            if ((exStyle & WS_EX_TOOLWINDOW) != 0)
                return true;

            found = hWnd;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    private static void UpdateWindowSetRegisterStatus(string? hintOverride = null)
    {
        if (_windowSetPhase != WindowSetPhase.Registering) return;

        string trigName = AppConfig.KeysToWindowSetName(AppConfig.WindowSetKey);
        int count = _windowSetSelections.Count;
        string selectedSummary = count == 0
            ? ""
            : "\n選択中: " + string.Join(", ",
                _windowSetSelections.Select(x => WindowSetManager.GetWindowAppDisplayName(x.HWnd)));

        string hint = hintOverride ?? $"'{trigName}' 以外のキー押下: 呼び出しキーとして登録 / Esc・右クリック: キャンセル";
        string text = $"Window Set 登録モード ({count} 件選択中){selectedSummary}\n左クリック: ウィンドウ選択/解除\n{hint}";

        TrayIcon.ShowMessage(text, durationMs: 0, clickThrough: true);
        TrayIcon.BringHelpPopupToTopMost();
    }

    private static void CompleteWindowSetRegistration(Keys recallKey)
    {
        if (_windowSetPhase != WindowSetPhase.Registering) return;

        var selectedHandles = _windowSetSelections.Select(x => x.HWnd).ToList();
        _windowSetPhase = WindowSetPhase.None;
        ClearWindowSetOverlays();
        TrayIcon.CloseHelpPopup();
        RestoreMoveCursor();
        ResetMouseSpeedState();

        if (selectedHandles.Count == 0)
        {
            TrayIcon.ShowMessage("ウィンドウが選択されていないためキャンセルしました", 1500);
            return;
        }

        var captured = new System.Collections.Generic.List<WindowSetApp>();
        foreach (IntPtr hWnd in selectedHandles)
        {
            var info = WindowSetManager.CaptureWindowInfo(hWnd);
            if (info != null)
                captured.Add(info);
        }

        if (captured.Count == 0)
        {
            TrayIcon.ShowMessage("アプリ情報を取得できませんでした", 1800);
            return;
        }

        string keyName = AppConfig.KeysToWindowSetName(recallKey);
        bool saved = AppConfig.SaveWindowSet(recallKey, captured);
        if (saved)
        {
            string names = string.Join(", ", captured.Select(a => a.DisplayName));
            TrayIcon.ShowMessage($"Window Set [{keyName}] を登録しました ({captured.Count} 件: {names})", 2200);
        }
        else
        {
            TrayIcon.ShowMessage($"Window Set [{keyName}] の保存に失敗しました", 2000);
        }
    }

    private static void ExecuteWindowSet(Keys recallKey)
    {
        if (_windowSetPhase != WindowSetPhase.Menu) return;

        _windowSetPhase = WindowSetPhase.None;
        TrayIcon.CloseHelpPopup();
        ResetMouseSpeedState();

        string keyName = AppConfig.KeysToWindowSetName(recallKey);
        if (!AppConfig.WindowSets.TryGetValue(recallKey, out var apps) || apps.Count == 0)
        {
            TrayIcon.ShowMessage($"Window Set [{keyName}] は未登録です", 1500);
            return;
        }

        WindowSetManager.LaunchAndRestoreAsync(recallKey, apps, ShowNotificationOnUiThread);
    }

    private static void ShowNotificationOnUiThread(string text, int durationMs)
    {
        var ctx = _uiContext;
        if (ctx != null)
            ctx.Post(_ => TrayIcon.ShowMessage(text, durationMs), null);
        else
            TrayIcon.ShowMessage(text, durationMs);
    }

    private static void CancelWindowSetMode(bool showCancelToast = false)
    {
        if (_windowSetPhase == WindowSetPhase.None) return;

        bool wasRegistering = _windowSetPhase == WindowSetPhase.Registering;
        _windowSetPhase = WindowSetPhase.None;
        ClearWindowSetOverlays();
        TrayIcon.CloseHelpPopup();
        if (wasRegistering)
            RestoreMoveCursor();
        ResetMouseSpeedState();

        if (showCancelToast)
            TrayIcon.ShowMessage("Window Set 登録をキャンセルしました", 1200);
    }

    private static void ClearWindowSetOverlays()
    {
        foreach (var (_, ov) in _windowSetSelections)
            CloseSingleOverlay(ov);
        _windowSetSelections.Clear();
    }

    private static void CloseSingleOverlay(OverlayForm ov)
    {
        try { ov.Hide(); } catch { }
        if (!ov.IsDisposed && ov.IsHandleCreated)
            ov.BeginInvoke(() => ov.Close());
        else if (!ov.IsDisposed)
            ov.Close();
    }
}

// ---------------------------------------------------------------------------
// Overlay Form: 半透明青色ボックス（Alpha-blending）
// ---------------------------------------------------------------------------

class OverlayForm : Form
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    private const uint LWA_ALPHA = 0x02;

    protected override bool ShowWithoutActivation => true;

    public OverlayForm(int left = 0, int top = 0, int width = 1, int height = 1, System.Drawing.Color? fillColor = null)
    {
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Location = new System.Drawing.Point(left, top);
        Size = new System.Drawing.Size(width, height);
        BackColor = fillColor ?? System.Drawing.Color.FromArgb(0, 120, 255);

        // クリックを透過させる（マウスフックで処理するため）
        SetStyle(ControlStyles.Selectable, false);
        SetStyle(ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
        exStyle |= WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
        SetWindowLong(Handle, GWL_EXSTYLE, exStyle);

        // Alpha-blending: 半透明（不透明度 約37%）
        SetLayeredWindowAttributes(Handle, 0, 95, LWA_ALPHA);

        BringToTopMost();
    }

    public void BringToTopMost()
    {
        if (IsHandleCreated)
        {
            SetWindowPos(Handle, HWND_TOPMOST, Left, Top, Width, Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
    }

    public void UpdateRect(int tlX, int tlY, int brX, int brY)
    {
        int left   = Math.Min(tlX, brX);
        int top    = Math.Min(tlY, brY);
        int right  = Math.Max(tlX, brX);
        int bottom = Math.Max(tlY, brY);
        int width  = Math.Max(1, right - left);
        int height = Math.Max(1, bottom - top);

        if (IsHandleCreated)
        {
            SetWindowPos(Handle, HWND_TOPMOST, left, top, width, height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
        else
        {
            Location = new System.Drawing.Point(left, top);
            Size = new System.Drawing.Size(width, height);
        }
    }

    protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
    {
        base.OnPaint(e);

        if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

        // 枠線を描画（ウィンドウ全体が半透明合成される）
        using var borderPen = new System.Drawing.Pen(System.Drawing.Color.White, 2);
        e.Graphics.DrawRectangle(borderPen, 1, 1, Math.Max(0, ClientSize.Width - 2), Math.Max(0, ClientSize.Height - 2));
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
            return cp;
        }
    }
}

// ---------------------------------------------------------------------------
// Dim Overlay Form: デスクトップ全体を覆う黒色半透明ウィンドウ
// ---------------------------------------------------------------------------

class DimOverlayForm : Form
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    private const uint LWA_ALPHA = 0x02;

    /// <summary>黒色の不透明度（0-255）。値が大きいほど暗くなる。</summary>
    private const byte DimAlpha = 140;

    protected override bool ShowWithoutActivation => true;

    public DimOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        // 全モニタ（仮想スクリーン）を覆う
        System.Drawing.Rectangle vs = SystemInformation.VirtualScreen;
        Location = new System.Drawing.Point(vs.Left, vs.Top);
        Size = new System.Drawing.Size(vs.Width, vs.Height);
        BackColor = System.Drawing.Color.Black;

        // クリックを透過させる（マウス操作は対象ウィンドウへ届く）
        SetStyle(ControlStyles.Selectable, false);
        DoubleBuffered = true;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
        exStyle |= WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
        SetWindowLong(Handle, GWL_EXSTYLE, exStyle);

        // Alpha-blending: 黒色半透明
        SetLayeredWindowAttributes(Handle, 0, DimAlpha, LWA_ALPHA);

        BringToTopMost();
    }

    public void BringToTopMost()
    {
        if (IsHandleCreated)
        {
            SetWindowPos(Handle, HWND_TOPMOST, Left, Top, Width, Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
            return cp;
        }
    }
}
