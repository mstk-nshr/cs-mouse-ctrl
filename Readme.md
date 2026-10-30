# Mouse Utility

## Feature

| Key | Description |
| --- | ----------- |
| Ctrl + Alt + Win + Numpad1..9 | move mouse pointer (window 9-positions) |
| Pause -> Numpad1..9 | move mouse pointer (window 9-positions, Pause again to cancel) |
| Alt + Win + F11 | resize active window. |
| Shift + Mouse Move | move mouse pointer at 2× speed |
| Ctrl + Mouse Move | move mouse pointer at 5× speed |

### move-mouse-pointer 

`Ctrl + Alt + Win` を押下している間、または `Pause` キーを押した後、カーソル位置にあるウィンドウに対象が固定（カーソルがウィンドウ内にクリップ）され、その状態でテンキー（NumPad 1〜9）を押すことで、ウィンドウ内の 9 箇所の位置へマウスポインターを瞬時に移動させることができます。

- **Pauseキー方式（モーダル入力）**:
  - `Pause` キーを押すとテンキー待ち状態に入り、カーソルが対象ウィンドウ内に固定されます。
  - 続けて `NumPad 1〜9` を押すと該当位置へカーソルが移動し、待ち状態が終了します。
  - 待ち状態中にもう一度 `Pause` キー（または `Esc` キー）を押すと、移動を行わずにテンキー待ちをキャンセルします。

#### テンキー配置と移動位置の対応

```
+---------------+---------------+---------------+
| 7: 左上       | 8: 上中央     | 9: 右上       |
+---------------+---------------+---------------+
| 4: 左中央     | 5: 中央       | 6: 右中央     |
+---------------+---------------+---------------+
| 1: 左下       | 2: 下中央     | 3: 右下       |
+---------------+---------------+---------------+
```

| キー | 移動位置 | 計算座標（ウィンドウ矩形内） | 説明 |
| :--- | :--- | :--- | :--- |
| **Ctrl + Alt + NumPad 7** | 左上 (Top-Left) | `(Left + 10, Top + 10)` | ウィンドウの左上隅（タイトルバー左側付近）へ移動 |
| **Ctrl + Alt + NumPad 8** | 上中央 (Top-Center) | `(CenterX, Top + 10)` | ウィンドウの上辺中央（タイトルバー中央付近）へ移動 |
| **Ctrl + Alt + NumPad 9** | 右上 (Top-Right) | `(Right - 10, Top + 10)` | ウィンドウの右上隅（閉じるボタン付近）へ移動 |
| **Ctrl + Alt + NumPad 4** | 左中央 (Middle-Left) | `(Left + 10, CenterY)` | ウィンドウの左辺中央へ移動 |
| **Ctrl + Alt + NumPad 5** | 中央 (Center) | `(CenterX, CenterY)` | ウィンドウの真ん中へ移動 |
| **Ctrl + Alt + NumPad 6** | 右中央 (Middle-Right) | `(Right - 10, CenterY)` | ウィンドウの右辺中央へ移動 |
| **Ctrl + Alt + NumPad 1** | 左下 (Bottom-Left) | `(Left + 10, Bottom - 10)` | ウィンドウの左下隅へ移動 |
| **Ctrl + Alt + NumPad 2** | 下中央 (Bottom-Center) | `(CenterX, Bottom - 10)` | ウィンドウの下辺中央へ移動 |
| **Ctrl + Alt + NumPad 3** | 右下 (Bottom-Right) | `(Right - 10, Bottom - 10)` | ウィンドウの右下隅へ移動 |

> [!NOTE]
> - **カーソルクリップ機能**: `Ctrl + Alt` を押している間はマウスカーソルの移動可能範囲が対象ウィンドウ内に制限（`ClipCursor`）されます。キーを離すと自動的に制限が解除されます。
> - **マージン**: 各隅の座標はウィンドウ境界の外に出てしまわないよう、境界から内側へ 10px のオフセットが自動適用されます。


### 高速マウス移動

`Shift` または `Ctrl` キーを押しながらマウスを動かすと、移動量が増幅されマウスポインターが高速に移動します。

| キー | 倍率 | 説明 |
| :--- | :---: | :--- |
| **Shift** + マウス移動 | **2倍** | 通常の移動量を 2 倍に増幅 |
| **Ctrl** + マウス移動 | **5倍** | 通常の移動量を 5 倍に増幅 |
| **Ctrl + Shift** + マウス移動 | **5倍** | Ctrl が優先される |

> [!NOTE]
> - `Ctrl + Alt` の組み合わせは既存の **カーソルクリップ機能** に割り当てられているため、高速移動は適用されません。
> - 高速移動中に画面端を超えようとした場合は、仮想スクリーンの端にクランプされます。
> - キーを離すと即座に通常速度に戻ります。


### Resize-Active-Window 動作フロー

```
Alt+Win+F11 押下
  ↓ フォアグラウンドウィンドウ取得・デスクトップ/タスクバー除外チェック
  ↓ 半透明青色オーバーレイ表示
  ↓ マウス→BOX左上へ移動
[TopLeft フェーズ]
  マウス移動 → オーバーレイをリアルタイム再描画
  左クリック → 左上座標確定・マウス→BOX右下へ移動
[BottomRight フェーズ]
  マウス移動 → オーバーレイをリアルタイム再描画
  左クリック → 右下座標確定・MoveWindow() でウィンドウリサイズ
[終了条件]
  左クリック以外のマウスイベント → モード即時終了・オーバーレイ消去
```


## Release ビルド & 実行方法

### ビルド

```powershell
cd h:\prj\cs-mouse-ctrl
dotnet build -c Release
```

または、単体の `.exe` として配布したい場合（依存なし・単一ファイル）:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### 出力先

| コマンド | 出力先 |
|---|---|
| `dotnet build -c Release` | `bin\Release\net9.0-windows\cs-mouse-ctrl.exe` |
| `dotnet publish ...` | `bin\Release\net9.0-windows\win-x64\publish\cs-mouse-ctrl.exe` |

### 実行

```powershell
# build した場合
h:\prj\cs-mouse-ctrl\bin\Release\net9.0-windows\cs-mouse-ctrl.exe

# publish した場合
h:\prj\cs-mouse-ctrl\bin\Release\net9.0-windows\win-x64\publish\cs-mouse-ctrl.exe
```

> [!TIP]
> このツールはキーボード・マウスの低レベルフックを使うため、**管理者権限なしでも動作**しますが、UAC が有効な環境で他の管理者権限プロセスのウィンドウをリサイズしたい場合は管理者として実行してください。

> [!NOTE]
> バックグラウンドで常駐するツールなので、タスクトレイには表示されません。停止するには **タスクマネージャー** から `cs-mouse-ctrl.exe` を終了するか、新しいインスタンスを起動すると既存インスタンスが自動終了します（後優先の仕様）。
