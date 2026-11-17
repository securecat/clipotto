<p align="center">
  <img src="docs/icon.png" width="128" alt="Clipotto icon">
</p>

<h1 align="center">Clipotto</h1>

<p align="center">
  A small Windows tray app that automatically saves images copied to the clipboard as files.
</p>

> **Note**
> The app's user interface is in Japanese only. Menu names in this section are shown with their Japanese labels.

## Features

- **Just copy, and it's saved** — Every time an image lands on the clipboard, such as a screenshot (Win + Shift + S) or a browser's "Copy image", it is saved as a PNG.
- **Lightweight** — It uses Windows' clipboard change notifications instead of polling, so it uses almost no CPU.
- **No duplicates** — If the same image is put on the clipboard several times in a row, it is saved only once.
- **Lives in the system tray** — It stays out of your way and is operated from the right-click menu.
- **Easy to build** — No Visual Studio required. It builds with the C# compiler that comes with Windows.

## Screenshot

![Tray menu](docs/screenshot-menu.png)

## Requirements

- Windows 10 / 11
- .NET Framework 4.x (included with Windows 10 / 11)

## Installation

### Use the prebuilt exe

Download `Clipotto.exe` from [Releases](../../releases), put it anywhere you like, and run it. No installation is needed.

> **Note**
> Because the exe is not signed, Windows SmartScreen may show a warning the first time you run it. In that case, click "More info" and then "Run anyway".

### Build from source

1. Clone this repository, or download it as a ZIP.
2. Double-click `build.bat` in the `src` folder.
3. `Clipotto.exe` is created in the `src` folder.

If `Clipotto.ico` is in the same folder, the icon is embedded in the exe. After building, the icon is shown even without the ico file.

<details>
<summary>Building with a command directly</summary>

Run the following command in the `src` folder.

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /nologo /codepage:65001 /target:winexe /out:Clipotto.exe ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  /win32icon:Clipotto.ico /resource:Clipotto.ico,Clipotto.ico ^
  Clipotto.cs
```

</details>

## Usage

When you start `Clipotto.exe`, an icon appears in the system tray. From then on, just copy an image and it is saved automatically.

### Save location

```
%USERPROFILE%\Pictures\Clipotto\
```

Files are named after the date and time they were saved (e.g. `20261116_153012_123.png`).

### Tray menu

Right-click the icon to use the following menu items.

| Menu item | Description |
| --- | --- |
| Open save folder (保存フォルダを開く) | Opens the save folder in File Explorer (you can also double-click the icon) |
| Pause (一時停止) | Temporarily stops saving. Select it again to resume |
| Notify on save (保存時に通知する) | Toggles whether a notification is shown when an image is saved |
| About Clipotto (Clipotto について) | Shows the version and license information |
| Exit (終了) | Exits Clipotto |

### Starting automatically when your PC starts

1. Press Win + R, type `shell:startup`, and press Enter.
2. Place a shortcut to `Clipotto.exe` in the folder that opens.

## Notes

- If you copy an image *file* in File Explorer, the clipboard holds the file path rather than image data, so nothing is saved.
- Depending on the source app, transparency may be lost and the background may turn black.
- When Windows "Do not disturb" (Focus) is on, notifications do not pop up on screen and appear only in the notification center.
- The "Pause" and "Notify on save" settings are reset when you exit the app.

## Changelog

### [1.1.0] - 2026-11-16

#### Added

- Support for high DPI displays: text stays sharp at display scaling above 100%, including after changing the scaling without signing out
- Support for the Windows text size setting in the tray menu and the About dialog

#### Changed

- Enlarged the text in the About dialog

See [CHANGELOG.md](CHANGELOG.md) for the full history.

---

<p align="center">
  <img src="docs/icon.png" width="128" alt="Clipotto icon">
</p>

<h1 align="center">クリポット</h1>

<p align="center">
  クリップボードに入った画像を、自動でファイルに保存してくれる Windows 用の小さな常駐ツールです。
</p>

## 特長

- **コピーするだけで自動保存** — スクリーンショット（Win + Shift + S）や、ブラウザの「画像をコピー」など、クリップボードに画像が入るたびに PNG で保存します。
- **軽い** — Windows のクリップボード変更通知を使っているので、定期チェックのような無駄な処理がなく、CPU をほとんど使いません。
- **重複しない** — 同じ画像が続けてクリップボードに入っても、二重には保存しません。
- **タスクトレイに常駐** — 画面の邪魔にならず、右クリックメニューから操作できます。
- **ビルドが簡単** — Visual Studio は不要です。Windows に標準で入っている C# コンパイラでビルドできます。

## スクリーンショット

![トレイメニュー](docs/screenshot-menu.png)

## 動作環境

- Windows 10 / 11
- .NET Framework 4.x（Windows 10 / 11 には標準で入っています）

## インストール

### ビルド済みの exe を使う

[Releases](../../releases) から `Clipotto.exe` をダウンロードして、好きな場所に置いて実行してください。インストール作業は必要ありません。

> **Note**
> 署名のない exe のため、初回起動時に Windows SmartScreen の警告が出ることがあります。その場合は「詳細情報」→「実行」で起動できます。

### ソースからビルドする

1. このリポジトリをクローン、または ZIP でダウンロードします。
2. `src` フォルダの中にある `build.bat` をダブルクリックします。
3. `src` フォルダに `Clipotto.exe` ができあがります。

`Clipotto.ico` が同じフォルダにあれば、アイコンが exe に埋め込まれます。ビルド後は ico ファイルがなくてもアイコンは表示されます。

<details>
<summary>コマンドで直接ビルドする場合</summary>

`src` フォルダで次のコマンドを実行します。

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /nologo /codepage:65001 /target:winexe /out:Clipotto.exe ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  /win32icon:Clipotto.ico /resource:Clipotto.ico,Clipotto.ico ^
  Clipotto.cs
```

