# Mouse Utility

## Feature

| Key | Description |
| --- | ----------- |
| Pause/Break -> Numpad1..9 | move mouse pointer (window 9-positions, Pause/Break again to cancel) |
| Pause/Break -> M | move active window |
| Pause/Break -> F11 | resize active window |
| Pause/Break -> F | fit active window to the largest free area |
| Pause/Break -> T | pin / unpin active window as topmost |
| Pause/Break -> B | send active window to the bottom of the Z-order |
| Pause/Break -> I | minimize active window |
| Pause/Break -> A | maximize / restore active window |
| Pause/Break -> X | close active window |
| Pause/Break -> S | snap active window to grid position |
| Alt + Mouse Move | move mouse pointer at 2× speed |
| Ctrl + Mouse Move | move mouse pointer at 5× speed |

### システムトレイ

アプリ起動中は、タスクトレイに常駐アイコン（マウス形状）が表示されます。アイコンを右クリックすると以下のメニューが利用できます。

| メニュー | 説明 |
| :--- | :--- |
| **設定** | `config.toml` を Notepad で開きます。 |
| **再起動** | アプリを終了し、同じ実行ファイルを新しいプロセスとして起動し直します（設定ファイル変更の反映などに使用）。 |
| **停止** | アプリを終了します。 |

> [!TIP]
> トレイアイコンをダブルクリックすると、主なホットキーの使い方をポップアップで確認できます。表示されるキー名・速度倍率は `config.toml` の設定内容が反映されます。ポップアップはクリックするか約 5 秒で自動的に閉じます。

### move-mouse-pointer 

`Pause` キーまたは `Break` キー（`Ctrl+Break`）を押した後、カーソル位置にあるウィンドウに対象が固定（カーソルがウィンドウ内にクリップ）され、その状態でテンキー（NumPad 1〜9）を押すことで、ウィンドウ内の 9 箇所の位置へマウスポインターを瞬時に移動させることができます。

- **Pause/Breakキー方式（モーダル入力）**:
  - `Pause` キーまたは `Break` キー（`Ctrl+Break`）を押すと、**デスクトップ全体が黒色半透明の矩形で覆われ**、対象ウィンドウがその手前（最前面）に表示され、**カーソル周辺にポップアップヘルプ**が表示されます。
  - そのままテンキー待ち状態に入り、カーソルが対象ウィンドウ内に固定されます。
  - 続けて `NumPad 1〜9` を押すと該当位置へカーソルが移動し、待ち状態が終了します（ポップアップヘルプも閉じます）。
  - 待ち状態中にもう一度 `Pause` キーまたは `Break` キー（あるいは `Esc` キー、マウスクリック）を押すと、移動を行わずにテンキー待ちをキャンセルします（ポップアップヘルプも閉じます）。
  - キー入力の処理が終わる（またはキャンセルする）と、黒色半透明の矩形およびポップアップヘルプは自動的に削除され、対象ウィンドウの最前面状態も元に戻ります。その後は次の `Pause` / `Break` キー入力を待ちます。

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
| **Pause -> NumPad 7** | 左上 (Top-Left) | `(Left + 10, Top + 10)` | ウィンドウの左上隅（タイトルバー左側付近）へ移動 |
| **Pause -> NumPad 8** | 上中央 (Top-Center) | `(CenterX, Top + 10)` | ウィンドウの上辺中央（タイトルバー中央付近）へ移動 |
| **Pause -> NumPad 9** | 右上 (Top-Right) | `(Right - 10, Top + 10)` | ウィンドウの右上隅（閉じるボタン付近）へ移動 |
| **Pause -> NumPad 4** | 左中央 (Middle-Left) | `(Left + 10, CenterY)` | ウィンドウの左辺中央へ移動 |
| **Pause -> NumPad 5** | 中央 (Center) | `(CenterX, CenterY)` | ウィンドウの真ん中へ移動 |
| **Pause -> NumPad 6** | 右中央 (Middle-Right) | `(Right - 10, CenterY)` | ウィンドウの右辺中央へ移動 |
| **Pause -> NumPad 1** | 左下 (Bottom-Left) | `(Left + 10, Bottom - 10)` | ウィンドウの左下隅へ移動 |
| **Pause -> NumPad 2** | 下中央 (Bottom-Center) | `(CenterX, Bottom - 10)` | ウィンドウの下辺中央へ移動 |
| **Pause -> NumPad 3** | 右下 (Bottom-Right) | `(Right - 10, Bottom - 10)` | ウィンドウの右下隅へ移動 |

