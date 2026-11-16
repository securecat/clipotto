// Clipotto - クリップボードに新しい画像が入るたびに PNG で保存するトレイ常駐アプリ
// Copyright (c) 2026 securecat
// Released under the MIT License. See LICENSE for details.
//
// .NET Framework 4.x 付属の csc.exe (C# 5) でビルドできるように書いています。

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

// exe のプロパティ（詳細タブ）に表示される情報
[assembly: AssemblyTitle(Clipotto.AppInfo.Name)]
[assembly: AssemblyProduct(Clipotto.AppInfo.Name)]
[assembly: AssemblyDescription(Clipotto.AppInfo.Description)]
[assembly: AssemblyCopyright(Clipotto.AppInfo.Copyright)]
[assembly: AssemblyVersion(Clipotto.AppInfo.Version)]
[assembly: AssemblyFileVersion(Clipotto.AppInfo.Version)]

namespace Clipotto
{
    // アプリ情報はここでまとめて管理
    static class AppInfo
    {
        public const string Name = "Clipotto";
        public const string Version = "1.0.0.0";
        public const string Description = "クリップボードの画像を自動で保存するツール";
        public const string Copyright = "Copyright (c) 2026 securecat";
        public const string License = "Released under the MIT License.";
        public const string RepositoryUrl = "https://github.com/securecat/clipotto";

        // exe に埋め込んだ Clipotto.ico から、指定サイズに近いアイコンを取り出す
        public static Icon LoadIcon(Size size)
        {
            try
            {
                using (var s = typeof(AppInfo).Assembly.GetManifestResourceStream("Clipotto.ico"))
                {
                    if (s != null) return new Icon(s, size);
                }
            }
            catch { }
            return SystemIcons.Application; // アイコンなしでビルドした場合
        }
    }

    // 高 DPI 対応のためのヘルパー
    static class Dpi
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        // ウィンドウを作る前に呼ぶこと。呼ばないと、拡大率が 100% より大きいディスプレイで
        // Windows がウィンドウを画像として引き伸ばすため、文字がぼやける
        public static void EnableAwareness()
        {
            try { SetProcessDPIAware(); }
            catch { }
        }

