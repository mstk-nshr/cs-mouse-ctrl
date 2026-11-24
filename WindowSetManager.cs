using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

/// <summary>
/// Window Set 機能のコア処理:
/// ・対象ウィンドウのアプリパス / working-dir / 位置 / サイズの取得
/// ・登録された複数アプリの起動とウィンドウ位置・サイズの復元
/// </summary>
static class WindowSetManager
{
    // -----------------------------------------------------------------------
    // Win32 / Native API
    // -----------------------------------------------------------------------

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute,
        out int pvAttribute, int cbAttribute);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(
        IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(
        IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle, int processInformationClass,
        ref PROCESS_BASIC_INFORMATION processInformation,
        int processInformationLength, out int returnLength);

    [DllImport("ntdll.dll", EntryPoint = "NtQueryInformationProcess")]
    private static extern int NtQueryInformationProcessPtr(
        IntPtr processHandle, int processInformationClass,
        ref IntPtr processInformation,
        int processInformationLength, out int returnLength);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        public IntPtr Reserved2_0;
        public IntPtr Reserved2_1;
        public IntPtr UniqueProcessId;
        public IntPtr Reserved3;
    }

    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    private const uint PROCESS_VM_READ = 0x0010;

    private const int ProcessBasicInformation = 0;
    private const int ProcessWow64Information = 26;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int SW_RESTORE = 9;
    private const int DWMWA_CLOAKED = 14;

    private static readonly uint _currentPid = (uint)Environment.ProcessId;

    // -----------------------------------------------------------------------
    // アクセントカラー取得
    // -----------------------------------------------------------------------

    /// <summary>Windows のシステムアクセントカラーを取得する（取得失敗時は Windows Blue）。</summary>
    public static Color GetAccentColor()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int abgr)
            {
                byte r = (byte)(abgr & 0xFF);
                byte g = (byte)((abgr >> 8) & 0xFF);
                byte b = (byte)((abgr >> 16) & 0xFF);
                return Color.FromArgb(255, r, g, b);
            }
        }
        catch { }
        return Color.FromArgb(0, 120, 215);
    }

    // -----------------------------------------------------------------------
    // ウィンドウ情報キャプチャ
    // -----------------------------------------------------------------------

    /// <summary>
    /// 指定ウィンドウからアプリパス・working-dir・位置・サイズを取得する。
    /// 取得できない場合は null を返す。
    /// </summary>
    public static WindowSetApp? CaptureWindowInfo(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
            return null;

        if (!GetWindowRect(hWnd, out RECT rect))
            return null;

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
            return null;

        uint pid = ResolveTargetProcessId(hWnd);
        if (pid == 0)
            return null;

        string? appPath = GetProcessImagePath(pid);
        if (string.IsNullOrWhiteSpace(appPath))
            return null;

        string workingDir = TryGetProcessWorkingDirectory(pid)
            ?? Path.GetDirectoryName(appPath)
            ?? "";

        return new WindowSetApp
        {
            AppPath = appPath,
            WorkingDir = workingDir,
            X = rect.Left,
            Y = rect.Top,
            Width = width,
            Height = height,
        };
    }

    /// <summary>
    /// ウィンドウの表示用アプリ名（拡張子なしの実行ファイル名）を取得する。
    /// </summary>
    public static string GetWindowAppDisplayName(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
            return "(unknown)";

        uint pid = ResolveTargetProcessId(hWnd);
        if (pid == 0)
            return "(unknown)";

        string? path = GetProcessImagePath(pid);
        if (string.IsNullOrWhiteSpace(path))
            return "(unknown)";

        string name = Path.GetFileNameWithoutExtension(path);
        return string.IsNullOrWhiteSpace(name) ? path : name;
    }

    /// <summary>
    /// 対象ウィンドウのプロセス ID を取得する。
    /// UWP アプリ等で ApplicationFrameHost.exe がホストしている場合は子ウィンドウの実プロセス ID を返す。
    /// </summary>
    private static uint ResolveTargetProcessId(IntPtr hWnd)
    {
        GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid == 0) return 0;

        string? hostPath = GetProcessImagePath(pid);
        if (!string.IsNullOrEmpty(hostPath)
            && Path.GetFileName(hostPath).Equals("ApplicationFrameHost.exe", StringComparison.OrdinalIgnoreCase))
        {
            uint childPid = 0;
            EnumChildWindows(hWnd, (child, _) =>
            {
                GetWindowThreadProcessId(child, out uint cpid);
                if (cpid != 0 && cpid != pid)
                {
                    childPid = cpid;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            if (childPid != 0)
                return childPid;
        }

        return pid;
    }

    /// <summary>QueryFullProcessImageName で実行ファイルのフルパスを取得する。</summary>
    private static string? GetProcessImagePath(uint pid)
    {
        IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (hProcess == IntPtr.Zero)
            return null;

        try
        {
            var sb = new StringBuilder(1024);
            int size = sb.Capacity;
            if (QueryFullProcessImageName(hProcess, 0, sb, ref size))
                return sb.ToString();
            return null;
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    /// <summary>
    /// リモートプロセスの PEB (RTL_USER_PROCESS_PARAMETERS.CurrentDirectory.DosPath) を読み取り、
    /// カレントディレクトリ（working-dir）を取得する。64bit / 32bit (WOW64) 両対応。
    /// </summary>
    private static string? TryGetProcessWorkingDirectory(uint pid)
    {
        IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
        if (hProcess == IntPtr.Zero)
            return null;

        try
        {
            bool isWow64 = false;
            if (Environment.Is64BitProcess)
                IsWow64Process(hProcess, out isWow64);

            string? raw = isWow64
                ? ReadCurrentDirectory32(hProcess)
                : (Environment.Is64BitProcess ? ReadCurrentDirectory64(hProcess) : ReadCurrentDirectory32Native(hProcess));

            if (string.IsNullOrWhiteSpace(raw))
                return null;

            string dir = raw.TrimEnd('\0').Trim();
            // ルートディレクトリ（例: C:\）以外は末尾のバックスラッシュを除去
            if (dir.Length > 3 && dir.EndsWith('\\'))
                dir = dir.TrimEnd('\\');

            return Directory.Exists(dir) ? dir : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    private static string? ReadCurrentDirectory64(IntPtr hProcess)
    {
        var pbi = new PROCESS_BASIC_INFORMATION();
        int status = NtQueryInformationProcess(
            hProcess, ProcessBasicInformation, ref pbi,
            Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(), out _);
        if (status != 0 || pbi.PebBaseAddress == IntPtr.Zero)
            return null;

        // PEB + 0x20 = ProcessParameters (8 bytes)
        byte[] ptrBuf = new byte[8];
        if (!ReadProcessMemory(hProcess, IntPtr.Add(pbi.PebBaseAddress, 0x20), ptrBuf, 8, out _))
            return null;

        IntPtr processParams = (IntPtr)BitConverter.ToInt64(ptrBuf, 0);
        if (processParams == IntPtr.Zero)
            return null;

        // RTL_USER_PROCESS_PARAMETERS + 0x38 = CurrentDirectory.DosPath (UNICODE_STRING: 16 bytes)
        // [0..1] Length (ushort), [2..3] MaxLength (ushort), [4..7] pad, [8..15] Buffer (IntPtr)
        byte[] usBuf = new byte[16];
        if (!ReadProcessMemory(hProcess, IntPtr.Add(processParams, 0x38), usBuf, 16, out _))
            return null;

        ushort length = BitConverter.ToUInt16(usBuf, 0);
        IntPtr strPtr = (IntPtr)BitConverter.ToInt64(usBuf, 8);
        return ReadRemoteUnicodeString(hProcess, strPtr, length);
    }

    private static string? ReadCurrentDirectory32(IntPtr hProcess)
    {
        IntPtr peb32 = IntPtr.Zero;
        int status = NtQueryInformationProcessPtr(
            hProcess, ProcessWow64Information, ref peb32, IntPtr.Size, out _);
        if (status != 0 || peb32 == IntPtr.Zero)
            return null;

        return ReadCurrentDirectoryFromPeb32(hProcess, peb32);
    }

    private static string? ReadCurrentDirectory32Native(IntPtr hProcess)
    {
        var pbi = new PROCESS_BASIC_INFORMATION();
        int status = NtQueryInformationProcess(
            hProcess, ProcessBasicInformation, ref pbi,
            Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(), out _);
        if (status != 0 || pbi.PebBaseAddress == IntPtr.Zero)
            return null;

        return ReadCurrentDirectoryFromPeb32(hProcess, pbi.PebBaseAddress);
    }

    private static string? ReadCurrentDirectoryFromPeb32(IntPtr hProcess, IntPtr peb32)
    {
        // PEB32 + 0x10 = ProcessParameters (4 bytes)
        byte[] ptrBuf = new byte[4];
        if (!ReadProcessMemory(hProcess, IntPtr.Add(peb32, 0x10), ptrBuf, 4, out _))
            return null;

        uint processParams = BitConverter.ToUInt32(ptrBuf, 0);
        if (processParams == 0)
            return null;

        // RTL_USER_PROCESS_PARAMETERS32 + 0x24 = CurrentDirectory.DosPath (UNICODE_STRING32: 8 bytes)
        // [0..1] Length (ushort), [2..3] MaxLength (ushort), [4..7] Buffer (uint)
        byte[] usBuf = new byte[8];
        if (!ReadProcessMemory(hProcess, new IntPtr(unchecked((long)processParams + 0x24)), usBuf, 8, out _))
            return null;

        ushort length = BitConverter.ToUInt16(usBuf, 0);
        uint strAddr = BitConverter.ToUInt32(usBuf, 4);
        return ReadRemoteUnicodeString(hProcess, new IntPtr(unchecked((long)strAddr)), length);
    }

    private static string? ReadRemoteUnicodeString(IntPtr hProcess, IntPtr strPtr, ushort byteLength)
    {
        if (strPtr == IntPtr.Zero || byteLength == 0 || byteLength > 4096 || (byteLength % 2) != 0)
            return null;

        byte[] strBuf = new byte[byteLength];
        if (!ReadProcessMemory(hProcess, strPtr, strBuf, byteLength, out IntPtr read) || read.ToInt64() < byteLength)
            return null;

        return Encoding.Unicode.GetString(strBuf);
    }

    // -----------------------------------------------------------------------
    // アプリ起動 & ウィンドウ位置・サイズ復元
    // -----------------------------------------------------------------------

    /// <summary>
    /// 登録された Window Set の各アプリを新規起動し、出現したウィンドウの位置・サイズを復元する。
    /// </summary>
    public static void LaunchAndRestoreAsync(
        Keys key,
        IReadOnlyList<WindowSetApp> apps,
        Action<string, int> showNotification)
    {
        if (apps.Count == 0) return;

        var snapshot = apps.Select(a => new WindowSetApp
        {
            AppPath = a.AppPath,
            WorkingDir = a.WorkingDir,
            X = a.X,
            Y = a.Y,
            Width = a.Width,
            Height = a.Height,
        }).ToList();

        string keyName = AppConfig.KeysToWindowSetName(key);
        showNotification($"Window Set [{keyName}] を起動中 ({snapshot.Count} 件)...", 1500);

        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteLaunchAndRestoreAsync(keyName, snapshot, showNotification);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowSet] 復元処理エラー: {ex.Message}");
            }
        });
    }

    private static async Task ExecuteLaunchAndRestoreAsync(
        string keyName,
        List<WindowSetApp> apps,
        Action<string, int> showNotification)
    {
        object claimedLock = new();
        HashSet<IntPtr> claimed = SnapshotVisibleTopLevelWindows();

        // 同じ実行ファイルの複数エントリは順に起動してウィンドウ取り違えを防ぎ、
        // 異なる実行ファイル同士は並列に起動する。
        var groups = apps
            .GroupBy(a => a.AppPath, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.ToList())
            .ToList();

        int launchedOk = 0;
        int restoredOk = 0;

        var tasks = groups.Select(group => Task.Run(async () =>
        {
            foreach (var app in group)
            {
                (bool started, bool positioned) = await LaunchSingleAndRestoreAsync(app, claimed, claimedLock);
                if (started) Interlocked.Increment(ref launchedOk);
                if (positioned) Interlocked.Increment(ref restoredOk);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        if (restoredOk < apps.Count)
        {
            showNotification(
                $"Window Set [{keyName}]: {launchedOk}/{apps.Count} 件起動, {restoredOk}/{apps.Count} 件配置復元",
                2500);
        }
    }

    private static async Task<(bool started, bool positioned)> LaunchSingleAndRestoreAsync(
        WindowSetApp app,
        HashSet<IntPtr> claimed,
        object claimedLock)
    {
        (bool started, Process? proc) = StartProcess(app);
        if (!started)
            return (false, false);

        uint launchedPid = 0;
        if (proc != null)
        {
            try { launchedPid = (uint)proc.Id; } catch { }
        }

        string expectedFileName = Path.GetFileNameWithoutExtension(app.AppPath);

        IntPtr foundHwnd = IntPtr.Zero;
        var sw = Stopwatch.StartNew();
        const int timeoutMs = 12000;

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            bool allowFallbackByImage = proc == null || sw.ElapsedMilliseconds >= 400;
            if (!allowFallbackByImage && proc != null)
            {
                try
                {
                    if (proc.HasExited)
                        allowFallbackByImage = true;
                }
                catch
                {
                    allowFallbackByImage = true;
                }
            }

            foundHwnd = TryClaimNewWindow(launchedPid, app.AppPath, expectedFileName,
                allowFallbackByImage, claimed, claimedLock);

            if (foundHwnd != IntPtr.Zero)
                break;

            await Task.Delay(100);
        }

        if (proc != null)
        {
            try { proc.Dispose(); } catch { }
        }

        if (foundHwnd == IntPtr.Zero)
            return (true, false);

        // 初期描画直後の自己リサイズ対策として適用後に短い待機を挟んで再確認する
        ApplyWindowBounds(foundHwnd, app);
        await Task.Delay(350);
        if (IsWindow(foundHwnd))
            ApplyWindowBounds(foundHwnd, app);

        return (true, true);
    }

    private static (bool started, Process? proc) StartProcess(WindowSetApp app)
    {
        string workDir = !string.IsNullOrWhiteSpace(app.WorkingDir) && Directory.Exists(app.WorkingDir)
            ? app.WorkingDir
            : (Path.GetDirectoryName(app.AppPath) ?? "");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = app.AppPath,
                WorkingDirectory = workDir,
                UseShellExecute = false,
            };
            return (true, Process.Start(psi));
        }
        catch (Win32Exception)
        {
            // UAC 昇格が必要なアプリやシェルフックが必要な実行ファイルは UseShellExecute で再試行
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = app.AppPath,
                    WorkingDirectory = workDir,
                    UseShellExecute = true,
                };
                return (true, Process.Start(psi));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowSet] 起動失敗 ({app.AppPath}): {ex.Message}");
                return (false, null);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowSet] 起動失敗 ({app.AppPath}): {ex.Message}");
            return (false, null);
        }
    }

    private static IntPtr TryClaimNewWindow(
        uint launchedPid,
        string expectedAppPath,
        string expectedFileName,
        bool allowFallbackByImage,
        HashSet<IntPtr> claimed,
        object claimedLock)
    {
        var candidates = new List<(IntPtr hWnd, uint pid)>();

        EnumWindows((hWnd, _) =>
        {
            if (!IsCandidateAppWindow(hWnd))
                return true;

            lock (claimedLock)
            {
                if (claimed.Contains(hWnd))
                    return true;
            }

            uint pid = ResolveTargetProcessId(hWnd);
            if (pid != 0)
                candidates.Add((hWnd, pid));

            return true;
        }, IntPtr.Zero);

        // 優先 1: 起動したプロセス ID と一致するウィンドウ
        if (launchedPid != 0)
        {
            foreach (var (hWnd, pid) in candidates)
            {
                if (pid == launchedPid)
                {
                    lock (claimedLock)
                    {
                        if (claimed.Add(hWnd))
                            return hWnd;
                    }
                }
            }
        }

        if (!allowFallbackByImage)
            return IntPtr.Zero;

        // 優先 2: 実行ファイルパスが一致する新規ウィンドウ（ランチャー型・単一インスタンス型アプリ対応）
        foreach (var (hWnd, pid) in candidates)
        {
            string? path = GetProcessImagePath(pid);
            if (!string.IsNullOrEmpty(path)
                && string.Equals(path, expectedAppPath, StringComparison.OrdinalIgnoreCase))
            {
                lock (claimedLock)
                {
                    if (claimed.Add(hWnd))
                        return hWnd;
                }
            }
        }

        // 優先 3: 実行ファイル名（拡張子なし）が一致する新規ウィンドウ
        if (!string.IsNullOrWhiteSpace(expectedFileName))
        {
            foreach (var (hWnd, pid) in candidates)
            {
                string? path = GetProcessImagePath(pid);
                if (!string.IsNullOrEmpty(path)
                    && string.Equals(Path.GetFileNameWithoutExtension(path), expectedFileName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    lock (claimedLock)
                    {
                        if (claimed.Add(hWnd))
                            return hWnd;
                    }
                }
            }
        }

        return IntPtr.Zero;
    }

    private static void ApplyWindowBounds(IntPtr hWnd, WindowSetApp app)
    {
        if (app.Width <= 0 || app.Height <= 0) return;
        if (!IsWindow(hWnd)) return;

        if (IsIconic(hWnd) || IsZoomed(hWnd))
            ShowWindow(hWnd, SW_RESTORE);

        MoveWindow(hWnd, app.X, app.Y, app.Width, app.Height, true);

        // モニタ間 DPI 差異によるスケーリング補正が必要なら再適用
        if (GetWindowRect(hWnd, out RECT cur))
        {
            int curW = cur.Right - cur.Left;
            int curH = cur.Bottom - cur.Top;
            if (Math.Abs(cur.Left - app.X) > 2 || Math.Abs(cur.Top - app.Y) > 2
                || Math.Abs(curW - app.Width) > 2 || Math.Abs(curH - app.Height) > 2)
            {
                MoveWindow(hWnd, app.X, app.Y, app.Width, app.Height, true);
            }
        }
    }

    private static HashSet<IntPtr> SnapshotVisibleTopLevelWindows()
    {
        var set = new HashSet<IntPtr>();
        EnumWindows((hWnd, _) =>
        {
            if (IsCandidateAppWindow(hWnd))
                set.Add(hWnd);
            return true;
        }, IntPtr.Zero);
        return set;
    }

    private static bool IsCandidateAppWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero || !IsWindowVisible(hWnd) || IsIconic(hWnd))
            return false;
        if (hWnd == GetDesktopWindow() || hWnd == GetShellWindow())
            return false;

        GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid == 0 || pid == _currentPid)
            return false;

        if ((GetWindowLong(hWnd, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) != 0)
            return false;

        if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0
            && cloaked != 0)
            return false;

        if (!GetWindowRect(hWnd, out RECT rect))
            return false;
        if (rect.Right - rect.Left <= 1 || rect.Bottom - rect.Top <= 1)
            return false;

        var sb = new StringBuilder(128);
        if (GetClassName(hWnd, sb, sb.Capacity) > 0)
        {
            string cls = sb.ToString();
            if (cls.Equals("Progman", StringComparison.OrdinalIgnoreCase)
                || cls.Equals("WorkerW", StringComparison.OrdinalIgnoreCase)
                || cls.Equals("Shell_TrayWnd", StringComparison.OrdinalIgnoreCase)
                || cls.Equals("Shell_SecondaryTrayWnd", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
