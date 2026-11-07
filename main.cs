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

    // 高速移動: 直前のマウス座標（増幅計算用）
    private static POINT _lastMousePt;
    private static bool _lastMousePtValid = false;

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
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private static DimOverlayForm? _dimOverlay;
    private static IntPtr _dimTarget = IntPtr.Zero;
    private static bool _dimTargetWasTopMost;

    // Window Snap Mode -------------------------------------------------------

    private static WindowSnapForm? _snapForm;
    private static IntPtr _snapTarget = IntPtr.Zero;

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

        // システムトレイに常駐アイコンを表示
        _trayIcon = new TrayIcon(RequestRestart, RequestExit);

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
            CloseDimOverlay();
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
                        if (!pressed) _lastMousePtValid = false;
                        break;
                    case Keys.RControlKey:
                        rightCtrlPressed = pressed;
                        if (!pressed) _lastMousePtValid = false;
                        break;
                    case Keys.LMenu:
                        leftAltPressed = pressed;
                        if (!pressed)
                        {
                            _lastMousePtValid = false;
                            _altMaskSent = false;
                        }
                        break;
                    case Keys.RMenu:
                        rightAltPressed = pressed;
                        if (!pressed)
                        {
                            _lastMousePtValid = false;
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
                }
            }

            // 直前に消費したキーの KeyUp を抑制
            if (isKeyUp && _suppressNextKeyUp != Keys.None && (Keys)vkCode == _suppressNextKeyUp)
            {
                _suppressNextKeyUp = Keys.None;
                return (IntPtr)1;
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

            // Pause / Break キーの処理（NumPad待ちのトグル・キャンセル）
            if (IsPauseOrBreakKey((Keys)vkCode))
            {
                if (isKeyDown && _resizePhase == ResizePhase.None && !_isMoveMode)
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
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    // -----------------------------------------------------------------------
    // Window Move Mode
    // -----------------------------------------------------------------------

    private static void TryStartMoveMode(IntPtr preferredTarget = default)
    {
        // 既に移動モードまたはリサイズモード中なら無視
        if (_isMoveMode || _resizePhase != ResizePhase.None) return;

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
    }

    private static void CancelMoveMode()
    {
        if (!_isMoveMode) return;
        _isMoveMode = false;
        RestoreMoveCursor();
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
        if (hWnd == GetDesktopWindow()) return true;
        // タスクバー
        IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
        if (hWnd == taskbar) return true;
        // タスクバーの子ウィンドウも除外
        IntPtr secondTaskbar = FindWindow("Shell_SecondaryTrayWnd", null);
        if (hWnd == secondTaskbar) return true;
        return false;
    }

    private static void TryStartResizeMode(IntPtr preferredTarget = default)
    {
        // 既にリサイズモード中または移動モード中なら無視
        if (_resizePhase != ResizePhase.None || _isMoveMode) return;

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
                // --- 高速移動 (Resize Mode, Move Mode 外のみ) ---
                if (_resizePhase == ResizePhase.None && !_isMoveMode)
                {
                    bool altDown  = leftAltPressed  || rightAltPressed;
                    bool ctrlDown = leftCtrlPressed || rightCtrlPressed;

                    if (altDown || ctrlDown)
                    {
                        int multiplier = ctrlDown ? AppConfig.CtrlMultiplier : AppConfig.AltMultiplier;

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

                            if (dx != 0 || dy != 0)
                            {
                                int newX = ms.pt.X + dx * (multiplier - 1);
                                int newY = ms.pt.Y + dy * (multiplier - 1);

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
                        }

                        _lastMousePt = ms.pt;
                        _lastMousePtValid = true;
                        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
                    }
                    else
                    {
                        _lastMousePtValid = false;
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
    /// config.toml の [hotkey] hotkey に列挙されたキー
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

            // デスクトップ全体を黒半透明で覆い、対象ウィンドウをその上に表示
            ShowDimOverlay(activeWindow);

            ClipActiveWindow();
        }

        // 対象windowが最前面に表示された後、次のキー入力待ちの直前にヘルプを表示（次のキー入力まで表示したままにするため autoClose: false）
        TrayIcon.ShowHelpPopup(autoClose: false);

        _isWaitingForNumpad = true;
    }

    private static void CancelNumpadWaiting()
    {
        _isWaitingForNumpad = false;
        ClipCursor(IntPtr.Zero);
        activeWindow = IntPtr.Zero;
        CloseDimOverlay();
        TrayIcon.CloseHelpPopup();
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
            if (ov.IsHandleCreated)
                ov.BeginInvoke(() => ov.Close());
            else
                ov.Close();
        }
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

        ClipCursor(ref rect);
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

    public OverlayForm(int left = 0, int top = 0, int width = 1, int height = 1)
    {
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Location = new System.Drawing.Point(left, top);
        Size = new System.Drawing.Size(width, height);
        BackColor = System.Drawing.Color.FromArgb(0, 120, 255);

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
