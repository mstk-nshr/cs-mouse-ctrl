using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/// <summary>
/// システムトレイに常駐アイコンを表示し、「再起動」「停止」メニューを提供する。
/// </summary>
sealed class TrayIcon : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _icon;
    private bool _disposed;

    /// <summary>現在表示中のヘルプポップアップ（多重表示防止用）。</summary>
    private static HelpPopupForm? _helpPopup;

    /// <param name="restart">「再起動」選択時に呼び出す処理。</param>
    /// <param name="exit">「停止」選択時に呼び出す処理。</param>
    public TrayIcon(Action restart, Action exit)
    {
        _icon = CreateAppIcon();

        var menu = new ContextMenuStrip();

        var settingsItem = new ToolStripMenuItem("設定(&S)");
        settingsItem.Click += (_, _) => OpenConfig();

        var restartItem = new ToolStripMenuItem("再起動(&R)");
        restartItem.Click += (_, _) => restart();

        var exitItem = new ToolStripMenuItem("停止(&X)");
        exitItem.Click += (_, _) => exit();

        menu.Items.Add(settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(restartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "cs-mouse-ctrl",
            Visible = true,
            ContextMenuStrip = menu,
        };

        // ダブルクリックで使い方のヒントを表示（config.toml のキー割り当てを反映）
        // バルーンヒントは高さ制限で行が欠けるため、内容に合わせて自動サイズする
        // 独自ポップアップで表示する。
        _notifyIcon.DoubleClick += (_, _) => ShowHelpPopup();
    }

    /// <summary>
    /// キー割り当てのヘルプポップアップをカーソル付近に表示する。
    /// </summary>
    /// <param name="autoClose">一定時間（5秒）で自動的に閉じるかどうか。</param>
    internal static void ShowHelpPopup(bool autoClose = true)
    {
        _helpPopup?.Close();
        _helpPopup = new HelpPopupForm(BuildHelpText(), autoClose);
        _helpPopup.FormClosed += (_, _) => _helpPopup = null;
        _helpPopup.Show();
        _helpPopup.BringToTopMost();
    }

    /// <summary>
    /// 表示中のヘルプポップアップを閉じる。
    /// </summary>
    internal static void CloseHelpPopup()
    {
        if (_helpPopup != null)
        {
            var p = _helpPopup;
            _helpPopup = null;
            if (p.IsHandleCreated)
                p.BeginInvoke(() => p.Close());
            else
                p.Close();
        }
    }

    /// <summary>
    /// config.toml を Notepad で開く。
    /// </summary>
    private static void OpenConfig()
    {
        try
        {
            string path = AppConfig.ConfigPath;
            if (!System.IO.File.Exists(path))
                AppConfig.Load(); // 存在しない場合はデフォルトを生成

            Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"")
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TrayIcon] 設定ファイルを開けませんでした: {ex.Message}");
        }
    }

    /// <summary>
    /// 現在の設定（config.toml）を反映したキー割り当てのヘルプ文字列を生成する。
    /// </summary>
    private static string BuildHelpText()
    {
        string pause = AppConfig.PauseKey.ToString();
        string resize = AppConfig.ResizeKey.ToString();
        string fit = AppConfig.FitKey.ToString();

        return
            $"{pause} → NumPad1..9: カーソル移動\n" +
            $"{pause} → {resize}, click, click: ウィンドウリサイズ\n" +
            $"{pause} → {fit}: 空きエリアへフィット\n" +
            $"Alt + マウス移動: {AppConfig.AltMultiplier}倍速\n" +
            $"Ctrl + マウス移動: {AppConfig.CtrlMultiplier}倍速";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        CloseHelpPopup();

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
    }

    // -----------------------------------------------------------------------
    // アイコン生成
    // -----------------------------------------------------------------------

    /// <summary>
    /// アプリ用のアイコンを実行時に生成する（マウス形状）。
    /// </summary>
    private static Icon CreateAppIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var body = new Rectangle(8, 3, 16, 26);
            using var path = RoundedRect(body, 8);

            using var bodyBrush = new SolidBrush(Color.FromArgb(0, 120, 255));
            using var bodyPen = new Pen(Color.White, 2f);
            g.FillPath(bodyBrush, path);
            g.DrawPath(bodyPen, path);

            // ボタン分割線
            using var linePen = new Pen(Color.White, 1.5f);
            g.DrawLine(linePen, 16, 4, 16, 13);
            g.DrawLine(linePen, 9, 13, 23, 13);

            // ホイール
            using var wheelBrush = new SolidBrush(Color.White);
            g.FillRectangle(wheelBrush, 15, 7, 2, 4);
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var tmp = Icon.FromHandle(hIcon);
            return (Icon)tmp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    // -----------------------------------------------------------------------
    // ヘルプポップアップ
    // -----------------------------------------------------------------------

    /// <summary>
    /// キー割り当てのヘルプを表示する、内容に合わせて自動サイズする
    /// 枠なし・非アクティブのポップアップウィンドウ。
    /// </summary>
    private sealed class HelpPopupForm : Form
    {
        private readonly System.Windows.Forms.Timer? _closeTimer;

        protected override bool ShowWithoutActivation => true;

        public HelpPopupForm(string text, bool autoClose = true)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(32, 32, 32);
            Padding = new Padding(12);

            var label = new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(Padding.Left, Padding.Top),
            };
            Controls.Add(label);

            // 内容に合わせてウィンドウサイズを決定（行の欠けを防ぐ）
            ClientSize = new Size(
                label.PreferredWidth + Padding.Horizontal,
                label.PreferredHeight + Padding.Vertical);

            // 初期位置をカーソル周辺に決定
            UpdatePosition();

            // クリックで閉じる
            Click += (_, _) => Close();
            label.Click += (_, _) => Close();

            if (autoClose)
            {
                // 一定時間で自動的に閉じる
                _closeTimer = new System.Windows.Forms.Timer { Interval = 5000 };
                _closeTimer.Tick += (_, _) => Close();
                _closeTimer.Start();
            }
        }

        private void UpdatePosition()
        {
            Point cursorPt = Cursor.Position;
            Rectangle wa = Screen.FromPoint(cursorPt).WorkingArea;

            int x = cursorPt.X - Width / 2;
            int y = cursorPt.Y - Height - 12;
            // 上側に十分な余白がない場合はカーソルの下側に表示
            if (y < wa.Top)
                y = cursorPt.Y + 24;

            x = Math.Clamp(x, wa.Left, Math.Max(wa.Left, wa.Right - Width));
            y = Math.Clamp(y, wa.Top, Math.Max(wa.Top, wa.Bottom - Height));
            Location = new Point(x, y);
        }

        public void BringToTopMost()
        {
            if (IsHandleCreated)
            {
                SetWindowPos(Handle, HWND_TOPMOST, Left, Top, Width, Height,
                    SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdatePosition();
            BringToTopMost();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var borderPen = new Pen(Color.FromArgb(90, 90, 90), 1);
            e.Graphics.DrawRectangle(borderPen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _closeTimer?.Dispose();
            base.Dispose(disposing);
        }
    }
}
