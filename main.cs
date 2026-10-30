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

    // Structs ---------------------------------------------------------------

    private struct POINT { public int X; public int Y; }
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

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
    private const uint GA_ROOT = 2;
    private const int LLKHF_EXTENDED = 0x01;

    private static IntPtr _hookID = IntPtr.Zero;
    private static IntPtr _mouseHookID = IntPtr.Zero;
    private static bool leftCtrlPressed;
    private static bool rightCtrlPressed;
    private static bool leftAltPressed;
    private static bool rightAltPressed;
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

    private const string MutexName = "Global\\cs-mouse-ctrl-single-instance";

    // Window Resize Mode ----------------------------------------------------

    private enum ResizePhase { None, TopLeft, BottomRight }
    private static ResizePhase _resizePhase = ResizePhase.None;
    private static IntPtr _resizeTarget = IntPtr.Zero;
    private static POINT _resizeTopLeft;
    private static POINT _resizeBottomRight;
    private static OverlayForm? _overlay;

    static void Main()
    {
        // 後優先: 既存インスタンスを終了させてから自身が起動する
        KillExistingInstances();

        using Mutex mutex = new Mutex(true, MutexName, out bool createdNew);
        // createdNew が false になるケースはほぼないが念のため待機
        if (!createdNew)
            mutex.WaitOne();

        _hookID = SetHook(_proc);
        _mouseHookID = SetMouseHook(_mouseProc);

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
            bool wasHotkeyPressed = IsHotkeyPressed();
            bool wasResizeHotkeyPressed = IsResizeHotkeyPressed();

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
                        break;
                    case Keys.RMenu:
                        rightAltPressed = pressed;
                        break;
                    case Keys.LWin:
                        leftWinPressed = pressed;
                        break;
                    case Keys.RWin:
                        rightWinPressed = pressed;
                        break;
                    case Keys.LShiftKey:
                        leftShiftPressed = pressed;
                        if (!pressed) _lastMousePtValid = false;
                        break;
                    case Keys.RShiftKey:
                        rightShiftPressed = pressed;
                        if (!pressed) _lastMousePtValid = false;
                        break;
                }
            }

            bool isHotkeyPressed = IsHotkeyPressed();
            bool isResizeHotkeyPressed = IsResizeHotkeyPressed();

            // 直前に消費したキーの KeyUp を抑制
            if (isKeyUp && _suppressNextKeyUp != Keys.None && (Keys)vkCode == _suppressNextKeyUp)
            {
                _suppressNextKeyUp = Keys.None;
                return (IntPtr)1;
            }

            // Pause キーの処理（NumPad待ちのトグル・キャンセル）
            if ((Keys)vkCode == Keys.Pause)
            {
                if (isKeyDown && _resizePhase == ResizePhase.None)
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
            }

            // Clip cursor hotkey (Ctrl+Alt)
            if (!wasHotkeyPressed && isHotkeyPressed && GetCursorPos(out POINT cursorPosition))
            {
                IntPtr window = WindowFromPoint(cursorPosition);
                activeWindow = window == IntPtr.Zero ? IntPtr.Zero : GetAncestor(window, GA_ROOT);
                ClipActiveWindow();
            }
            else if (wasHotkeyPressed && !isHotkeyPressed)
            {
                if (!_isWaitingForNumpad)
                {
                    ClipCursor(IntPtr.Zero);
                    activeWindow = IntPtr.Zero;
                }
            }

            if (isKeyDown && isHotkeyPressed
                && TryGetNumpadKey(vkCode, lParam, out Keys numpadKey))
            {
                MoveCursorToWindowPosition(numpadKey);
            }

            // Alt+Win+F11 → window resize mode
            // Alt+Win を先に押してから F11 を押すパターンに対応するため、
            // 修飾キーのエッジ検出ではなく「F11 が押された瞬間に Alt+Win が揃っているか」で判定する。
            if (isKeyDown && isResizeHotkeyPressed && (Keys)vkCode == Keys.F11
                && _resizePhase == ResizePhase.None)
            {
                TryStartResizeMode();
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    // -----------------------------------------------------------------------
    // Window Resize Mode
    // -----------------------------------------------------------------------

    /// <summary>
    /// Alt+Win+F11 検出: Alt (left or right) かつ Win (left or right) が押されているか。
    /// F11 キー自体はイベント側で判定するので、ここでは修飾キーのみ確認。
    /// </summary>
    private static bool IsResizeHotkeyPressed()
    {
        return (leftAltPressed || rightAltPressed) && (leftWinPressed || rightWinPressed);
    }

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

    private static void TryStartResizeMode()
    {
        // 既にリサイズモード中なら無視
        if (_resizePhase != ResizePhase.None) return;

        if (_isWaitingForNumpad)
            CancelNumpadWaiting();

        IntPtr fgWnd = GetForegroundWindow();
        if (fgWnd == IntPtr.Zero) return;
        IntPtr rootWnd = GetAncestor(fgWnd, GA_ROOT);
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

            if (msg == WM_MOUSEMOVE)
            {
                // --- 高速移動 (Resize Mode 外のみ) ---
                if (_resizePhase == ResizePhase.None)
                {
                    bool shiftDown = leftShiftPressed || rightShiftPressed;
                    bool ctrlDown  = leftCtrlPressed  || rightCtrlPressed;

                    // Ctrl+Alt は ClipCursor ホットキーなので高速移動は除外
                    bool altDown = leftAltPressed || rightAltPressed;

                    if ((shiftDown || ctrlDown) && !altDown)
                    {
                        int multiplier = ctrlDown ? 5 : 2;

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

    // メインロジック -------------------------------------------------------

    private static void StartNumpadWaiting()
    {
        _isWaitingForNumpad = true;
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
            ClipActiveWindow();
        }
    }

    private static void CancelNumpadWaiting()
    {
        _isWaitingForNumpad = false;
        if (!IsHotkeyPressed())
        {
            ClipCursor(IntPtr.Zero);
            activeWindow = IntPtr.Zero;
        }
    }

    private static void Timer_Tick(object? sender, EventArgs e)
    {
        if (IsHotkeyPressed() || _isWaitingForNumpad)
            ClipActiveWindow();
    }

    private static void ClipActiveWindow()
    {
        if (activeWindow == IntPtr.Zero) return;
        if (!GetWindowRect(activeWindow, out RECT rect)
            || rect.Right <= rect.Left || rect.Bottom <= rect.Top)
        {
            ClipCursor(IntPtr.Zero);
            activeWindow = IntPtr.Zero;
            _isWaitingForNumpad = false;
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

    private static bool IsHotkeyPressed()
    {
        return (leftCtrlPressed || rightCtrlPressed)
            && (leftAltPressed || rightAltPressed);
    }

    private static double Dist(POINT a, POINT b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
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
