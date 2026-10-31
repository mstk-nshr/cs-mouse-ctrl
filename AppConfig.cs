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

    /// <summary>Shift + Mouse Move の速度倍率（デフォルト 2）</summary>
    public static int ShiftMultiplier { get; private set; } = 2;

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
        # Shift キーを押しながらマウス移動したときの速度倍率（整数）
        shift = {ShiftMultiplier}
        # Ctrl キーを押しながらマウス移動したときの速度倍率（整数）
        ctrl = {CtrlMultiplier}

        [hotkey]
        # NumPad 待ち受けを起動するキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "Pause", "F12", "ScrollLock"
        pause_key = "{KeysToString(PauseKey)}"

        # ウィンドウリサイズモードを起動するトリガーキー名（Windows Forms の Keys 列挙体の名前）
        # 例: "F11", "F10", "F9"
        resize_key = "{KeysToString(ResizeKey)}"
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
                if (key == "shift" && int.TryParse(value, out int sv) && sv >= 1)
                    ShiftMultiplier = sv;
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

    private static string KeysToString(Keys k) => k.ToString();
}