> [!NOTE]
> - **黒色半透明オーバーレイ**: `Pause` キーを押すと、全モニタ（仮想スクリーン）を覆う黒色半透明ウィンドウが表示され、対象ウィンドウだけがその手前に浮き上がって見えます。オーバーレイはクリックを透過するため、対象ウィンドウの操作を妨げません。
> - **カーソルクリップ機能**: `Pause` キーを押してテンキー待ち状態に入っている間はマウスカーソルの移動可能範囲が対象ウィンドウ内に制限（`ClipCursor`）されます。移動完了時、またはもう一度 `Pause` キー（もしくは `Esc` キー）を押すと自動的に制限が解除されます。
> - **マージン**: 各隅の座標はウィンドウ境界の外に出てしまわないよう、境界から内側へ 10px のオフセットが自動適用されます。


### 高速・低速マウス移動（速度変更）

`Alt` または `Ctrl` キーを押しながらマウスを動かすと、設定された倍率に応じてマウスポインターが高速化または低速化（減速）します。

| キー | 倍率 | 説明 |
| :--- | :---: | :--- |
| **Alt** + マウス移動 | **2倍**（既定） | 通常の移動量を 2 倍に増幅（`0.5` を指定すると移動速度を半分に低下） |
| **Ctrl** + マウス移動 | **5倍**（既定） | 通常の移動量を 5 倍に増幅 |
| **Ctrl + Alt** + マウス移動 | **5倍** | Ctrl が優先される |

> [!NOTE]
> - 移動速度の倍率は小数を指定可能（例: `0.5` で半減、`2` で 2 倍）です。
> - 高速移動中に画面端を超えようとした場合は、仮想スクリーンの端にクランプされます。
> - キーを離すと即座に通常速度に戻ります。


### Window Move 動作フロー

```
Pause -> M 押下
  ↓ 対象ウィンドウ取得・デスクトップ/タスクバー除外チェック
  ↓ ウィンドウ移動モード開始（マウス移動に伴い対象ウィンドウがリアルタイム追従）
[移動決定]
  マウスクリック（左クリック）、または Enter キー押下 → 現在位置で移動を決定・モード終了
[移動キャンセル]
  他の入力（Esc、その他のキー、右クリック等） → 移動前の位置に戻してモード終了
```

- `Pause` キーを押して対象ウィンドウを選択後、`M` キーを押すことでウィンドウ移動モードに入ります。
- 移動モード中は、マウスの移動に伴い対象ウィンドウもリアルタイムに移動します。
- **カーソル形状**: 移動モード中はマウスカーソルが**手のひら（開いた手）形状**に変化し、移動操作中であることが視覚的に分かります。モード終了時（決定・キャンセル）には標準の矢印カーソルへ戻ります。
- **移動の決定**: マウスクリック（左クリック）または `Enter` キーを押すと、現在の位置で移動を確定して移動モードを終了します。
- **移動のキャンセル**: それ以外の入力（`Esc` やその他のキー、右クリックなど）があった場合は、ウィンドウを移動前の位置に戻して移動モードを終了します。


### Resize-Active-Window 動作フロー

```
Pause -> F11 押下
  ↓ 対象ウィンドウ取得・デスクトップ/タスクバー除外チェック
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


### Window Fit 動作フロー

```
Pause -> F 押下
  ↓ 対象ウィンドウ取得（カーソル直下のウィンドウ）
  ↓ 対象ウィンドウが存在するモニタの作業領域を取得
  ↓ outer_spacing を適用した利用可能領域を算出
  ↓ 他の可視ウィンドウを inner_spacing 分拡張して障害物として収集
  ↓ 座標圧縮により、面積が最大になる空き矩形を探索
  ↓ MoveWindow() で対象ウィンドウを移動・リサイズ
```

- 対象ウィンドウは、現在のモニタの作業領域（タスクバーを除く）内で**面積が最大になる空き矩形**へ移動します。
- `outer_spacing` によりデスクトップ端との余白を、`inner_spacing` により他のウィンドウとの余白を確保します。
- 他のウィンドウは `inner_spacing` 分だけ外側に拡張した領域として扱われるため、移動後のウィンドウとの間に必ず `inner_spacing` 以上の間隔が空きます。


### Window Topmost 固定 / 解除 動作フロー

```
Pause -> T 押下
  ↓ カーソル直下の対象ウィンドウ取得
  ↓ 現在の最前面（TopMost）状態を判定
  ↓ 状態を反転（HWND_TOPMOST ⇔ HWND_NOTOPMOST）
  ↓ カーソル周辺にトースト通知を表示
