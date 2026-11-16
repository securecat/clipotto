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
            // アイコンなしでビルドした場合。呼び出し側で Dispose できるように複製を返す
            return new Icon(SystemIcons.Application, size);
        }
    }

    // 高 DPI 対応のためのヘルパー
    //
    // .NET Framework の WinForms は、モニターごとの拡大率（Per-Monitor V2）に自動では追従しない。
    // そのため、フォントや余白の大きさは、表示するモニターの DPI から自前で計算する。
    static class Dpi
    {
        public const int Standard = 96; // 拡大率 100%

        private static readonly IntPtr PerMonitorAwareV2 = new IntPtr(-4);
        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const int MDT_EFFECTIVE_DPI = 0;
        private const int SM_CXSMICON = 49;
        private const int SM_CYSMICON = 50;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetricsForDpi(int index, uint dpi);

        // ウィンドウを作る前に呼ぶこと。呼ばないと、拡大率が 100% より大きいディスプレイで
        // Windows がウィンドウを画像として引き伸ばすため、文字がぼやける
        public static void EnableAwareness()
        {
            // Windows 10 1703 以降：モニターごとの拡大率に、サインインし直さなくても追従できる
            try
            {
                if (SetProcessDpiAwarenessContext(PerMonitorAwareV2)) return;
            }
            catch (EntryPointNotFoundException) { }

            // それより古い Windows：サインインした時点の拡大率に合わせる
            try { SetProcessDPIAware(); }
            catch { }
        }

        // 画面上の位置（ピクセル）にあるモニターの DPI
        public static int ForPoint(Point p)
        {
            try
            {
                var pt = new POINT();
                pt.X = p.X;
                pt.Y = p.Y;
                uint x, y;
                IntPtr monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
                if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out x, out y) == 0) return (int)x;
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }

            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return (int)g.DpiX;
            }
        }

        // 拡大率 100% のときのピクセル値を、指定した DPI に合わせて拡大する
        public static int Scale(float pixels, int dpi)
        {
            return (int)Math.Round(pixels * dpi / Standard);
        }

        // ポイントで指定されたフォント（Windows の文字サイズの設定が反映されている）を、
        // 指定した DPI でのピクセル指定のフォントにする。factor で、さらに拡大できる
        public static Font CreateFont(Font basis, float factor, FontStyle style, int dpi)
        {
            float pixels = basis.SizeInPoints * factor * dpi / 72f;
            return new Font(basis.FontFamily.Name, pixels, style, GraphicsUnit.Pixel);
        }

        // 指定した DPI での、小さいアイコン（タスクトレイなど）の大きさ
        public static Size SmallIconSize(int dpi)
        {
            try
            {
                return new Size(
                    GetSystemMetricsForDpi(SM_CXSMICON, (uint)dpi),
                    GetSystemMetricsForDpi(SM_CYSMICON, (uint)dpi));
            }
            catch (EntryPointNotFoundException)
            {
                return SystemInformation.SmallIconSize;
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
        private Font menuFont = null;
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
            // 開くたびに、表示するモニターの拡大率と、Windows の文字サイズの設定に合わせる
            menu.Opening += (s, e) => UpdateMenuFont();
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
            UpdateTrayIcon();
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += (s, e) => OpenFolder();
            UpdateTrayText();
            tray.Visible = true;

            watcher = new ClipboardWatcher();
            watcher.ClipboardChanged += OnClipboardChanged;

            // 拡大率などの設定が変わったら、トレイのアイコンを作り直す
            SystemEvents.DisplaySettingsChanged += OnSystemSettingsChanged;
            SystemEvents.UserPreferenceChanged += OnSystemSettingsChanged;
        }

        private void OnSystemSettingsChanged(object sender, EventArgs e)
        {
            UpdateTrayIcon();
        }

        private void UpdateTrayIcon()
        {
            // タスクトレイは、通常メインのディスプレイにある
            int dpi = Dpi.ForPoint(Screen.PrimaryScreen.Bounds.Location);
            Icon old = tray.Icon;
            tray.Icon = AppInfo.LoadIcon(Dpi.SmallIconSize(dpi));
            if (old != null) old.Dispose();
        }

        private void UpdateMenuFont()
        {
            int dpi = Dpi.ForPoint(Cursor.Position);
            Font old = menuFont;
            using (var basis = SystemFonts.MenuFont)
            {
                menuFont = Dpi.CreateFont(basis, 1f, FontStyle.Regular, dpi);
            }
            menu.Font = menuFont;
            if (old != null) old.Dispose();
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
            SystemEvents.DisplaySettingsChanged -= OnSystemSettingsChanged;
            SystemEvents.UserPreferenceChanged -= OnSystemSettingsChanged;
            watcher.Dispose();
            tray.Visible = false;
            tray.Dispose();
            menu.Dispose();
            if (menuFont != null) menuFont.Dispose();
            base.ExitThreadCore();
        }
    }

    // 「Clipotto について」ダイアログ
    class AboutForm : Form
    {
        private const int WM_DPICHANGED = 0x02E0;

        // 中身の文字は、Windows 標準の文字サイズの 4/3 倍にする（拡大率・文字サイズが 100% のとき 16px）。
        // 余白やアイコンの大きさも、同じ割合で大きくする
        private const float ContentScale = 4f / 3f;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private int dpi;
        private Font contentFont = null;
        private Font titleFont = null;
        private Image iconImage = null;
        private readonly Icon windowIcon;
        private Button okButton = null;
        private LinkLabel repoLink = null;

        public AboutForm()
        {
            Text = AppInfo.Name + " について";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None; // 拡大は BuildContent で自前で行う
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            windowIcon = AppInfo.LoadIcon(SystemInformation.SmallIconSize);
            Icon = windowIcon;

            // CenterScreen では、マウスポインターのあるモニターに表示される
            dpi = Dpi.ForPoint(Cursor.Position);
            BuildContent();

            // 開いている間に Windows の文字サイズの設定が変わったら、作り直す
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        // 拡大率 100%・標準の文字サイズのときのピクセル値を、いまの表示に合わせて拡大する
        private int S(int pixels)
        {
            return Dpi.Scale(pixels * ContentScale, dpi);
        }

        // ダイアログの中身を作る。拡大率や文字サイズが変わったときは、作り直す
        private void BuildContent()
        {
            bool rebuilding = okButton != null;
            bool focusOnLink = repoLink != null && repoLink.Focused;

            SuspendLayout();
            while (Controls.Count > 0) Controls[0].Dispose();

            Font oldContent = contentFont;
            Font oldTitle = titleFont;
            Image oldImage = iconImage;
            using (var basis = SystemFonts.MessageBoxFont)
            {
                contentFont = Dpi.CreateFont(basis, ContentScale, FontStyle.Regular, dpi);
                titleFont = Dpi.CreateFont(basis, ContentScale * 1.5f, FontStyle.Bold, dpi);
            }
            Font = contentFont;
            if (oldContent != null) oldContent.Dispose();
            if (oldTitle != null) oldTitle.Dispose();
            if (oldImage != null) oldImage.Dispose();

            Padding = new Padding(S(16));
            int gap = S(3);

            var layout = new TableLayoutPanel();
            layout.AutoSize = true;
            layout.ColumnCount = 2;
            layout.RowCount = 2;
            layout.Dock = DockStyle.Fill;

            // 左: アイコン
            var pic = new PictureBox();
            int iconSize = S(64);
            pic.Size = new Size(iconSize, iconSize);
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            using (var icon = AppInfo.LoadIcon(new Size(iconSize, iconSize)))
            {
                iconImage = icon.ToBitmap();
            }
            pic.Image = iconImage;
            pic.Margin = new Padding(0, 0, S(16), 0);
            layout.Controls.Add(pic, 0, 0);

            // 右: テキスト
            var info = new FlowLayoutPanel();
            info.FlowDirection = FlowDirection.TopDown;
            info.AutoSize = true;
            info.WrapContents = false;

            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            var title = MakeLabel(AppInfo.Name + "  v" + v.Major + "." + v.Minor + "." + v.Build, gap);
            title.Font = titleFont;
            info.Controls.Add(title);
            info.Controls.Add(MakeLabel(AppInfo.Description, gap));

            var copyright = MakeLabel(AppInfo.Copyright, gap);
            copyright.Margin = new Padding(gap, S(14), gap, 0);
            info.Controls.Add(copyright);
            info.Controls.Add(MakeLabel(AppInfo.License, gap));

            repoLink = null;
            if (AppInfo.RepositoryUrl.Length > 0)
            {
                repoLink = new LinkLabel();
                repoLink.Text = AppInfo.RepositoryUrl;
                repoLink.AutoSize = true;
                repoLink.Margin = new Padding(gap, S(14), gap, 0);
                repoLink.LinkClicked += (s, e) =>
                {
                    try { Process.Start(AppInfo.RepositoryUrl); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, AppInfo.Name); }
                };
                info.Controls.Add(repoLink);
            }
            layout.Controls.Add(info, 1, 0);

            // 下: OK ボタン
            okButton = new Button();
            okButton.Text = "OK";
            okButton.AutoSize = true;
            okButton.MinimumSize = new Size(S(88), 0);
            okButton.Anchor = AnchorStyles.Right;
            okButton.Margin = new Padding(gap, S(18), 0, 0);
            okButton.Click += (s, e) => Close();
            layout.Controls.Add(okButton, 0, 1);
            layout.SetColumnSpan(okButton, 2);

            Controls.Add(layout);
            AcceptButton = okButton;
            CancelButton = okButton;

            // 作り直した場合は、フォーカスがあったコントロールにフォーカスを戻す
            if (rebuilding) ActiveControl = focusOnLink ? (Control)repoLink : okButton;
            ResumeLayout(true);
        }

        private static Label MakeLabel(string text, int gap)
        {
            var label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Margin = new Padding(gap);
            return label;
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (!IsDisposed) BuildContent();
        }

        protected override void WndProc(ref Message m)
        {
            // 別の拡大率のモニターへ移動したときは、その拡大率で作り直す
            if (m.Msg == WM_DPICHANGED)
            {
                dpi = (int)((long)m.WParam & 0xFFFF);
                var suggested = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));
                Location = new Point(suggested.Left, suggested.Top);
                BuildContent();
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            }
            base.Dispose(disposing);
            if (disposing)
            {
                if (contentFont != null) contentFont.Dispose();
                if (titleFont != null) titleFont.Dispose();
                if (iconImage != null) iconImage.Dispose();
                windowIcon.Dispose();
            }
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
