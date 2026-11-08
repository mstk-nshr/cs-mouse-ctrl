using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

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
    /// デスクトップ端との余白（上, 右, 下, 左 の順、px）。
    /// config.toml の [window_fit] outer_spacing に配列で指定する。
    /// </summary>
    public static int[] OuterSpacing { get; private set; } = { 10, 10, 10, 10 };

    /// <summary>
    /// ウィンドウ間の余白（px）。
    /// config.toml の [window_fit] inner_spacing に整数で指定する。
    /// </summary>
    public static int InnerSpacing { get; private set; } = 10;

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

        [window_fit]
        # デスクトップ端との余白（上, 右, 下, 左 の順、px）
        outer_spacing = [{OuterSpacing[0]}, {OuterSpacing[1]}, {OuterSpacing[2]}, {OuterSpacing[3]}]
        # ウィンドウ間の余白（px）
        inner_spacing = {InnerSpacing}
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
        string currentSection = "";

        foreach (string rawLine in toml.Split('\n'))
        {
            string line = rawLine.Trim();

            // コメント・空行をスキップ
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            // セクション
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].Trim();
                continue;
            }

            // key = value
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;

            string key   = line[..eq].Trim();
            string value = StripComment(line[(eq + 1)..]).Trim();

            ApplyValue(currentSection, key, value);
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
                if (key == "snap_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        SnapKey = k;
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
        }
    }

    // -----------------------------------------------------------------------
    // ユーティリティ
    // -----------------------------------------------------------------------

    /// <summary>行末コメント（# 以降）を除去する。文字列リテラル内の # は除去しない。</summary>
    private static string StripComment(string s)
    {
        bool inStr = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '"') inStr = !inStr;
            if (!inStr && s[i] == '#') return s[..i];
        }
        return s;
    }

    /// <summary>"value" → value</summary>
    private static string UnquoteString(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
            return s[1..^1];
        return s;
    }

    /// <summary>速度倍率の文字列（"2", "0.5" 等）を double へパースする（> 0）。</summary>
    private static bool TryParseMultiplier(string s, out double result)
    {
        return double.TryParse(s.Trim().Replace(',', '.'),
                               System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture,
                               out result) && result > 0 && !double.IsInfinity(result) && !double.IsNaN(result);
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