</details>

## 使い方

`Clipotto.exe` を起動すると、タスクトレイにアイコンが表示されます。あとは画像をコピーするだけで、自動的に保存されます。

### 保存先

```
%USERPROFILE%\Pictures\Clipotto\
```

ファイル名は保存した日時になります（例: `20261116_153012_123.png`）。

### トレイメニュー

アイコンを右クリックすると、次のメニューが使えます。

| メニュー | 内容 |
| --- | --- |
| 保存フォルダを開く | 保存先フォルダをエクスプローラーで開きます（アイコンのダブルクリックでも開けます） |
| 一時停止 | 保存を一時的に止めます。もう一度選ぶと再開します |
| 保存時に通知する | 保存したときに通知を表示するかどうかを切り替えます |
| Clipotto について | バージョンとライセンス情報を表示します |
| 終了 | Clipotto を終了します |

### PC の起動時に自動で起動したい場合

1. Win + R を押して `shell:startup` と入力し、Enter を押します。
2. 開いたフォルダに `Clipotto.exe` のショートカットを置きます。

## 注意事項

- エクスプローラーで画像「ファイル」をコピーした場合は、クリップボードに入るのが画像データではなくファイルのパスなので、保存されません。
- コピー元のアプリによっては、透過情報が失われて背景が黒くなることがあります。
- Windows の「応答不可」（集中モード）がオンのときは、通知は画面に出ずに通知センターにだけ表示されます。
- 「一時停止」や「保存時に通知する」の設定は、アプリを終了すると元に戻ります。

## 更新履歴

### [1.1.0] - 2026-11-16

#### 追加

- 高 DPI に対応。拡大率が 100% より大きいディスプレイでも、文字がぼやけないようにした。サインインし直さずに拡大率を変えた場合にも対応
- トレイメニューと「Clipotto について」ダイアログで、Windows の文字サイズの設定に対応

#### 変更

- 「Clipotto について」ダイアログの文字を大きくした

全履歴は [CHANGELOG.md](CHANGELOG.md) を参照。
