# Accessibility

Last updated: 2026-11-16

## Priorities and Goals

Saving an image from the clipboard usually takes several steps: open an image editor, paste, choose a folder in a save dialog, and type a file name. For people who use a keyboard alone, a screen reader, a screen magnifier, or who find repeated mouse operations tiring, each of those steps adds effort. Clipotto aims to reduce this to a single copy. Because it runs in the background with very little UI, the few places where people do interact with it — the tray menu, notifications, and the About dialog — need to work reliably with assistive technology.

This statement covers:

- The system tray icon and its right-click menu
- The notifications shown when an image is saved or saving fails
- The "About Clipotto" dialog

The image files Clipotto saves are copies of what was on the clipboard, and are not covered by this statement.

For the UI, we target WCAG 2.2 Level AA, and we treat it as a minimum, not a ceiling. Some of our own rules are stricter than AA (see [Contributor Expectations](#contributor-expectations)). We have not had a third-party evaluation, so this is a target, not a claim of conformance.

## Supported Environments

- Operating systems: Windows 10 / 11
- Screen readers: Not yet tested (Narrator, NVDA, and PC-Talker are not yet tested)
- Keyboard: Not yet tested. The tray icon is expected to be reachable with the standard Windows keys (Win + B to move to the notification area, arrow keys to select the icon, and the Application key or Shift + F10 to open the menu). The About dialog is expected to be closable with Enter or Esc.
- Windows contrast themes and text size settings: Not yet tested

## Known Limitations

- The UI is in Japanese only.
- Notifications are dismissed automatically by Windows after a few seconds, and are not shown on screen while "Do not disturb" (Focus) is on. You can review them in the notification center (Win + N), and check saved images with "Open save folder" (保存フォルダを開く) in the tray menu.
- Clipotto is not DPI-aware, so on displays with scaling above 100%, Windows enlarges it as a bitmap and the text may look blurry.
- The "Pause" (一時停止) and "Notify on save" (保存時に通知する) settings are reset when you exit the app. If you turn notifications off, there is no other feedback when an image is saved.

## Reporting Accessibility Barriers

Please report barriers through [GitHub Issues](https://github.com/securecat/clipotto/issues). You can write in English or Japanese.

The following details help us, but all of them are optional:

- What you were trying to do
- The menu, dialog, or feature you were using
- What happened, and what you expected
- Your Windows version and Clipotto version
- Any assistive technology you use, such as a screen reader

You don't need to tell us about any disability. Screenshots and recordings are not required.

## Response Process

Clipotto is developed by an individual, so we can't promise a deadline for replies or fixes. We will respond as quickly as we can.

- We will reply on the issue once we've seen your report
- If there is a workaround, we will share it
- Before releasing a fix, we may ask you to confirm that it works for you
- Fixes are recorded in [CHANGELOG.md](https://github.com/securecat/clipotto/blob/main/CHANGELOG.md)

## Contributor Expectations

These expectations apply equally to human contributors and coding agents.

When you change the UI:

- Use standard Windows Forms controls (`Button`, `CheckBox`, `LinkLabel`, `ToolStripMenuItem`, and so on). Don't make a `Label` or `PictureBox` clickable in place of a button, and don't draw controls yourself (owner draw)
- Give every control that has no visible text an `AccessibleName`
- Make every feature reachable with a keyboard alone, in a logical Tab order. Set `AcceptButton` and `CancelButton` on dialogs
- Don't hide an option by setting `Enabled = false`. Keep it available and explain why it can't be used right now
- Don't hide focus indicators
- Use system colors (`SystemColors`) so that Windows contrast themes apply. If you must use your own colors, keep text contrast at 7:1 or higher for body text and 4.5:1 or higher for secondary text, and keep non-text elements, such as borders and icons, at 3:1 or higher against adjacent colors
- Use the system font (`SystemFonts.MessageBoxFont`) and don't make text smaller than it. Don't fix font sizes in points or pixels
- Use automatic layout (`AutoSize`, `TableLayoutPanel`, `FlowLayoutPanel`) instead of fixed positions and sizes, so that longer text and larger fonts don't get cut off
- Don't rely only on notifications that disappear on their own. Make the same information available somewhere that stays, such as the save folder or the tray icon's tooltip
- Don't add anything that moves on its own, such as animations or auto-scrolling

After the change, check that everything can be operated with a keyboard alone, and that a screen reader reads the content correctly.

Clipotto is built with the `csc.exe` (C# 5) that comes with .NET Framework 4.x. Don't use language features or libraries that require Visual Studio or a newer .NET.

## Ownership and Maintenance

This statement is maintained by the maintainer, securecat. We update it when we add features or when the tested environments change.

## Feedback on This Statement

Suggestions about this statement or our approach to accessibility are also welcome through [GitHub Issues](https://github.com/securecat/clipotto/issues).

---

# アクセシビリティ

最終更新：2026-11-16

## 優先事項と目標

クリップボードの画像を保存するには、ふつうは何段階もの操作が必要です。画像編集アプリを開き、貼り付け、保存ダイアログでフォルダを選び、ファイル名を入力します。キーボードだけで操作する人、スクリーンリーダーや画面拡大鏡を使う人、マウスの操作を繰り返すのが負担になる人にとっては、その一つひとつが手間になります。Clipotto は、これを「コピーするだけ」に減らすことを目指しています。裏で動き続けるアプリで、UI はごくわずかです。だからこそ、利用者が実際に触れる数少ない場所（トレイメニュー、通知、「Clipotto について」ダイアログ）は、支援技術で確実に使えるようにする必要があります。

この文書が対象とするのは、次のとおりです。

- タスクトレイのアイコンと、その右クリックメニュー
- 画像を保存したとき・保存に失敗したときの通知
- 「Clipotto について」ダイアログ

Clipotto が保存する画像ファイルは、クリップボードに入っていたものの複製なので、この文書の対象外です。

UIは WCAG 2.2 のレベル AA を目標とし、これを上限ではなく下限と考えています。AAより厳しい独自の基準もあります（[貢献者への期待](#貢献者への期待)を参照）。第三者による試験は受けていないため、これは目標であり、適合を表明するものではありません。

## 対応している環境

- OS：Windows 10 / 11
- スクリーンリーダー：まだ試していません（ナレーター、NVDA、PC-Talker はいずれも未確認）
- キーボード：まだ試していません。タスクトレイのアイコンは、Windows の標準のキー操作（Win + B で通知領域に移動し、矢印キーでアイコンを選んで、アプリケーションキーか Shift + F10 でメニューを開く）で操作できる想定です。「Clipotto について」ダイアログは、Enter か Esc で閉じられる想定です。
- Windows のコントラストテーマ、文字サイズの設定：まだ試していません

## 既知の制限

- UI は日本語だけです。
- 通知は、数秒たつと Windows によって自動的に消えます。また、「応答不可」（集中モード）がオンのときは画面に表示されません。通知は通知センター（Win + N）で見返せます。保存された画像は、トレイメニューの「保存フォルダを開く」から確認できます。
- Clipotto は高 DPI に対応していないため、拡大率を 100% より大きくしているディスプレイでは、Windows が画像として引き伸ばして表示し、文字がぼやけることがあります。
- 「一時停止」や「保存時に通知する」の設定は、アプリを終了すると元に戻ります。通知をオフにすると、画像を保存したことをほかの方法では知ることができません。

## アクセシビリティの問題を報告する

問題は [GitHub Issues](https://github.com/securecat/clipotto/issues) で報告してください。日本語でも英語でも構いません。

次のことを書いてもらえると助かりますが、どれも任意です。

- 何をしようとしていたか
- 使っていたメニュー、ダイアログ、機能
- 何が起きたか、どうなると思っていたか
- Windows のバージョン、Clipotto のバージョン
- 使っている支援技術（スクリーンリーダーなど）

障害の有無や内容を書く必要はありません。スクリーンショットや録画も必須ではありません。

## 対応の流れ

Clipotto は個人で開発しているため、返信や修正の期限はお約束できません。それでも、できるかぎり早く対応します。

- 報告を確認したら、Issue で返信します
- 回避策があれば、お知らせします
- 修正をリリースする前に、問題が解消したかどうかの確認をお願いすることがあります
- 修正の内容は [CHANGELOG.md](https://github.com/securecat/clipotto/blob/main/CHANGELOG.md) に記載します

## 貢献者への期待

ここに挙げることは、人間の貢献者にもコーディングエージェントにも、同じように求めます。

UIを変更するときは、次を守ってください。

- Windows フォームの標準のコントロール（`Button`・`CheckBox`・`LinkLabel`・`ToolStripMenuItem` など）を使う。`Label` や `PictureBox` をクリックできるようにしてボタンの代わりにしない。コントロールを自分で描画（オーナードロー）しない
- 見えるテキストを持たないコントロールには、`AccessibleName` を付ける
- すべての機能を、キーボードだけで、自然な Tab 順で操作できるようにする。ダイアログには `AcceptButton` と `CancelButton` を設定する
- `Enabled = false` で選択肢を使えなくしない。選べる状態のまま、いまは使えない理由を伝える
- フォーカスインジケーターを消さない
- Windows のコントラストテーマが効くように、システムの色（`SystemColors`）を使う。どうしても独自の色を使う場合は、テキストのコントラスト比を本文で 7:1 以上、補足的なテキストで 4.5:1 以上にし、境界線やアイコンなどテキスト以外の要素も、隣り合う色に対して 3:1 以上にする
- システムのフォント（`SystemFonts.MessageBoxFont`）を使い、それより小さくしない。文字サイズをポイントやピクセルで固定しない
- 長い文字列や大きなフォントでも切れないように、位置や大きさを固定せず、自動レイアウト（`AutoSize`・`TableLayoutPanel`・`FlowLayoutPanel`）を使う
- ひとりでに消える通知だけに頼らない。同じ情報を、保存フォルダやトレイアイコンのツールチップなど、消えない場所でも確かめられるようにする
- アニメーションや自動スクロールなど、ひとりでに動くものを作らない

変更したあとは、キーボードだけですべて操作できること、スクリーンリーダーで内容が正しく読み上げられることを確認してください。

Clipotto は、.NET Framework 4.x 付属の `csc.exe`（C# 5）でビルドしています。Visual Studio や新しい .NET が必要になる言語機能・ライブラリは使わないでください。

## 責任者と保守

この文書は、メンテナーの securecat が管理しています。機能を追加したときや、動作を確認した環境が変わったときに更新します。

## この文書へのフィードバック

この文書やアクセシビリティへの取り組み方についての提案も、[GitHub Issues](https://github.com/securecat/clipotto/issues) で受け付けています。