```

- `Pause` キーを押して対象ウィンドウを選択後、`T` キーを押すことで、そのウィンドウの「最前面表示（TopMost）」の固定と解除をトグル切り替えできます。
- 固定されたウィンドウは、他のウィンドウを操作しても背面に隠れず最前面に表示され続けます。
- 再度 `Pause -> T` を押すと固定が解除され、通常のウィンドウ表示に戻ります。
- 切り替え時には、カーソル周辺に「最前面に固定しました (TopMost: ON)」または「最前面固定を解除しました (TopMost: OFF)」のポップアップ通知が約 1.5 秒間表示されます。


### Window 最背面（Z-order 最下位）移動 動作フロー

```
Pause -> B 押下
  ↓ カーソル直下の対象ウィンドウ取得
  ↓ 最前面（TopMost）属性があれば解除 (HWND_NOTOPMOST)
  ↓ SetWindowPos(HWND_BOTTOM) で Z-order の最背面へ移動
  ↓ カーソル周辺にトースト通知を表示
```

- `Pause` キーを押して対象ウィンドウを選択後、`B` キーを押すことで、そのウィンドウの Z-order を最下位（最背面）へ移動させます。
- 作業中のウィンドウを最小化することなく、他の重なっているウィンドウの背後へ素早く退避させたい場合に便利です。
- 対象ウィンドウが最前面（TopMost）に固定されていた場合は、最前面固定が解除された上で最背面に移動します。
- 実行時には、カーソル周辺に「最背面に移動しました (Send to Bottom)」のポップアップ通知が約 1.5 秒間表示されます。


### Window 最小化 / 最大化 / 閉じる

`Pause` キーを押して対象ウィンドウを選択後、以下のキーでウィンドウ操作を行えます。

| キー | 動作 | 説明 |
| :--- | :--- | :--- |
| **Pause -> I** | 最小化 | 対象ウィンドウを最小化します（タスクバーへ格納）。 |
| **Pause -> A** | 最大化 / 元に戻す | 対象ウィンドウを最大化します（すでに最大化されている場合は元の通常サイズに戻します）。 |
| **Pause -> X** | ウィンドウを閉じる | 対象ウィンドウへ `WM_CLOSE` を送信して閉じます（未保存ファイルがある場合は通常の保存確認が出ます）。 |

各操作の実行時には、カーソル周辺にポップアップ通知が表示されます。


### Window Snap 動作フロー

```
Pause -> S 押下
  ↓ 対象ウィンドウ取得・デスクトップ/タスクバー除外チェック
  ↓ スナップ選択 UI（1x1〜4x4 グリッド一覧）を表示
[選択]
  グリッドにマウスをホバー → アクセントカラーでハイライト
  左クリック → 選択したグリッドサイズに合わせてウィンドウを移動・リサイズ
[キャンセル]
  Rect 外クリック、または任意のキー入力 → キャンセルして UI を閉じる
```

- `outer_spacing` と `inner_spacing` を考慮した配置位置で `MoveWindow()` を実行します。
- 最大化されていた場合は復元してからスナップします。


## 設定ファイル

`%LOCALAPPDATA%\cs-mouse-ctrl\config.toml` で動作をカスタマイズできます。
ファイルが存在しない場合、アプリ起動時にデフォルト値で**自動生成**されます。

```toml
# cs-mouse-ctrl 設定ファイル
# 変更後はアプリを再起動してください。

[speed_multiplier]
# Alt キーを押しながらマウス移動したときの速度倍率（数値。0.5で半分に低下、2で2倍など）
alt = 2
# Ctrl キーを押しながらマウス移動したときの速度倍率（数値。0.5で半分に低下、5で5倍など）
ctrl = 5

[hotkey]
# NumPad 待ち受けを起動するキー名の配列（Windows Forms の Keys 列挙体の名前）
# 例: ["Pause", "Break"], ["F12"], ["ScrollLock"]
# "Break" は Ctrl+Break に対応します。
hotkey = ["Pause", "Break"]

# ウィンドウ移動モードを起動するトリガーキー名（Windows Forms の Keys 列挙体の名前）
# 例: "M"
move_key = "M"

