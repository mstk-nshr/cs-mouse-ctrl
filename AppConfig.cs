using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

/// <summary>
/// Window Set に登録される 1 アプリ分の起動・配置情報。
/// </summary>
sealed class WindowSetApp
{
    public string AppPath { get; set; } = "";
    public string WorkingDir { get; set; } = "";
    public List<string> Params { get; set; } = new();
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>一覧表示用のアプリ名（拡張子なしの実行ファイル名）。</summary>
    public string DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AppPath)) return "(unknown)";
            string name = Path.GetFileNameWithoutExtension(AppPath);
            return string.IsNullOrWhiteSpace(name) ? AppPath : name;
        }
    }
}

/// <summary>
/// %LOCALAPPDATA%\cs-mouse-ctrl\config.toml を読み書きする設定クラス。
/// ファイルが存在しない場合はデフォルト値で自動生成する。
/// </summary>
static class AppConfig
{
    // -----------------------------------------------------------------------
    // 設定値（既定値）
    // -----------------------------------------------------------------------

    /// <summary>Alt + Mouse Move の速度倍率（デフォルト 0.3）</summary>
    public static double AltMultiplier { get; private set; } = 0.3;

    /// <summary>互換用エイリアス</summary>
    public static double ShiftMultiplier => AltMultiplier;

    /// <summary>Ctrl + Mouse Move の速度倍率（デフォルト 5.0）</summary>
    public static double CtrlMultiplier { get; private set; } = 5.0;

    /// <summary>
    /// NumPad 待ち受けを開始するキーの一覧（デフォルト Pause, Break）。
    /// config.toml の [hotkey] hotkeys に文字列配列で指定する。
    /// "Break" は Ctrl+Break が生成する VK_CANCEL に対応する。
    /// </summary>
    public static Keys[] PauseKeys { get; private set; } = { Keys.Pause, Keys.Cancel };

    /// <summary>待ち受けキーの表示用文字列（例: "Pause/Break"）。</summary>
    public static string PauseKeysDisplay =>
        string.Join("/", PauseKeys.Select(KeysToConfigName));

    /// <summary>待ち受けキーの config.toml 用配列文字列（例: ["Pause", "Break"]）。</summary>
    private static string PauseKeysToml =>
        "[" + string.Join(", ", PauseKeys.Select(k => $"\"{KeysToConfigName(k)}\"")) + "]";

    /// <summary>
    /// ウィンドウ移動モードを起動するトリガーキー（デフォルト M）。
    /// config.toml の [hotkey] move_key に文字列で指定する。
    /// </summary>
    public static Keys MoveKey { get; private set; } = Keys.M;

    /// <summary>
    /// ウィンドウリサイズモードを起動するトリガーキー（デフォルト R）。
    /// config.toml の [hotkey] resize_key に文字列で指定する。
    /// </summary>
    public static Keys ResizeKey { get; private set; } = Keys.R;

    /// <summary>
    /// ウィンドウを空きエリアへフィットさせるキー（デフォルト F）。
    /// config.toml の [hotkey] fit_key に文字列で指定する。
    /// </summary>
    public static Keys FitKey { get; private set; } = Keys.F;

    /// <summary>
    /// ウィンドウの最前面（Topmost）固定/解除をトグルするキー（デフォルト T）。
    /// config.toml の [hotkey] topmost_key に文字列で指定する。
    /// </summary>
    public static Keys TopmostKey { get; private set; } = Keys.T;

    /// <summary>
    /// ウィンドウの Z-order を最背面（最下位）へ移動するキー（デフォルト B）。
    /// config.toml の [hotkey] bottom_key に文字列で指定する。
    /// </summary>
    public static Keys BottomKey { get; private set; } = Keys.B;

    /// <summary>
    /// 対象ウィンドウを最小化するキー（デフォルト I）。
    /// config.toml の [hotkey] minimize_key に文字列で指定する。
    /// </summary>
    public static Keys MinimizeKey { get; private set; } = Keys.I;

    /// <summary>
    /// 対象ウィンドウを最大化（トグル）するキー（デフォルト A）。
    /// config.toml の [hotkey] maximize_key に文字列で指定する。
    /// </summary>
    public static Keys MaximizeKey { get; private set; } = Keys.A;

    /// <summary>
    /// 対象ウィンドウを閉じるキー（デフォルト X）。
    /// config.toml の [hotkey] close_key に文字列で指定する。
    /// </summary>
    public static Keys CloseKey { get; private set; } = Keys.X;

    /// <summary>
    /// ウィンドウスナップ選択 UI を起動するトリガーキー（デフォルト S）。
    /// config.toml の [hotkey] snap_key に文字列で指定する。
    /// </summary>
    public static Keys SnapKey { get; private set; } = Keys.S;

