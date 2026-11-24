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

    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    internal enum HelpCommand
    {
        Move,
        Resize,
        Edge,
        Fit,
        Topmost,
        Bottom,
        Minimize,
        Maximize,
        Close,
        Snap,
        Swap,
    }

    private readonly record struct HelpItem(string Text, HelpCommand? Command = null, bool IsMouseSpeedRow = false);

    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _icon;
    private bool _disposed;

    /// <summary>現在表示中のヘルプポップアップ（多重表示防止用）。</summary>
    private static HelpPopupForm? _helpPopup;

    /// <summary>ヘルプ項目クリック時に実行するコマンドハンドラー。</summary>
    private static Action<HelpCommand>? _executeCommand;

    /// <summary>現在のマウス速度（1..20）を取得するハンドラー。</summary>
    private static Func<int>? _getMouseSpeed;

    /// <summary>マウス速度（1..20）を設定するハンドラー。</summary>
    private static Func<int, int>? _setMouseSpeed;

    /// <param name="restart">「再起動」選択時に呼び出す処理。</param>
    /// <param name="exit">「停止」選択時に呼び出す処理。</param>
    /// <param name="executeCommand">ヘルプ項目クリック時に呼び出す処理。</param>
    /// <param name="getMouseSpeed">現在のマウス速度を取得する処理。</param>
    /// <param name="setMouseSpeed">マウス速度を設定する処理。</param>
    public TrayIcon(
        Action restart,
        Action exit,
        Action<HelpCommand>? executeCommand = null,
        Func<int>? getMouseSpeed = null,
        Func<int, int>? setMouseSpeed = null)
    {
        _executeCommand = executeCommand;
        _getMouseSpeed = getMouseSpeed;
        _setMouseSpeed = setMouseSpeed;
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
    /// <param name="onClosed">ポップアップが閉じられたときのコールバック。</param>
    internal static void ShowHelpPopup(bool autoClose = true, Action? onClosed = null)
    {
        CloseHelpPopup();
        var popup = new HelpPopupForm(
            BuildHelpItems(),
            autoClose,
            onCommand: cmd => _executeCommand?.Invoke(cmd),
            getMouseSpeed: _getMouseSpeed,
            setMouseSpeed: _setMouseSpeed);
        _helpPopup = popup;
        popup.FormClosed += (_, _) =>
        {
            if (_helpPopup == popup)
                _helpPopup = null;
            onClosed?.Invoke();
        };
        popup.Show();
        popup.BringToTopMost();
    }

    /// <summary>
    /// 指定したスクリーン座標が現在表示中のヘルプポップアップ内にあるかどうかを返す。
    /// </summary>
    internal static bool IsPointInHelpPopup(int screenX, int screenY)
    {
        var popup = _helpPopup;
        return popup != null && !popup.IsDisposed && popup.Visible
            && popup.Bounds.Contains(screenX, screenY);
    }

    /// <summary>
    /// 低レベルマウスフックからのイベントをヘルプポップアップ内の速度操作コントロールへ直接ルーティングする。
    /// イベントを消費すべき場合は true を返す。
    /// </summary>
    internal static bool TryHandleHelpPopupMouse(int msg, int screenX, int screenY)
    {
        var popup = _helpPopup;
        if (popup == null || popup.IsDisposed || !popup.Visible)
            return false;
        return popup.TryHandleHookMouse(msg, screenX, screenY);
    }

    /// <summary>
    /// 現在表示中のヘルプポップアップの境界矩形を返す（非表示時は null）。
    /// </summary>
    internal static Rectangle? GetHelpPopupBounds()
    {
        var popup = _helpPopup;
        if (popup != null && !popup.IsDisposed && popup.Visible)
            return popup.Bounds;
        return null;
    }

    /// <summary>
    /// 任意のメッセージをカーソル周辺にポップアップ表示する。
    /// durationMs が 0 以下の場合は明示的に閉じるまで表示し続ける。
    /// </summary>
    internal static void ShowMessage(string text, int durationMs = 1500, bool clickThrough = false)
    {
        CloseHelpPopup();
        bool autoClose = durationMs > 0;
        var popup = new HelpPopupForm(
            text,
            autoClose: autoClose,
            durationMs: autoClose ? durationMs : 1500,
            clickThrough: clickThrough);
        _helpPopup = popup;
        popup.FormClosed += (_, _) =>
        {
            if (_helpPopup == popup)
                _helpPopup = null;
        };
        popup.Show();
        popup.BringToTopMost();
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
            try { p.Hide(); } catch { }
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
    /// 現在の設定（config.toml）を反映したキー割り当てのヘルプ項目リストを生成する。
    /// </summary>
    private static HelpItem[] BuildHelpItems()
    {
        // config.toml の hotkey 配列に列挙されたキーをまとめて表示する
        string pause = AppConfig.PauseKeysDisplay;
        string move = AppConfig.MoveKey.ToString();
        string resize = AppConfig.ResizeKey.ToString();
        string fit = AppConfig.FitKey.ToString();
        string topmost = AppConfig.TopmostKey.ToString();
        string bottom = AppConfig.BottomKey.ToString();
        string minimize = AppConfig.MinimizeKey.ToString();
        string maximize = AppConfig.MaximizeKey.ToString();
        string close = AppConfig.CloseKey.ToString();
        string snap = AppConfig.SnapKey.ToString();
        string swap = AppConfig.SwapKey.ToString();

        return new[]
        {
            new HelpItem("cs_mouse_ctrl v2611.2409"),
            new HelpItem(""),
            new HelpItem($"{pause} →"),
            new HelpItem("    NumPad1..9:  カーソル移動"),
            new HelpItem($"    {move}:  ウィンドウ移動", HelpCommand.Move),
            new HelpItem($"    {resize}:  ウィンドウリサイズ", HelpCommand.Resize),
            new HelpItem($"    {AppConfig.EdgeKey}:  辺拡張（矢印で辺選択→マウスで移動、同じ矢印で境界まで拡張）", HelpCommand.Edge),
            new HelpItem($"    {fit}:  空きエリアへフィット", HelpCommand.Fit),
            new HelpItem($"    {topmost}:  最前面固定/解除", HelpCommand.Topmost),
            new HelpItem($"    {bottom}:  最背面へ移動", HelpCommand.Bottom),
            new HelpItem($"    {minimize}:  最小化", HelpCommand.Minimize),
            new HelpItem($"    {maximize}:  最大化", HelpCommand.Maximize),
            new HelpItem($"    {close}:  ウィンドウを閉じる", HelpCommand.Close),
            new HelpItem($"    {snap}:  スナップ選択 UI", HelpCommand.Snap),
            new HelpItem($"    {swap}:  ウィンドウ位置入れ替え", HelpCommand.Swap),
            new HelpItem("    , / . :  マウス移動速度", IsMouseSpeedRow: true),
            new HelpItem($"Alt + マウス移動:  {AppConfig.AltMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)}倍速"),
            new HelpItem($"Ctrl + マウス移動:  {AppConfig.CtrlMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)}倍速"),
        };
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
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;
        private const int WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;

        private readonly System.Windows.Forms.Timer? _closeTimer;
        private readonly Font _normalFont;
        private readonly Font _underlineFont;
        private readonly bool _clickThrough;
        private bool _fontsDisposed;

        private StepButtonControl? _minusBtn;
        private StepButtonControl? _plusBtn;
        private SpeedSliderControl? _slider;

        protected override bool ShowWithoutActivation => true;

        public HelpPopupForm(string text, bool autoClose = true, int durationMs = 5000, bool clickThrough = false)
            : this(new[] { new HelpItem(text) }, autoClose, durationMs, null, null, null, clickThrough)
        {
        }

        public HelpPopupForm(
            HelpItem[] items,
            bool autoClose = true,
            int durationMs = 5000,
            Action<HelpCommand>? onCommand = null,
            Func<int>? getMouseSpeed = null,
            Func<int, int>? setMouseSpeed = null,
            bool clickThrough = false)
        {
            _clickThrough = clickThrough;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(32, 32, 32);
            Padding = new Padding(12);

            _normalFont = new Font("Segoe UI", 9f);
            _underlineFont = new Font(_normalFont, FontStyle.Underline);

            int y = Padding.Top;
            int maxWidth = 0;
            int emptyLineHeight = TextRenderer.MeasureText(" ", _normalFont).Height;

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Text))
                {
                    y += emptyLineHeight;
                    continue;
                }

                if (item.IsMouseSpeedRow)
                {
                    int rowWidth = AddMouseSpeedRow(item.Text, y, getMouseSpeed, setMouseSpeed, out int rowHeight);
                    maxWidth = Math.Max(maxWidth, rowWidth);
                    y += rowHeight;
                    continue;
                }

                var label = new Label
                {
                    Text = item.Text,
                    AutoSize = true,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Font = _normalFont,
                    Location = new Point(Padding.Left, y),
                };

                if (item.Command is HelpCommand cmd && onCommand != null)
                {
                    label.Cursor = Cursors.Hand;
                    label.MouseEnter += (_, _) =>
                    {
                        if (!IsDisposed && !label.IsDisposed && !_fontsDisposed)
                            label.Font = _underlineFont;
                    };
                    label.MouseLeave += (_, _) =>
                    {
                        if (!IsDisposed && !label.IsDisposed && !_fontsDisposed)
                            label.Font = _normalFont;
                    };
                    label.Click += (_, _) => onCommand(cmd);
                }
                else if (!_clickThrough)
                {
                    label.Click += (_, _) => Close();
                }

                Controls.Add(label);
                maxWidth = Math.Max(maxWidth, label.PreferredWidth);
                y += label.PreferredHeight;
            }

            // 内容に合わせてウィンドウサイズを決定（行の欠けを防ぐ）
            ClientSize = new Size(
                maxWidth + Padding.Horizontal,
                y + Padding.Bottom);

            // 初期位置をカーソル周辺に決定
            UpdatePosition();

            // クリックで閉じる
            if (!_clickThrough)
                Click += (_, _) => Close();

            if (autoClose)
            {
                // 一定時間で自動的に閉じる
                _closeTimer = new System.Windows.Forms.Timer { Interval = durationMs };
                _closeTimer.Tick += (_, _) => Close();
                _closeTimer.Start();
            }
        }

        private int AddMouseSpeedRow(
            string title,
            int y,
            Func<int>? getMouseSpeed,
            Func<int, int>? setMouseSpeed,
            out int rowHeight)
        {
            int currentSpeed = Math.Clamp(getMouseSpeed?.Invoke() ?? 10, 1, 20);

            var titleLabel = new Label
            {
                Text = title,
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = _normalFont,
                Location = new Point(Padding.Left, y + 2),
            };
            Controls.Add(titleLabel);

            int ctrlHeight = Math.Max(20, titleLabel.PreferredHeight + 2);
            rowHeight = ctrlHeight + 2;

            int x = Padding.Left + titleLabel.PreferredWidth + 8;

            var minusBtn = new StepButtonControl("-", _normalFont, _underlineFont)
            {
                Location = new Point(x, y),
                Size = new Size(22, ctrlHeight),
            };
            x += minusBtn.Width + 4;

            var slider = new SpeedSliderControl(currentSpeed)
            {
                Location = new Point(x, y),
                Size = new Size(120, ctrlHeight),
            };
            x += slider.Width + 4;

            var plusBtn = new StepButtonControl("+", _normalFont, _underlineFont)
            {
                Location = new Point(x, y),
                Size = new Size(22, ctrlHeight),
            };
            x += plusBtn.Width + 6;

            var valueLabel = new Label
            {
                Text = currentSpeed.ToString(),
                AutoSize = false,
                Size = new Size(22, ctrlHeight),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(120, 190, 255),
                BackColor = Color.Transparent,
                Font = _normalFont,
                Location = new Point(x, y),
            };
            x += valueLabel.Width;

            void ApplySpeed(int newSpeed)
            {
                int applied = setMouseSpeed != null
                    ? setMouseSpeed(newSpeed)
                    : Math.Clamp(newSpeed, 1, 20);
                slider.SetValue(applied);
                valueLabel.Text = applied.ToString();
                slider.Refresh();
                valueLabel.Refresh();
                if (_closeTimer != null)
                {
                    _closeTimer.Stop();
                    _closeTimer.Start();
                }
            }

            minusBtn.StepTriggered += (_, _) => ApplySpeed(slider.Value - 1);
            plusBtn.StepTriggered += (_, _) => ApplySpeed(slider.Value + 1);
            slider.ValueChanged += (_, newVal) => ApplySpeed(newVal);

            _minusBtn = minusBtn;
            _plusBtn = plusBtn;
            _slider = slider;

            Controls.Add(minusBtn);
            Controls.Add(slider);
            Controls.Add(plusBtn);
            Controls.Add(valueLabel);

            return x - Padding.Left;
        }

        public bool TryHandleHookMouse(int msg, int screenX, int screenY)
        {
            if (IsDisposed || !Visible) return false;
            var pt = new Point(screenX, screenY);

            if (msg == WM_LBUTTONDOWN)
            {
                if (_minusBtn != null && !_minusBtn.IsDisposed
                    && _minusBtn.RectangleToScreen(_minusBtn.ClientRectangle).Contains(pt))
                {
                    _minusBtn.PressStart();
                    return true;
                }
                if (_plusBtn != null && !_plusBtn.IsDisposed
                    && _plusBtn.RectangleToScreen(_plusBtn.ClientRectangle).Contains(pt))
                {
                    _plusBtn.PressStart();
                    return true;
                }
                if (_slider != null && !_slider.IsDisposed
                    && _slider.RectangleToScreen(_slider.ClientRectangle).Contains(pt))
                {
                    _slider.BeginDragFromScreenX(screenX);
                    return true;
                }
                return false;
            }

            if (msg == WM_MOUSEMOVE)
            {
                if (_slider != null && !_slider.IsDisposed && _slider.IsDragging)
                    _slider.DragFromScreenX(screenX);
                return false;
            }

            if (msg == WM_LBUTTONUP)
            {
                bool handled = false;
                if (_minusBtn != null && !_minusBtn.IsDisposed && _minusBtn.IsPressed)
                {
                    _minusBtn.PressEnd();
                    handled = true;
                }
                if (_plusBtn != null && !_plusBtn.IsDisposed && _plusBtn.IsPressed)
                {
                    _plusBtn.PressEnd();
                    handled = true;
                }
                if (_slider != null && !_slider.IsDisposed && _slider.IsDragging)
                {
                    _slider.EndDrag();
                    handled = true;
                }
                return handled;
            }

            return false;
        }

        private sealed class StepButtonControl : Control
        {
            private readonly Font _normalFont;
            private readonly Font _underlineFont;
            private readonly System.Windows.Forms.Timer _repeatTimer;
            private bool _hovered;
            private bool _pressed;

            public bool IsPressed => _pressed;
            public event EventHandler? StepTriggered;

            public void PressStart()
            {
                if (IsDisposed) return;
                _pressed = true;
                Refresh();
                StepTriggered?.Invoke(this, EventArgs.Empty);
                _repeatTimer.Interval = 350;
                _repeatTimer.Start();
            }

            public void PressEnd()
            {
                if (IsDisposed || !_pressed) return;
                _pressed = false;
                _repeatTimer.Stop();
                Refresh();
            }

            public StepButtonControl(string text, Font normalFont, Font underlineFont)
            {
                Text = text;
                _normalFont = normalFont;
                _underlineFont = underlineFont;
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                    true);
                Cursor = Cursors.Hand;

                _repeatTimer = new System.Windows.Forms.Timer { Interval = 350 };
                _repeatTimer.Tick += (_, _) =>
                {
                    _repeatTimer.Interval = 80;
                    StepTriggered?.Invoke(this, EventArgs.Empty);
                };
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                _hovered = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hovered = false;
                _pressed = false;
                _repeatTimer.Stop();
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Left)
                    PressStart();
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (e.Button == MouseButtons.Left)
                    PressEnd();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var bg = _pressed
                    ? Color.FromArgb(0, 120, 255)
                    : _hovered
                        ? Color.FromArgb(75, 75, 75)
                        : Color.FromArgb(52, 52, 52);

                using (var brush = new SolidBrush(bg))
                    e.Graphics.FillRectangle(brush, ClientRectangle);

                var font = _hovered ? _underlineFont : _normalFont;
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    font,
                    ClientRectangle,
                    Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    _repeatTimer.Dispose();
                base.Dispose(disposing);
            }
        }

        private sealed class SpeedSliderControl : Control
        {
            private int _value;
            private bool _hovered;
            private bool _dragging;

            public int Value => _value;
            public bool IsDragging => _dragging;
            public event EventHandler<int>? ValueChanged;

            public void BeginDragFromScreenX(int screenX)
            {
                if (IsDisposed) return;
                _dragging = true;
                int localX = PointToClient(new Point(screenX, 0)).X;
                UpdateFromX(localX);
                Refresh();
            }

            public void DragFromScreenX(int screenX)
            {
                if (IsDisposed || !_dragging) return;
                int localX = PointToClient(new Point(screenX, 0)).X;
                UpdateFromX(localX);
            }

            public void EndDrag()
            {
                if (IsDisposed || !_dragging) return;
                _dragging = false;
                Capture = false;
                Refresh();
            }

            public SpeedSliderControl(int initialValue)
            {
                _value = Math.Clamp(initialValue, 1, 20);
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                    true);
                BackColor = Color.FromArgb(32, 32, 32);
                Cursor = Cursors.Hand;
            }

            public void SetValue(int value)
            {
                int clamped = Math.Clamp(value, 1, 20);
                if (clamped == _value) return;
                _value = clamped;
                Invalidate();
            }

            private void UpdateFromX(int x)
            {
                const int pad = 8;
                int trackWidth = Math.Max(1, Width - pad * 2);
                double ratio = Math.Clamp((double)(x - pad) / trackWidth, 0.0, 1.0);
                int newVal = 1 + (int)Math.Round(ratio * 19.0);
                if (newVal != _value)
                {
                    _value = newVal;
                    Invalidate();
                    ValueChanged?.Invoke(this, _value);
                }
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                _hovered = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hovered = false;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Left)
                {
                    _dragging = true;
                    Capture = true;
                    UpdateFromX(e.X);
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (_dragging)
                    UpdateFromX(e.X);
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (_dragging && e.Button == MouseButtons.Left)
                {
                    _dragging = false;
                    Capture = false;
                    Invalidate();
                }
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                int next = Math.Clamp(_value + (e.Delta > 0 ? 1 : -1), 1, 20);
                if (next != _value)
                {
                    _value = next;
                    Invalidate();
                    ValueChanged?.Invoke(this, _value);
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                const int pad = 8;
                int cy = Height / 2;
                int trackLeft = pad;
                int trackRight = Math.Max(pad + 1, Width - pad);
                float ratio = (_value - 1) / 19f;
                int thumbX = trackLeft + (int)Math.Round((trackRight - trackLeft) * ratio);

                using var bgPen = new Pen(Color.FromArgb(90, 90, 90), 4f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                };
                g.DrawLine(bgPen, trackLeft, cy, trackRight, cy);

                if (thumbX > trackLeft)
                {
                    using var activePen = new Pen(Color.FromArgb(0, 120, 255), 4f)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round,
                    };
                    g.DrawLine(activePen, trackLeft, cy, thumbX, cy);
                }

                int r = (_hovered || _dragging) ? 6 : 5;
                var thumbRect = new Rectangle(thumbX - r, cy - r, r * 2, r * 2);
                using var thumbBrush = new SolidBrush(Color.White);
                using var thumbPen = new Pen(Color.FromArgb(0, 120, 255), 1.5f);
                g.FillEllipse(thumbBrush, thumbRect);
                g.DrawEllipse(thumbPen, thumbRect);
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
                if (_clickThrough)
                    cp.ExStyle |= WS_EX_TRANSPARENT;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (_clickThrough && m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
                return;
            }
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _closeTimer?.Dispose();

            // 子コントロール破棄（WmDestroy での MouseLeave 等）が完了してからフォントを破棄する
            base.Dispose(disposing);

            if (disposing && !_fontsDisposed)
            {
                _fontsDisposed = true;
                _underlineFont.Dispose();
                _normalFont.Dispose();
            }
        }
    }
}
