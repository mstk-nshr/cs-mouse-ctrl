using System;
using System.IO;
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

    /// <summary>Alt + Mouse Move の速度倍率（デフォルト 2）</summary>
    public static int AltMultiplier { get; private set; } = 2;

    /// <summary>互換用エイリアス</summary>
    public static int ShiftMultiplier => AltMultiplier;

    /// <summary>Ctrl + Mouse Move の速度倍率（デフォルト 5）</summary>
    public static int CtrlMultiplier { get; private set; } = 5;

    /// <summary>
    /// NumPad 待ち受けを開始するキー（デフォルト Pause）。
    /// config.toml の [hotkey] pause_key に文字列で指定する。
    /// </summary>
    public static Keys PauseKey { get; private set; } = Keys.Pause;

    /// <summary>
    /// ウィンドウリサイズモードを起動するトリガーキー（デフォルト F11）。
    /// config.toml の [hotkey] resize_key に文字列で指定する。
    /// </summary>
    public static Keys ResizeKey { get; private set; } = Keys.F11;

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
        # Alt キーを押しながらマウス移動したときの速度倍率（整数）
        alt = {AltMultiplier}
        # Ctrl キーを押しながらマウス移動したときの速度倍率（整数）
        ctrl = {CtrlMultiplier}

        [hotkey]
        # NumPad 待ち受けを起動するキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "Pause", "F12", "ScrollLock"
        pause_key = "{KeysToString(PauseKey)}"

        # ウィンドウリサイズモードを起動するトリガーキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "F11", "F10", "F9"
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
                if ((key == "alt" || key == "shift") && int.TryParse(value, out int av) && av >= 1)
                    AltMultiplier = av;
                if (key == "ctrl"  && int.TryParse(value, out int cv) && cv >= 1)
                    CtrlMultiplier = cv;
                break;

            case "hotkey":
                if (key == "pause_key")
                {
                    string name = UnquoteString(value);
                    if (TryParseKeys(name, out Keys k))
                        PauseKey = k;
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

    private static bool TryParseKeys(string name, out Keys key)
    {
        return Enum.TryParse(name, ignoreCase: true, out key);
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
}