        // 96 DPI（拡大率 100%）を基準にしたピクセル値を、いまの DPI に合わせて拡大する
        public static int Scale(int pixels)
        {
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return (int)Math.Round(pixels * g.DpiX / 96f);
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Dpi.EnableAwareness();

            bool createdNew;
            using (var mutex = new Mutex(true, "Clipotto_SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(AppInfo.Name + " はすでに起動しています。", AppInfo.Name);
                    return;
                }
                Application.EnableVisualStyles();
                Application.Run(new ClipottoContext());
            }
        }
    }

    class ClipottoContext : ApplicationContext
    {
        private readonly NotifyIcon tray;
        private readonly ContextMenuStrip menu;
        private readonly ClipboardWatcher watcher;
        private readonly string saveDir;
        private readonly ToolStripMenuItem pauseItem;
        private readonly ToolStripMenuItem notifyItem;
        private string lastHash = null;
        private bool paused = false;
        private AboutForm aboutForm = null;

        public ClipottoContext()
        {
            // 保存先: ピクチャ\Clipotto
            saveDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), AppInfo.Name);
            Directory.CreateDirectory(saveDir);

            menu = new ContextMenuStrip();
            // Windows の「文字のサイズ」の設定が反映されるメニュー用のフォント
            menu.Font = SystemFonts.MenuFont;
            menu.Items.Add("保存フォルダを開く", null, (s, e) => OpenFolder());
            pauseItem = new ToolStripMenuItem("一時停止", null, (s, e) => TogglePause());
            menu.Items.Add(pauseItem);
            notifyItem = new ToolStripMenuItem("保存時に通知する", null, (s, e) =>
            {
                notifyItem.Checked = !notifyItem.Checked;
            });
            notifyItem.Checked = true;
            menu.Items.Add(notifyItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(AppInfo.Name + " について", null, (s, e) => ShowAbout());
            menu.Items.Add("終了", null, (s, e) => ExitThread());

            tray = new NotifyIcon();
            tray.Icon = AppInfo.LoadIcon(SystemInformation.SmallIconSize);
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += (s, e) => OpenFolder();
            UpdateTrayText();
            tray.Visible = true;

            watcher = new ClipboardWatcher();
            watcher.ClipboardChanged += OnClipboardChanged;

            // 起動中に文字のサイズなどの設定が変わったら、メニューのフォントを取り直す
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            Font old = menu.Font;
            menu.Font = SystemFonts.MenuFont;
            old.Dispose();
        }

        private void UpdateTrayText()
        {
            tray.Text = AppInfo.Name + (paused ? "（一時停止中）" : "（監視中）");
        }

        private void OpenFolder()
        {
            Process.Start("explorer.exe", "\"" + saveDir + "\"");
        }

        private void TogglePause()
        {
            paused = !paused;
            pauseItem.Checked = paused;
            UpdateTrayText();
        }

        private void ShowAbout()
        {
            // すでに開いていれば前面に出すだけ
            if (aboutForm != null && !aboutForm.IsDisposed)
            {
                aboutForm.Activate();
                return;
            }
            aboutForm = new AboutForm();
            aboutForm.Show();
        }

        private void OnClipboardChanged(object sender, EventArgs e)
        {
            if (paused) return;

            try
            {
                Image img = null;

                // 他のアプリがクリップボードを掴んでいることがあるので数回リトライ
                for (int i = 0; i < 5 && img == null; i++)
                {
                    try
                    {
                        if (!Clipboard.ContainsImage()) return;
                        img = Clipboard.GetImage();
                    }
                    catch (ExternalException)
                    {
                        Thread.Sleep(100);
                    }
                }
                if (img == null) return;

                using (img)
                using (var ms = new MemoryStream())
                {
                    img.Save(ms, ImageFormat.Png);
                    byte[] data = ms.ToArray();

                    // 同じ画像の重複保存を防ぐ（1回のコピーで通知が複数来ることがあるため）
                    string hash = ComputeHash(data);
                    if (hash == lastHash) return;
                    lastHash = hash;

                    string name = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png";
                    File.WriteAllBytes(Path.Combine(saveDir, name), data);

                    if (notifyItem.Checked)
                    {
                        tray.ShowBalloonTip(1500, "画像を保存しました", name, ToolTipIcon.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                tray.ShowBalloonTip(3000, "保存に失敗しました", ex.Message, ToolTipIcon.Error);
            }
        }

        private static string ComputeHash(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(data));
            }
        }

        protected override void ExitThreadCore()
        {
            if (aboutForm != null && !aboutForm.IsDisposed) aboutForm.Close();
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            watcher.Dispose();
            tray.Visible = false;
            tray.Dispose();
            base.ExitThreadCore();
        }
    }

    // 「Clipotto について」ダイアログ
    class AboutForm : Form
    {
        public AboutForm()
        {
            Text = AppInfo.Name + " について";
            Font = SystemFonts.MessageBoxFont;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(Dpi.Scale(16));
            Icon = AppInfo.LoadIcon(SystemInformation.SmallIconSize);

            int gap = Dpi.Scale(3);

            var layout = new TableLayoutPanel();
            layout.AutoSize = true;
            layout.ColumnCount = 2;
            layout.RowCount = 2;
            layout.Dock = DockStyle.Fill;

            // 左: アイコン
            var pic = new PictureBox();
            int iconSize = Dpi.Scale(64);
            pic.Size = new Size(iconSize, iconSize);
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            pic.Image = AppInfo.LoadIcon(new Size(iconSize, iconSize)).ToBitmap();
            pic.Margin = new Padding(0, 0, Dpi.Scale(16), 0);
            layout.Controls.Add(pic, 0, 0);

            // 右: テキスト
            var info = new FlowLayoutPanel();
            info.FlowDirection = FlowDirection.TopDown;
            info.AutoSize = true;
            info.WrapContents = false;

            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            var title = MakeLabel(AppInfo.Name + "  v" + v.Major + "." + v.Minor + "." + v.Build);
            title.Font = new Font(Font.FontFamily, Font.Size * 1.5f, FontStyle.Bold);
            info.Controls.Add(title);
            info.Controls.Add(MakeLabel(AppInfo.Description));

            var copyright = MakeLabel(AppInfo.Copyright);
            copyright.Margin = new Padding(gap, Dpi.Scale(14), gap, 0);
            info.Controls.Add(copyright);
            info.Controls.Add(MakeLabel(AppInfo.License));

            if (AppInfo.RepositoryUrl.Length > 0)
            {
                var link = new LinkLabel();
                link.Text = AppInfo.RepositoryUrl;
                link.AutoSize = true;
                link.Margin = new Padding(gap, Dpi.Scale(14), gap, 0);
                link.LinkClicked += (s, e) =>
                {
                    try { Process.Start(AppInfo.RepositoryUrl); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, AppInfo.Name); }
                };
                info.Controls.Add(link);
            }
            layout.Controls.Add(info, 1, 0);

            // 下: OK ボタン
            var ok = new Button();
            ok.Text = "OK";
            ok.AutoSize = true;
            ok.MinimumSize = new Size(Dpi.Scale(88), 0);
            ok.Anchor = AnchorStyles.Right;
            ok.Margin = new Padding(gap, Dpi.Scale(18), 0, 0);
            ok.Click += (s, e) => Close();
            layout.Controls.Add(ok, 0, 1);
            layout.SetColumnSpan(ok, 2);

            Controls.Add(layout);
            AcceptButton = ok;
            CancelButton = ok;
        }

        private static Label MakeLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            label.AutoSize = true;
            return label;
        }
    }

    // クリップボードの変更通知 (WM_CLIPBOARDUPDATE) を受け取るための不可視ウィンドウ
    class ClipboardWatcher : NativeWindow, IDisposable
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public event EventHandler ClipboardChanged;

        public ClipboardWatcher()
        {
            var cp = new CreateParams();
            cp.Parent = HWND_MESSAGE; // メッセージ専用ウィンドウ
            CreateHandle(cp);
            AddClipboardFormatListener(Handle);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE)
            {
                var handler = ClipboardChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                RemoveClipboardFormatListener(Handle);
                DestroyHandle();
            }
        }
    }
}