# ウィンドウリサイズモードを起動するトリガーキー名（Windows Forms の Keys 列挙体の名前）
# 例: "F11", "F10", "F9"
resize_key = "F11"

# ウィンドウを空きエリアへフィットさせるキー名（Windows Forms の Keys 列挙体の名前）
# 例: "F", "F8"
fit_key = "F"

# ウィンドウの最前面（Topmost）固定/解除をトグルするキー名（Windows Forms の Keys 列挙体の名前）
# 例: "T"
topmost_key = "T"

# ウィンドウの Z-order を最背面（最下位）へ移動するキー名（Windows Forms の Keys 列挙体の名前）
# 例: "B"
bottom_key = "B"

# 対象ウィンドウを最小化するキー名（Windows Forms の Keys 列挙体の名前）
# 例: "I"
minimize_key = "I"

# 対象ウィンドウを最大化するキー名（Windows Forms の Keys 列挙体の名前）
# 例: "A"
maximize_key = "A"

# 対象ウィンドウを閉じるキー名（Windows Forms の Keys 列挙体の名前）
# 例: "X"
close_key = "X"

[window_fit]
# デスクトップ端との余白（上, 右, 下, 左 の順、px）
outer_spacing = [10, 10, 10, 10]
# ウィンドウ間の余白（px）
inner_spacing = 10
```

### 設定項目一覧

| セクション | キー | 型 | 既定値 | 説明 |
| :--- | :--- | :--- | :--- | :--- |
| `[speed_multiplier]` | `alt` | 数値 | `2` | Alt + マウス移動の速度倍率（0.5 等の小数指定で減速可能） |
| `[speed_multiplier]` | `ctrl` | 数値 | `5` | Ctrl + マウス移動の速度倍率（0.5 等の小数指定で減速可能） |
| `[hotkey]` | `hotkey` | 文字列配列 | `["Pause", "Break"]` | NumPad 待ち受けを起動するキー名の配列 |
| `[hotkey]` | `move_key` | 文字列 | `"M"` | ウィンドウ移動モードのトリガーキー |
| `[hotkey]` | `resize_key` | 文字列 | `"F11"` | ウィンドウリサイズモードのトリガーキー |
| `[hotkey]` | `fit_key` | 文字列 | `"F"` | ウィンドウを空きエリアへフィットさせるキー |
| `[hotkey]` | `topmost_key` | 文字列 | `"T"` | ウィンドウの最前面（Topmost）固定/解除をトグルするキー |
| `[hotkey]` | `bottom_key` | 文字列 | `"B"` | ウィンドウの Z-order を最背面（最下位）へ移動するキー |
| `[hotkey]` | `minimize_key` | 文字列 | `"I"` | 対象ウィンドウを最小化するキー |
| `[hotkey]` | `maximize_key` | 文字列 | `"A"` | 対象ウィンドウを最大化するキー |
| `[hotkey]` | `close_key` | 文字列 | `"X"` | 対象ウィンドウを閉じるキー |
| `[hotkey]` | `snap_key` | 文字列 | `"S"` | ウィンドウスナップ選択 UI を起動するキー |
| `[window_fit]` | `outer_spacing` | 整数配列 | `[10, 10, 10, 10]` | デスクトップ端との余白（上, 右, 下, 左） |
| `[window_fit]` | `inner_spacing` | 整数 | `10` | ウィンドウ間の余白 |

> [!NOTE]
> 各ホットキー設定には [Windows Forms の `Keys` 列挙体](https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.forms.keys)の名前を指定します（例: `"Pause"`, `"F11"`, `"F12"`, `"ScrollLock"`, `"F"`, `"T"`, `"B"`, `"I"`, `"A"`, `"X"`）。
>
> `hotkey` は配列で指定し、複数のキーを登録できます。`"Break"` は `Ctrl+Break` に対応します（内部的には `Keys.Cancel`）。旧形式の `pause_key = "Pause"` も後方互換のため引き続き読み込まれます。
>
> `outer_spacing` は `[上, 右, 下, 左]` の順に 4 つの整数を指定します（例: `outer_spacing = [40, 10, 10, 10]`）。


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
> バックグラウンドで常駐するツールです。タスクトレイのアイコンを右クリックして **停止** を選ぶと終了できます。また、新しいインスタンスを起動すると既存インスタンスが自動終了します（後優先の仕様）。

## TODO

### Pause -> ? to display the window process information

### Pause -> Tab to switch the target window