    /// <summary>
    /// 辺拡張モードを起動するトリガーキー（デフォルト E）。
    /// config.toml の [hotkey] edge_key に文字列で指定する。
    /// </summary>
    public static Keys EdgeKey { get; private set; } = Keys.E;

    /// <summary>
    /// 2つのウィンドウの位置とサイズを入れ替えるモードを起動するトリガーキー（デフォルト W）。
    /// config.toml の [hotkey] swap_key に文字列で指定する。
    /// </summary>
    public static Keys SwapKey { get; private set; } = Keys.W;

    /// <summary>
    /// Window Stack モードを起動するトリガーキー（デフォルト K）。
    /// config.toml の [hotkey] stack_key に文字列で指定する。
    /// </summary>
    public static Keys StackKey { get; private set; } = Keys.K;

    /// <summary>
    /// Window Set 機能を呼び出すトリガーキー（デフォルト G）。
    /// config.toml の [hotkey] window_set_key に文字列で指定する。
    /// </summary>
    public static Keys WindowSetKey { get; private set; } = Keys.G;

    /// <summary>
    /// 登録済みの Window Set 一覧（呼び出しキー → アプリ情報リスト）。
    /// config.toml の [[window_set.&lt;key&gt;]] から読み込まれる。
    /// </summary>
    private static readonly Dictionary<Keys, List<WindowSetApp>> _windowSets = new();
    public static IReadOnlyDictionary<Keys, List<WindowSetApp>> WindowSets => _windowSets;

    /// <summary>
    /// デスクトップ端との余白（上, 右, 下, 左 の順、px）。
    /// config.toml の [window_fit] outer_spacing に配列で指定する。
    /// </summary>
    public static int[] OuterSpacing { get; private set; } = { 10, 10, 10, 10 };

    /// <summary>
    /// ウィンドウ間の余白（px）。
    /// config.toml の [window_fit] inner_spacing に整数で指定する。
    /// </summary>
    public static int InnerSpacing { get; private set; } = 10;

    /// <summary>
    /// Window Stack タブバーの高さ（px、デフォルト 28、最小 16）。
    /// config.toml の [window_stack] tab_bar_height に整数で指定する。
    /// </summary>
    public static int StackTabBarHeight { get; private set; } = 28;

    /// <summary>
    /// Window Stack タブバーの選択中タブ背景色（null の場合は Windows アクセントカラー）。
    /// config.toml の [window_stack] selected_color に "#RRGGBB" または "accent" で指定する。
    /// </summary>
    public static Color? StackSelectedColor { get; private set; } = null;

    /// <summary>
    /// Window Stack タブバーの非選択タブ背景色（デフォルト #2D2D2D）。
    /// config.toml の [window_stack] background_color に "#RRGGBB" で指定する。
    /// </summary>
    public static Color StackBackgroundColor { get; private set; } = Color.FromArgb(45, 45, 45);

    /// <summary>
    /// Window Stack タブバーのホバー中タブ背景色（デフォルト #444444）。
    /// config.toml の [window_stack] hover_color に "#RRGGBB" で指定する。
    /// </summary>
    public static Color StackHoverColor { get; private set; } = Color.FromArgb(68, 68, 68);

    /// <summary>
    /// Window Stack タブバーの不透明度（0.1〜1.0、デフォルト 1.0）。
    /// config.toml の [window_stack] opacity に小数で指定する。
    /// </summary>
    public static double StackOpacity { get; private set; } = 1.0;

    /// <summary>Window Stack タブボタンの上マージン（px、デフォルト 0）。</summary>
    public static int StackButtonMarginTop { get; private set; } = 0;

    /// <summary>Window Stack タブボタンの下マージン（px、デフォルト 0）。</summary>
    public static int StackButtonMarginBottom { get; private set; } = 0;

    /// <summary>Window Stack タブボタンの左マージン（px、デフォルト 0）。</summary>
    public static int StackButtonMarginLeft { get; private set; } = 0;

    /// <summary>Window Stack タブボタンの右マージン（px、デフォルト 0）。</summary>
    public static int StackButtonMarginRight { get; private set; } = 0;

    /// <summary>Window Stack タブボタンの角を丸めるか（デフォルト false）。</summary>
    public static bool StackButtonRounded { get; private set; } = false;

    /// <summary>Window Stack タブボタンの角丸半径（px、デフォルト 6）。</summary>
    public static int StackButtonCornerRadius { get; private set; } = 6;

    /// <summary>Window Stack タブバーの外周枠を描画するか（デフォルト true）。</summary>
    public static bool StackTabBarBorder { get; private set; } = true;

