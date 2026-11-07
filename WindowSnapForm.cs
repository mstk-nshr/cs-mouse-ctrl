using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/// <summary>
/// ウィンドウスナップ選択 UI。
/// 1x1〜4x4 の 16 種類のグリッドレイアウトを 4列×4行に並べて表示する。
///
/// ホバー判定はグループ内の「個別セル」単位で行う。
///   例）2x2 グループ内の右上セルにホバー → そのセルだけアクセントカラーで強調
///       クリック → デスクトップ右上 1/4 の位置にウィンドウをスナップ
///
/// Rect 外クリックまたはキー入力でキャンセルする。
/// </summary>
sealed class WindowSnapForm : Form
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
    private static extern bool GetCursorPos(out Point lpPoint);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    // -----------------------------------------------------------------------
    // レイアウト定数
    // -----------------------------------------------------------------------

    // グリッドバリエーションの最大列数・行数
    private const int MaxCols = 4;
    private const int MaxRows = 4;

    // 各グリッドプレビュー内のセルサイズ（px）
    private const int PreviewCellW = 26;
    private const int PreviewCellH = 22;
    // プレビュー内セル間の隙間
    private const int PreviewGap = 3;
    // グリッドプレビュー間の水平マージン
    private const int GroupMarginX = 24;
    // グループ行の垂直間隔（ラベル高さ + 行間）
    private const int GroupRowSpacing = 36;
    // ウィンドウ左右パディング
    private const int PadX = 16;
    // ウィンドウ下パディング
    private const int PadBottom = 14;

    // タイトル行の高さ（1行目：タイトル文字、2行目：空白）
    private const int TitleAreaH = 44;
    // 各グループ行の上に置くラベル（"1x1" 等）の高さ
    private const int LabelAreaH = 22;

    // グループタイトルフォントサイズ
    private const float LabelFontSize = 8.5f;

    // -----------------------------------------------------------------------
    // 内部構造体：ホバー位置
    // -----------------------------------------------------------------------

    /// <summary>
    /// ホバー中のセルを表す。
    /// groupIdx: _groupRects のインデックス
    /// cellCol / cellRow: グループ内のセル座標（0 始まり）
    /// </summary>
    private record struct HoverCell(int GroupIdx, int CellCol, int CellRow)
    {
        public static readonly HoverCell None = new(-1, -1, -1);
        public bool IsNone => GroupIdx < 0;
    }

    // -----------------------------------------------------------------------
    // フィールド
    // -----------------------------------------------------------------------

    private readonly IntPtr _target;
    private readonly Action<IntPtr, int, int, int, int> _onSelected;
    private readonly Action _onCancelled;

    private HoverCell _hover = HoverCell.None;

    /// <summary>グループごとの描画領域（グリッド線の内側）。</summary>
    private Rectangle[] _groupRects = Array.Empty<Rectangle>();

    private readonly Font _labelFont;
    private readonly Font _titleFont;
    private readonly Color _accentColor;
    private readonly System.Windows.Forms.Timer _hoverTimer;

    // -----------------------------------------------------------------------
    // コンストラクタ
    // -----------------------------------------------------------------------

    protected override bool ShowWithoutActivation => true;

    public WindowSnapForm(IntPtr target,
        Action<IntPtr, int, int, int, int> onSelected,
        Action onCancelled)
    {
        _target      = target;
        _onSelected  = onSelected;
        _onCancelled = onCancelled;

        _accentColor = GetAccentColor();
        _labelFont   = new Font("Segoe UI", LabelFontSize, FontStyle.Regular, GraphicsUnit.Point);
        _titleFont   = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

        FormBorderStyle = FormBorderStyle.None;
        TopMost         = true;
        ShowInTaskbar   = false;
        StartPosition   = FormStartPosition.Manual;
        BackColor       = Color.FromArgb(30, 30, 30);
        DoubleBuffered  = true;

        LayoutGroups();
        UpdatePosition();

        // 16ms ポーリングでカーソル位置からホバー判定
        _hoverTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _hoverTimer.Tick += HoverTimer_Tick;
        _hoverTimer.Start();

        MouseClick += OnMouseClick;
        KeyDown    += OnKeyDown;
        Deactivate += (_, _) => Cancel();
    }

    // -----------------------------------------------------------------------
    // グループレイアウト計算
    // -----------------------------------------------------------------------

    private void LayoutGroups()
    {
        _groupRects = new Rectangle[MaxCols * MaxRows];

        // 全グループを最大サイズ（4x4）相当の矩形に揃える
        int groupW = PreviewCellW * MaxCols + PreviewGap * (MaxCols - 1);
        int groupH = PreviewCellH * MaxRows + PreviewGap * (MaxRows - 1);

        // 1行目のグループ上端 = タイトル領域 + ラベル領域
        int firstTop = TitleAreaH + LabelAreaH;

        int totalW = PadX * 2 + groupW * MaxCols + GroupMarginX * (MaxCols - 1);
        int totalH = firstTop + groupH * MaxRows
                     + GroupRowSpacing * (MaxRows - 1)
                     + PadBottom;

        for (int row = 0; row < MaxRows; row++)
        {
            for (int col = 0; col < MaxCols; col++)
            {
                int gx = PadX + col * (groupW + GroupMarginX);
                int gy = firstTop + row * (groupH + GroupRowSpacing);
                _groupRects[row * MaxCols + col] = new Rectangle(gx, gy, groupW, groupH);
            }
        }

        ClientSize = new Size(totalW, totalH);
    }

    // -----------------------------------------------------------------------
    // 位置決定
    // -----------------------------------------------------------------------

    private void UpdatePosition()
    {
        GetCursorPos(out Point cur);
        Rectangle wa = Screen.FromPoint(cur).WorkingArea;

        int x = cur.X - Width / 2;
        int y = cur.Y - Height - 20;
        if (y < wa.Top) y = cur.Y + 20;

        x = Math.Clamp(x, wa.Left, Math.Max(wa.Left, wa.Right  - Width));
        y = Math.Clamp(y, wa.Top,  Math.Max(wa.Top,  wa.Bottom - Height));
        Location = new Point(x, y);
    }

    // -----------------------------------------------------------------------
    // Win32 ハンドル生成後の設定
    // -----------------------------------------------------------------------

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
        exStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLong(Handle, GWL_EXSTYLE, exStyle);
        SetWindowPos(Handle, HWND_TOPMOST, Left, Top, Width, Height,
            SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        SetWindowPos(Handle, HWND_TOPMOST, Left, Top, Width, Height,
            SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    // -----------------------------------------------------------------------
    // 描画
    // -----------------------------------------------------------------------

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode        = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint    = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // --- タイトル（1行目に描画、2行目は空白として TitleAreaH が確保する）---
        using var titleBrush = new SolidBrush(Color.White);
        var titleRect = new Rectangle(0, 0, Width, TitleAreaH);
        using var sf = new StringFormat
        {
            Alignment     = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        g.DrawString("Window Snap", _titleFont, titleBrush, titleRect, sf);

        // --- グループ ---
        using var normalBrush = new SolidBrush(Color.FromArgb(80, 80, 80));
        using var hoverBrush  = new SolidBrush(_accentColor);
        using var dimBrush    = new SolidBrush(Color.FromArgb(55, 55, 55));
        using var labelBrush  = new SolidBrush(Color.FromArgb(180, 180, 180));
        using var borderPen   = new Pen(Color.FromArgb(110, 110, 110), 1f);
        using var hoverPen    = new Pen(Color.White, 1f);

        for (int row = 0; row < MaxRows; row++)
        {
            for (int col = 0; col < MaxCols; col++)
            {
                int      groupIdx  = row * MaxCols + col;
                int      cols      = col + 1;   // このグループのグリッド列数
                int      rows      = row + 1;   // このグループのグリッド行数
                Rectangle groupRect = _groupRects[groupIdx];

                // ラベル（グループ上方の LabelAreaH 領域内に中央揃えで描画）
                var labelRect = new Rectangle(
                    groupRect.Left,
                    groupRect.Top - LabelAreaH,
                    groupRect.Width,
                    LabelAreaH);
                g.DrawString($"{cols}x{rows}", _labelFont, labelBrush, labelRect, sf);

                // セルごとのピクセルサイズ（グループ内）
                int cellW = (groupRect.Width  - PreviewGap * (cols - 1)) / cols;
                int cellH = (groupRect.Height - PreviewGap * (rows - 1)) / rows;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int cx = groupRect.Left + c * (cellW + PreviewGap);
                        int cy = groupRect.Top  + r * (cellH + PreviewGap);
                        var cellRect = new Rectangle(cx, cy, cellW, cellH);

                        // このセルがホバー対象かどうか
                        bool isHovered = !_hover.IsNone
                            && _hover.GroupIdx == groupIdx
                            && _hover.CellCol  == c
                            && _hover.CellRow  == r;

                        // ホバーされているグループかどうか（非ホバーセルは少し暗く）
                        bool groupHovered = !_hover.IsNone && _hover.GroupIdx == groupIdx;

                        Brush fillBrush = isHovered  ? hoverBrush
                                        : groupHovered ? dimBrush
                                        : normalBrush;

                        g.FillRectangle(fillBrush, cellRect);
                        g.DrawRectangle(isHovered ? hoverPen : borderPen, cellRect);
                    }
                }
            }
        }

        // ウィンドウ枠
        using var outerPen = new Pen(Color.FromArgb(80, 80, 80), 1);
        g.DrawRectangle(outerPen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }

    // -----------------------------------------------------------------------
    // ホバー判定（タイマー駆動）
    // -----------------------------------------------------------------------

    private void HoverTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsHandleCreated || IsDisposed) return;

        GetCursorPos(out Point screenPt);
        Point local = PointToClient(screenPt);

        HoverCell newHover = HitTestCell(local);
        if (newHover != _hover)
        {
            _hover = newHover;
            Invalidate();
        }
    }

    /// <summary>
    /// ローカル座標からホバーセルを特定する。
    /// グループ矩形内のどのセルに対応するかを計算して返す。
    /// </summary>
    private HoverCell HitTestCell(Point pt)
    {
        for (int row = 0; row < MaxRows; row++)
        {
            for (int col = 0; col < MaxCols; col++)
            {
                int      groupIdx  = row * MaxCols + col;
                Rectangle groupRect = _groupRects[groupIdx];

                if (!groupRect.Contains(pt)) continue;

                int cols  = col + 1;
                int rows  = row + 1;
                int cellW = (groupRect.Width  - PreviewGap * (cols - 1)) / cols;
                int cellH = (groupRect.Height - PreviewGap * (rows - 1)) / rows;

                // グループ内の相対座標
                int rx = pt.X - groupRect.Left;
                int ry = pt.Y - groupRect.Top;

                // セル列・行を計算（隙間部分はセルの外 → ヒットなし扱い不要＝隣のセルへ丸め）
                int cellCol = Math.Clamp(rx / (cellW + PreviewGap), 0, cols - 1);
                int cellRow = Math.Clamp(ry / (cellH + PreviewGap), 0, rows - 1);

                return new HoverCell(groupIdx, cellCol, cellRow);
            }
        }
        return HoverCell.None;
    }

    // -----------------------------------------------------------------------
    // マウスクリック
    // -----------------------------------------------------------------------

    private void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            Cancel();
            return;
        }

        HoverCell hit = HitTestCell(e.Location);
        if (hit.IsNone)
        {
            Cancel();
            return;
        }

        // グループインデックス → cols / rows
        int groupRow = hit.GroupIdx / MaxCols;
        int groupCol = hit.GroupIdx % MaxCols;
        int cols     = groupCol + 1;
        int rows     = groupRow + 1;

        // 選択されたセルの位置（0 始まり）
        int cellCol = hit.CellCol;
        int cellRow = hit.CellRow;

        _onSelected(_target, cols, rows, cellCol, cellRow);
    }

    // -----------------------------------------------------------------------
    // キー入力
    // -----------------------------------------------------------------------

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        Cancel();
        e.Handled = true;
    }

    // -----------------------------------------------------------------------
    // キャンセル
    // -----------------------------------------------------------------------

    private void Cancel() => _onCancelled();

    // -----------------------------------------------------------------------
    // アクセントカラー取得
    // -----------------------------------------------------------------------

    private static Color GetAccentColor()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int abgr)
            {
                // Windows は ABGR で格納
                byte r  = (byte)( abgr        & 0xFF);
                byte gg = (byte)((abgr >>  8) & 0xFF);
                byte b  = (byte)((abgr >> 16) & 0xFF);
                return Color.FromArgb(255, r, gg, b);
            }
        }
        catch { }
        return Color.FromArgb(0, 120, 215); // Windows Blue フォールバック
    }

    // -----------------------------------------------------------------------
    // Dispose
    // -----------------------------------------------------------------------

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hoverTimer.Stop();
            _hoverTimer.Dispose();
            _labelFont.Dispose();
            _titleFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