    /// <summary>
    /// Window Stack タブバーウィンドウの背景色（RGBA、null の場合は background_color から自動算出）。
    /// config.toml の [window_stack] tab_bar_bg_rgba に "#RRGGBBAA" で指定する。
    /// </summary>
    public static Color? StackTabBarBgColor { get; private set; } = null;

    // -----------------------------------------------------------------------
    // パス
    // -----------------------------------------------------------------------

    private static string ConfigDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "cs-mouse-ctrl");

    public static string ConfigPath => Path.Combine(ConfigDir, "config.toml");

    // -----------------------------------------------------------------------
    // デフォルト TOML テンプレート
    // -----------------------------------------------------------------------

    private static string DefaultToml => $"""
        # cs-mouse-ctrl 設定ファイル
        # 変更後はアプリを再起動してください。

        [speed_multiplier]
        # Alt キーを押しながらマウス移動したときの速度倍率（数値。0.5で半分に低下、2で2倍など）
        alt = {AltMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)}
        # Ctrl キーを押しながらマウス移動したときの速度倍率（数値。0.5で半分に低下、5で5倍など）
        ctrl = {CtrlMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)}

        [hotkey]
        # NumPad 待ち受けを起動するキー名の配列（Windows Forms の Keys 列挙体の名前）
        # 例: ["Pause", "Break"], ["F12"], ["ScrollLock"]
        # "Break" は Ctrl+Break に対応します。
        hotkeys = {PauseKeysToml}

        # ウィンドウ移動モードを起動するトリガーキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "M"
        move_key = "{KeysToString(MoveKey)}"

        # ウィンドウリサイズモードを起動するトリガーキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "R", "F10", "F9"
        resize_key = "{KeysToString(ResizeKey)}"

        # ウィンドウを空きエリアへフィットさせるキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "F", "F8"
        fit_key = "{KeysToString(FitKey)}"

        # ウィンドウの最前面（Topmost）固定/解除をトグルするキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "T"
        topmost_key = "{KeysToString(TopmostKey)}"

        # ウィンドウの Z-order を最背面（最下位）へ移動するキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "B"
        bottom_key = "{KeysToString(BottomKey)}"

        # 対象ウィンドウを最小化するキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "I"
        minimize_key = "{KeysToString(MinimizeKey)}"

        # 対象ウィンドウを最大化するキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "A"
        maximize_key = "{KeysToString(MaximizeKey)}"

        # 対象ウィンドウを閉じるキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "X"
        close_key = "{KeysToString(CloseKey)}"

        # ウィンドウスナップ選択 UI を起動するトリガーキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "S"
        snap_key = "{KeysToString(SnapKey)}"

        # 辺拡張モードを起動するトリガーキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "E"
        edge_key = "{KeysToString(EdgeKey)}"

        # 2つのウィンドウの位置とサイズを入れ替えるトリガーキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "W"
        swap_key = "{KeysToString(SwapKey)}"

        # Window Stack モードを起動するトリガーキー名
        # 例: "K"
        stack_key = "{KeysToString(StackKey)}"

        # Window Set 機能を呼び出すトリガーキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "G", "L"
        window_set_key = "{KeysToString(WindowSetKey)}"

        [window_fit]
        # デスクトップ端との余白（上, 右, 下, 左 の順、px）
        outer_spacing = [{OuterSpacing[0]}, {OuterSpacing[1]}, {OuterSpacing[2]}, {OuterSpacing[3]}]
        # ウィンドウ間の余白（px）
        inner_spacing = {InnerSpacing}

        [window_stack]
        # タブバーの高さ（px、最小 16）
        tab_bar_height = {StackTabBarHeight}
        # 選択中タブの色（"#RRGGBB" 形式、または "accent" で Windows アクセントカラー）
        selected_color = "{(StackSelectedColor.HasValue ? ColorToHex(StackSelectedColor.Value) : "accent")}"
        # 背景色（"#RRGGBB" 形式）
        background_color = "{ColorToHex(StackBackgroundColor)}"
        # ホバー色（"#RRGGBB" 形式）
        hover_color = "{ColorToHex(StackHoverColor)}"
        # 透過率 / 不透明度（0.1〜1.0、1.0 で不透明、値が小さいほど透過）
        opacity = {StackOpacity.ToString("0.##", CultureInfo.InvariantCulture)}
        # ボタンのマージン（px）
        button_margin_top = {StackButtonMarginTop}
        button_margin_bottom = {StackButtonMarginBottom}
        button_margin_left = {StackButtonMarginLeft}
        button_margin_right = {StackButtonMarginRight}
        # ボタンの角を丸めるか（true / false）
        button_rounded = {(StackButtonRounded ? "true" : "false")}
        # ボタンの角丸半径（px、button_rounded = true のときに有効）
        button_corner_radius = {StackButtonCornerRadius}
        # タブバー外周枠を描画するか（true / false）
        tab_bar_border = {(StackTabBarBorder ? "true" : "false")}
        # タブバーウィンドウの背景色（"#RRGGBBAA" 形式、AA=00 で完全透過・FF で不透明）
        tab_bar_bg_rgba = "{ColorToHexRgba(StackTabBarBgColor ?? Color.FromArgb(255, 30, 30, 30))}"
        """;

    // -----------------------------------------------------------------------
    // 公開 API
    // -----------------------------------------------------------------------

    /// <summary>
    /// config.toml を読み込む。ファイルが存在しない場合はデフォルト値で生成してから読む。
    /// </summary>
    public static void Load()
    {
        if (!File.Exists(ConfigPath))
            WriteDefault();

        try
        {
            Parse(File.ReadAllText(ConfigPath));
        }
        catch (Exception ex)
        {
            // 読み込み失敗時はデフォルト値のまま継続（ダイアログは出さない）
            System.Diagnostics.Debug.WriteLine($"[AppConfig] 読み込み失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// 指定した呼び出しキーに紐づく Window Set をメモリと config.toml に保存する。
    /// 既存のコメントや他セクションは保持したまま、同一キーの [[window_set.&lt;key&gt;]] のみを置き換える。
    /// </summary>
    public static bool SaveWindowSet(Keys key, IReadOnlyList<WindowSetApp> apps)
    {
        if (apps.Count == 0) return false;

        var copy = apps.Select(a => new WindowSetApp
        {
            AppPath = a.AppPath,
            WorkingDir = a.WorkingDir,
            Params = new List<string>(a.Params),
            X = a.X,
            Y = a.Y,
            Width = a.Width,
            Height = a.Height,
        }).ToList();

        _windowSets[key] = copy;

        try
        {
            if (!File.Exists(ConfigPath))
                WriteDefault();

            string existing = File.Exists(ConfigPath) ? File.ReadAllText(ConfigPath) : DefaultToml;
            List<string> kept = RemoveWindowSetLines(existing, key, out string lineEnding);

            string keyName = KeysToWindowSetName(key);
            foreach (var app in copy)
            {
                if (kept.Count > 0)
                    kept.Add("");
                kept.Add($"[[window_set.{keyName}]]");
                kept.Add($"app_path = \"{EscapeTomlString(app.AppPath)}\"");
                kept.Add($"working_dir = \"{EscapeTomlString(app.WorkingDir)}\"");
                kept.Add($"Params = {FormatTomlStringArray(app.Params)}");
                kept.Add($"x = {app.X}");
                kept.Add($"y = {app.Y}");
                kept.Add($"width = {app.Width}");
                kept.Add($"height = {app.Height}");
            }

            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(ConfigPath, string.Join(lineEnding, kept) + lineEnding);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppConfig] Window Set 保存失敗: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 指定した呼び出しキーに紐づく Window Set をメモリと config.toml から削除する。
    /// 既存のコメントや他セクション・他キーのエントリは保持する。
    /// </summary>
    public static bool DeleteWindowSet(Keys key)
    {
        if (!_windowSets.Remove(key))
            return false;

        try
        {
            if (!File.Exists(ConfigPath))
                return true;

            string existing = File.ReadAllText(ConfigPath);
            List<string> kept = RemoveWindowSetLines(existing, key, out string lineEnding);

            Directory.CreateDirectory(ConfigDir);
            string content = kept.Count > 0 ? string.Join(lineEnding, kept) + lineEnding : "";
            File.WriteAllText(ConfigPath, content);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppConfig] Window Set 削除失敗: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// TOML テキストから指定キーに対応する [[window_set.&lt;key&gt;]] ブロックをすべて取り除く。
    /// </summary>
    private static List<string> RemoveWindowSetLines(string existing, Keys key, out string lineEnding)
    {
        lineEnding = existing.Contains("\r\n") ? "\r\n" : "\n";
        string[] lines = existing.Replace("\r\n", "\n").Split('\n');

        var kept = new List<string>(lines.Length);
        bool skippingTarget = false;

        foreach (string rawLine in lines)
        {
            string trimmed = rawLine.Trim();
            if (TryParseWindowSetHeader(trimmed, out Keys headerKey))
            {
                bool isTarget = (headerKey == key);
                if (isTarget)
                {
                    skippingTarget = true;
                    while (kept.Count > 0 && string.IsNullOrWhiteSpace(kept[^1]))
                        kept.RemoveAt(kept.Count - 1);
                    continue;
                }
                else
                {
                    if (skippingTarget && kept.Count > 0 && !string.IsNullOrWhiteSpace(kept[^1]))
                        kept.Add("");
                    skippingTarget = false;
                }
            }
            else if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                if (skippingTarget && kept.Count > 0 && !string.IsNullOrWhiteSpace(kept[^1]))
                    kept.Add("");
                skippingTarget = false;
            }

            if (!skippingTarget)
                kept.Add(rawLine);
        }

        // 末尾の余分な空行をトリム
        while (kept.Count > 0 && string.IsNullOrWhiteSpace(kept[^1]))
            kept.RemoveAt(kept.Count - 1);

        return kept;
    }

    // -----------------------------------------------------------------------
    // 内部実装: デフォルト生成
    // -----------------------------------------------------------------------

    private static void WriteDefault()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(ConfigPath, DefaultToml);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppConfig] デフォルト生成失敗: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // 内部実装: 軽量 TOML パーサー
    // -----------------------------------------------------------------------
    // 対応範囲: セクション([section])・キー=値・文字列・整数・インライン配列
    // -----------------------------------------------------------------------

    private static void Parse(string toml)
    {
        _windowSets.Clear();
        string currentSection = "";
        WindowSetApp? currentWindowSetApp = null;

        foreach (string rawLine in toml.Split('\n'))
        {
            string line = rawLine.Trim();

            // コメント・空行をスキップ
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            // Window Set 配列テーブル: [[window_set.<key>]]
            if (TryParseWindowSetHeader(line, out Keys setKey))
            {
                currentSection = "";
                if (!_windowSets.TryGetValue(setKey, out var list))
                {
                    list = new List<WindowSetApp>();
                    _windowSets[setKey] = list;
                }
                currentWindowSetApp = new WindowSetApp();
                list.Add(currentWindowSetApp);
                continue;
            }

            // セクション
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentWindowSetApp = null;
                currentSection = line[1..^1].Trim();
                continue;
            }

            // key = value
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;

            string key   = line[..eq].Trim();
            string value = StripComment(line[(eq + 1)..]).Trim();

            if (currentWindowSetApp != null)
            {
                ApplyWindowSetValue(currentWindowSetApp, key, value);
                continue;
            }

            ApplyValue(currentSection, key, value);
        }

        // app_path が空の不完全なエントリを除外し、空リストのキーも整理
        foreach (Keys k in _windowSets.Keys.ToList())
        {
            _windowSets[k].RemoveAll(a => string.IsNullOrWhiteSpace(a.AppPath));
            if (_windowSets[k].Count == 0)
                _windowSets.Remove(k);
        }
    }

    private static void ApplyWindowSetValue(WindowSetApp app, string key, string value)
    {
        switch (key.ToLowerInvariant())
        {
            case "app_path":
                app.AppPath = UnescapeTomlString(value);
                break;
            case "working_dir":
                app.WorkingDir = UnescapeTomlString(value);
                break;
            case "params":
                if (TryParseStringArray(value, out List<string> parsedParams))
                    app.Params = parsedParams;
                break;
            case "x":
                if (int.TryParse(value, out int x)) app.X = x;
                break;
            case "y":
                if (int.TryParse(value, out int y)) app.Y = y;
                break;
            case "width":
                if (int.TryParse(value, out int w)) app.Width = w;
                break;
            case "height":
                if (int.TryParse(value, out int h)) app.Height = h;
                break;
        }
    }

    private static void ApplyValue(string section, string key, string value)
    {
        switch (section)
        {
            case "speed_multiplier":
                if ((key == "alt" || key == "shift") && TryParseMultiplier(value, out double av))
                    AltMultiplier = av;
                if (key == "ctrl" && TryParseMultiplier(value, out double cv))
                    CtrlMultiplier = cv;
                break;

            case "hotkey":
                if (key == "hotkeys")
                {
                    if (TryParseKeysArray(value, out Keys[] keys) && keys.Length > 0)
                        PauseKeys = keys;
                }
                // 後方互換: 旧 pause_key（単一キー）
                if (key == "pause_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        PauseKeys = new[] { k };
                }
                if (key == "move_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        MoveKey = k;
                }
                if (key == "resize_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        ResizeKey = k;
                }
                if (key == "fit_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        FitKey = k;
                }
                if (key == "topmost_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        TopmostKey = k;
                }
                if (key == "bottom_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        BottomKey = k;
                }
                if (key == "minimize_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        MinimizeKey = k;
                }
                if (key == "maximize_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        MaximizeKey = k;
                }
                if (key == "close_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        CloseKey = k;
                }
                if (key == "edge_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        EdgeKey = k;
                }
                if (key == "snap_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        SnapKey = k;
                }
                if (key == "swap_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        SwapKey = k;
                }
                if (key == "stack_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        StackKey = k;
                }
                if (key == "window_set_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        WindowSetKey = k;
                }
                break;

            case "window_fit":
                if (key == "outer_spacing"
                    && TryParseIntArray(value, out int[] outer) && outer.Length == 4)
                {
                    OuterSpacing = outer;
                }
                if (key == "inner_spacing"
                    && int.TryParse(value, out int inner) && inner >= 0)
                {
                    InnerSpacing = inner;
                }
                break;

            case "window_stack":
                switch (key)
                {
                    case "tab_bar_height":
                        if (int.TryParse(value, out int tbh))
                            StackTabBarHeight = Math.Max(16, tbh);
                        break;
                    case "selected_color":
                        {
                            string raw = UnquoteString(value).Trim();
                            if (raw.Length == 0 || string.Equals(raw, "accent", StringComparison.OrdinalIgnoreCase))
                                StackSelectedColor = null;
                            else if (TryParseColor(raw, out Color sc))
                                StackSelectedColor = sc;
                        }
                        break;
                    case "background_color":
                        if (TryParseColor(UnquoteString(value), out Color bgc))
                            StackBackgroundColor = bgc;
                        break;
                    case "hover_color":
                        if (TryParseColor(UnquoteString(value), out Color hc))
                            StackHoverColor = hc;
                        break;
                    case "opacity":
                        if (TryParseOpacity(value, out double op))
                            StackOpacity = op;
                        break;
                    case "button_margin_top":
                        if (int.TryParse(value, out int mt) && mt >= 0)
                            StackButtonMarginTop = mt;
                        break;
                    case "button_margin_bottom":
                        if (int.TryParse(value, out int mb) && mb >= 0)
                            StackButtonMarginBottom = mb;
                        break;
                    case "button_margin_left":
                        if (int.TryParse(value, out int ml) && ml >= 0)
                            StackButtonMarginLeft = ml;
                        break;
                    case "button_margin_right":
                        if (int.TryParse(value, out int mr) && mr >= 0)
                            StackButtonMarginRight = mr;
                        break;
                    case "button_margin":
                        if (TryParseIntArray(value, out int[] margins) && margins.Length == 4
                            && margins.All(m => m >= 0))
                        {
                            StackButtonMarginTop    = margins[0];
                            StackButtonMarginRight  = margins[1];
                            StackButtonMarginBottom = margins[2];
                            StackButtonMarginLeft   = margins[3];
                        }
                        break;
                    case "button_rounded":
                        if (bool.TryParse(UnquoteString(value).Trim(), out bool br))
                            StackButtonRounded = br;
                        break;
                    case "button_corner_radius":
                        if (int.TryParse(value, out int rad) && rad >= 0)
                            StackButtonCornerRadius = rad;
                        break;
                    case "tab_bar_border":
                        if (bool.TryParse(UnquoteString(value).Trim(), out bool tbb))
                            StackTabBarBorder = tbb;
                        break;
                    case "tab_bar_bg_rgba":
                        {
                            string raw = UnquoteString(value).Trim();
                            if (raw.Length == 0 || string.Equals(raw, "auto", StringComparison.OrdinalIgnoreCase))
                                StackTabBarBgColor = null;
                            else if (TryParseColorRgba(raw, out Color barBg))
                                StackTabBarBgColor = barBg;
                        }
                        break;
                }
                break;
        }
    }

    // -----------------------------------------------------------------------
    // ユーティリティ
    // -----------------------------------------------------------------------

    /// <summary>行末コメント（# 以降）を除去する。文字列リテラル内の # は除去しない。</summary>
    private static string StripComment(string s)
    {
        bool inDouble = false;
        bool inSingle = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (inDouble)
            {
                if (c == '\\' && i + 1 < s.Length) { i++; continue; }
                if (c == '"') inDouble = false;
            }
            else if (inSingle)
            {
                if (c == '\'') inSingle = false;
            }
            else
            {
                if (c == '"') inDouble = true;
                else if (c == '\'') inSingle = true;
                else if (c == '#') return s[..i];
            }
        }
        return s;
    }

    /// <summary>"value" → value</summary>
    private static string UnquoteString(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 && ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\'')))
            return s[1..^1];
        return s;
    }

    /// <summary>文字列リストを TOML インライン配列（例: [ "param1", "param2" ] / []）へ整形する。</summary>
    private static string FormatTomlStringArray(IReadOnlyList<string> items)
    {
        if (items.Count == 0) return "[]";
        return "[ " + string.Join(", ", items.Select(s => $"\"{EscapeTomlString(s)}\"")) + " ]";
    }

    /// <summary>TOML 文字列配列（例: [ "param1", "param2" ] / []）をパースする。</summary>
    private static bool TryParseStringArray(string s, out List<string> result)
    {
        result = new List<string>();
        s = s.Trim();
        if (s.Length < 2 || s[0] != '[' || s[^1] != ']')
            return false;

        int i = 1;
        int end = s.Length - 1;

        while (i < end)
        {
            while (i < end && char.IsWhiteSpace(s[i])) i++;
            if (i >= end) break;

            if (s[i] == '"')
            {
                int start = i;
                i++;
                bool closed = false;
                while (i < end)
                {
                    if (s[i] == '\\' && i + 1 < end)
                    {
                        i += 2;
                        continue;
                    }
                    if (s[i] == '"')
                    {
                        i++;
                        closed = true;
                        break;
                    }
                    i++;
                }
                if (!closed) return false;
                result.Add(UnescapeTomlString(s[start..i]));
            }
            else if (s[i] == '\'')
            {
                int start = i;
                i++;
                int closeIdx = s.IndexOf('\'', i);
                if (closeIdx < 0 || closeIdx >= end) return false;
                i = closeIdx + 1;
                result.Add(UnescapeTomlString(s[start..i]));
            }
            else
            {
                // クォートなし要素にも寛容に対応
                int comma = s.IndexOf(',', i);
                int tokenEnd = (comma < 0 || comma >= end) ? end : comma;
                string token = s[i..tokenEnd].Trim();
                if (token.Length > 0)
                    result.Add(token);
                i = tokenEnd;
            }

            while (i < end && char.IsWhiteSpace(s[i])) i++;
            if (i < end)
            {
                if (s[i] == ',') i++;
                else return false;
            }
        }

        return true;
    }

    /// <summary>TOML 基本文字列（"..."）用にエスケープする。</summary>
    private static string EscapeTomlString(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
    }

    /// <summary>TOML 文字列リテラル（基本文字列 "..." / リテラル文字列 '...'）をアンエスケープする。</summary>
    private static string UnescapeTomlString(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 && s[0] == '\'' && s[^1] == '\'')
            return s[1..^1];

        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
        {
            string inner = s[1..^1];
            var sb = new StringBuilder(inner.Length);
            for (int i = 0; i < inner.Length; i++)
            {
                if (inner[i] == '\\' && i + 1 < inner.Length)
                {
                    char next = inner[i + 1];
                    switch (next)
                    {
                        case '\\': sb.Append('\\'); i++; break;
                        case '"':  sb.Append('"');  i++; break;
                        case 'n':  sb.Append('\n'); i++; break;
                        case 'r':  sb.Append('\r'); i++; break;
                        case 't':  sb.Append('\t'); i++; break;
                        default:
                            // 手動編集で未エスケープの Windows パス (\W 等) が書かれた場合はそのまま保持
                            sb.Append('\\');
                            break;
                    }
                }
                else
                {
                    sb.Append(inner[i]);
                }
            }
            return sb.ToString();
        }

        return s;
    }

    /// <summary>[[window_set.&lt;key&gt;]] ヘッダを解析する。</summary>
    private static bool TryParseWindowSetHeader(string trimmedLine, out Keys key)
    {
        key = Keys.None;
        string cleaned = StripComment(trimmedLine).Trim();
        if (!cleaned.StartsWith("[[") || !cleaned.EndsWith("]]"))
            return false;

        string inner = cleaned[2..^2].Trim();
        const string prefix = "window_set.";
        if (!inner.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string keyPart = UnquoteString(inner[prefix.Length..].Trim());
        return TryParseWindowSetKey(keyPart, out key);
    }

    /// <summary>Window Set 呼び出しキー名を Keys へ変換する（"0".."9" → D0..D9 対応）。</summary>
    public static bool TryParseWindowSetKey(string name, out Keys key)
    {
        name = name.Trim();
        if (name.Length == 1 && name[0] >= '0' && name[0] <= '9')
        {
            key = Keys.D0 + (name[0] - '0');
            return true;
        }
        return TryParseKeys(name, out key);
    }

    /// <summary>Window Set の呼び出しキーを表示・config.toml 用の短いキー名へ変換する（D0..D9 → "0".."9"）。</summary>
    public static string KeysToWindowSetName(Keys k)
    {
        if (k >= Keys.D0 && k <= Keys.D9)
            return ((char)('0' + (k - Keys.D0))).ToString();
        return KeysToConfigName(k);
    }

    /// <summary>速度倍率の文字列（"2", "0.5" 等）を double へパースする（> 0）。</summary>
    private static bool TryParseMultiplier(string s, out double result)
    {
        return double.TryParse(s.Trim().Replace(',', '.'),
                               NumberStyles.Float,
                               CultureInfo.InvariantCulture,
                               out result) && result > 0 && !double.IsInfinity(result) && !double.IsNaN(result);
    }

    /// <summary>不透明度の文字列（"0.85", "1.0" 等）を double へパースする（0.1〜1.0 にクランプ）。</summary>
    private static bool TryParseOpacity(string s, out double result)
    {
        if (!double.TryParse(s.Trim().Replace(',', '.'),
                             NumberStyles.Float,
                             CultureInfo.InvariantCulture,
                             out double parsed)
            || double.IsInfinity(parsed) || double.IsNaN(parsed) || parsed <= 0)
        {
            result = 1.0;
            return false;
        }
        result = Math.Clamp(parsed, 0.1, 1.0);
        return true;
    }

    /// <summary>色文字列（"#RRGGBB", "#RGB", 既知の色名）を Color へパースする。</summary>
    private static bool TryParseColor(string s, out Color color)
    {
        color = Color.Empty;
        s = s.Trim();
        if (s.Length == 0) return false;

        string hex = s.StartsWith('#') ? s[1..] : s;
        if (hex.Length == 6
            && int.TryParse(hex[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r)
            && int.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g)
            && int.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
        {
            color = Color.FromArgb(255, r, g, b);
            return true;
        }
        if (hex.Length == 3
            && int.TryParse(hex[0..1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r3)
            && int.TryParse(hex[1..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g3)
            && int.TryParse(hex[2..3], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b3))
        {
            color = Color.FromArgb(255, r3 * 17, g3 * 17, b3 * 17);
            return true;
        }

        Color named = Color.FromName(s);
        if (named.IsKnownColor)
        {
            color = Color.FromArgb(255, named.R, named.G, named.B);
            return true;
        }
        return false;
    }

    private static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private static string ColorToHexRgba(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";

    /// <summary>RGBA 色文字列（"#RRGGBBAA", "#RGBA", "#RRGGBB", "#RGB", 既知の色名）を Color へパースする。</summary>
    private static bool TryParseColorRgba(string s, out Color color)
    {
        color = Color.Empty;
        s = s.Trim();
        if (s.Length == 0) return false;

        string hex = s.StartsWith('#') ? s[1..] : s;
        if (hex.Length == 8
            && int.TryParse(hex[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r)
            && int.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g)
            && int.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b)
            && int.TryParse(hex[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int a))
        {
            color = Color.FromArgb(a, r, g, b);
            return true;
        }
        if (hex.Length == 4
            && int.TryParse(hex[0..1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r4)
            && int.TryParse(hex[1..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g4)
            && int.TryParse(hex[2..3], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b4)
            && int.TryParse(hex[3..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int a4))
        {
            color = Color.FromArgb(a4 * 17, r4 * 17, g4 * 17, b4 * 17);
            return true;
        }

        return TryParseColor(s, out color);
    }

    private static bool TryParseKeys(string name, out Keys key)
    {
        // "Break" は Windows Forms の Keys 列挙体に存在しないため、
        // Ctrl+Break が生成する VK_CANCEL（Keys.Cancel）へ対応付ける。
        if (string.Equals(name, "Break", StringComparison.OrdinalIgnoreCase))
        {
            key = Keys.Cancel;
            return true;
        }
        return Enum.TryParse(name, ignoreCase: true, out key);
    }

    /// <summary>"[\"Pause\", \"Break\"]" → Keys[] { Keys.Pause, Keys.Cancel }</summary>
    private static bool TryParseKeysArray(string s, out Keys[] result)
    {
        result = Array.Empty<Keys>();
        s = s.Trim();
        if (s.Length < 2 || s[0] != '[' || s[^1] != ']')
            return false;

        string inner = s[1..^1];
        string[] parts = inner.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return false;

        var keys = new List<Keys>(parts.Length);
        foreach (string part in parts)
        {
            if (!TryParseKeys(UnquoteString(part), out Keys k))
                return false;
            keys.Add(k);
        }

        result = keys.ToArray();
        return true;
    }

    /// <summary>"[10, 10, 10, 10]" → int[] { 10, 10, 10, 10 }</summary>
    private static bool TryParseIntArray(string s, out int[] result)
    {
        result = Array.Empty<int>();
        s = s.Trim();
        if (s.Length < 2 || s[0] != '[' || s[^1] != ']')
            return false;

        string inner = s[1..^1];
        string[] parts = inner.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return false;

        int[] values = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out values[i]))
                return false;
        }

        result = values;
        return true;
    }

    private static string KeysToString(Keys k) => k.ToString();

    /// <summary>Keys を config.toml 用のキー名へ変換する（Break は "Break"）。</summary>
    private static string KeysToConfigName(Keys k) =>
        k == Keys.Cancel ? "Break" : k.ToString();
}

