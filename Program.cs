using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ThaiAiCapture
{
    static class Program
    {
        // P/Invoke for DPI Awareness
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private const string AppGuid = "1ThaiAiCapture_SingleInstance_Mutex_8F7A9B";

        [STAThread]
        static void Main()
        {
            // Set Process DPI Aware to ensure crisp pixel-perfect screenshots on scaled displays (125%, 150%, etc.)
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    SetProcessDPIAware();
                }
            }
            catch { }

            // Single Instance Guard
            bool isNewInstance;
            using (Mutex mutex = new Mutex(true, AppGuid, out isNewInstance))
            {
                if (!isNewInstance)
                {
                    MessageBox.Show(
                        "โปรแกรม 1ThaiAi Capture กำลังทำงานอยู่ใน System Tray (มุมขวาล่าง) อยู่แล้วครับ\nกด F1 เพื่อเริ่มจับภาพได้ทันที",
                        "1ThaiAi Capture ทำงานอยู่แล้ว",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Run in background via ApplicationContext
                Application.Run(new TrayAppContext());
            }
        }
    }

    public enum AppLanguage
    {
        Thai,
        English
    }

    public enum ImageSaveFormat
    {
        PNG,
        JPG,
        BMP
    }

    /// <summary>
    /// Background Application Context managing System Tray, Global Low-Level Keyboard Hook, and Multi-language support
    /// </summary>
    public class TrayAppContext : ApplicationContext
    {
        public static AppLanguage CurrentLanguage = AppLanguage.Thai;
        public static ImageSaveFormat DefaultFormat = ImageSaveFormat.PNG;

        private readonly NotifyIcon _notifyIcon;
        private readonly GlobalKeyboardHook _keyboardHook;
        private CaptureOverlayForm _currentOverlay;
        private const string WEBSITE_URL = "https://1thaiai.com";

        // Menu Items
        private ToolStripMenuItem _titleItem;
        private ToolStripMenuItem _captureItem;
        private ToolStripMenuItem _hotkeyMenu;
        private ToolStripMenuItem _formatMenu;
        private ToolStripMenuItem _fmtPngItem;
        private ToolStripMenuItem _fmtJpgItem;
        private ToolStripMenuItem _fmtBmpItem;
        private ToolStripMenuItem _langMenu;
        private ToolStripMenuItem _langThaiItem;
        private ToolStripMenuItem _langEngItem;
        private ToolStripMenuItem _websiteItem;
        private ToolStripMenuItem _exitItem;

        public TrayAppContext()
        {
            // Create Context Menu for Tray Icon
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            contextMenu.RenderMode = ToolStripRenderMode.System;

            // Title Header
            _titleItem = new ToolStripMenuItem("🎯 1ThaiAi Capture")
            {
                Enabled = false,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            contextMenu.Items.Add(_titleItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            // Menu: Capture Now
            _captureItem = new ToolStripMenuItem("📸 จับภาพหน้าจอ (Capture)", null, (s, e) => TriggerCapture());
            contextMenu.Items.Add(_captureItem);

            // Submenu: Choose Hotkey
            _hotkeyMenu = new ToolStripMenuItem("⚙️ ตั้งค่าปุ่มลัด (Hotkey)");
            CreateHotkeySubmenu(_hotkeyMenu);
            contextMenu.Items.Add(_hotkeyMenu);

            // Submenu: Choose Default Image Format
            _formatMenu = new ToolStripMenuItem("📁 ฟอร์แมตไฟล์ (Save Format)");
            CreateFormatSubmenu(_formatMenu);
            contextMenu.Items.Add(_formatMenu);

            // Submenu: Language Switcher with National Flags
            _langMenu = new ToolStripMenuItem("🌐 ภาษา / Language");
            CreateLanguageSubmenu(_langMenu);
            contextMenu.Items.Add(_langMenu);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Menu: 1ThaiAi.com Link
            _websiteItem = new ToolStripMenuItem("🌐 1ThaiAi.com (ไปที่เว็บ)", null, (s, e) => OpenWebsite());
            _websiteItem.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            contextMenu.Items.Add(_websiteItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Menu: Exit
            _exitItem = new ToolStripMenuItem("❌ Exit (ปิดโปรแกรม)", null, (s, e) => ExitApplication());
            contextMenu.Items.Add(_exitItem);

            // Create Tray Icon
            Icon appIcon = CreateAppIcon();
            _notifyIcon = new NotifyIcon
            {
                Icon = appIcon,
                ContextMenuStrip = contextMenu,
                Text = "1ThaiAi Capture (กด F1 เพื่อจับภาพ)",
                Visible = true
            };

            // Open Context Menu on both Left-Click and Right-Click
            _notifyIcon.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    try
                    {
                        MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (mi != null)
                        {
                            mi.Invoke(_notifyIcon, null);
                        }
                        else
                        {
                            contextMenu.Show(Cursor.Position);
                        }
                    }
                    catch
                    {
                        contextMenu.Show(Cursor.Position);
                    }
                }
            };

            _notifyIcon.DoubleClick += (s, e) => TriggerCapture();

            // Initialize Global Keyboard Hook (WH_KEYBOARD_LL)
            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.Triggered += TriggerCapture;

            // Apply initial language labels
            UpdateLanguageUI();

            _notifyIcon.ShowBalloonTip(
                2500,
                "1ThaiAi Capture พร้อมใช้งาน",
                "กดปุ่ม [ F1 ] บนคีย์บอร์ดเมื่อใดก็ได้เพื่อเริ่มจับภาพหน้าจอ",
                ToolTipIcon.Info
            );
        }

        private void CreateFormatSubmenu(ToolStripMenuItem parentMenu)
        {
            _fmtPngItem = new ToolStripMenuItem("PNG (*.png) - คุณภาพสูงสุด", null, (s, e) => SetFormat(ImageSaveFormat.PNG));
            _fmtJpgItem = new ToolStripMenuItem("JPG (*.jpg) - ไฟล์ขนาดเล็ก", null, (s, e) => SetFormat(ImageSaveFormat.JPG));
            _fmtBmpItem = new ToolStripMenuItem("BMP (*.bmp) - บิตแมป", null, (s, e) => SetFormat(ImageSaveFormat.BMP));

            UpdateFormatCheckmarks();

            parentMenu.DropDownItems.Add(_fmtPngItem);
            parentMenu.DropDownItems.Add(_fmtJpgItem);
            parentMenu.DropDownItems.Add(_fmtBmpItem);
        }

        private void SetFormat(ImageSaveFormat format)
        {
            DefaultFormat = format;
            UpdateFormatCheckmarks();
        }

        private void UpdateFormatCheckmarks()
        {
            if (_fmtPngItem != null) _fmtPngItem.Checked = (DefaultFormat == ImageSaveFormat.PNG);
            if (_fmtJpgItem != null) _fmtJpgItem.Checked = (DefaultFormat == ImageSaveFormat.JPG);
            if (_fmtBmpItem != null) _fmtBmpItem.Checked = (DefaultFormat == ImageSaveFormat.BMP);
        }

        private void CreateLanguageSubmenu(ToolStripMenuItem parentMenu)
        {
            _langThaiItem = new ToolStripMenuItem("ไทย (Thai)", CreateThaiFlagBitmap(), (s, e) => SetLanguage(AppLanguage.Thai));
            _langEngItem = new ToolStripMenuItem("English", CreateEnglishFlagBitmap(), (s, e) => SetLanguage(AppLanguage.English));

            _langThaiItem.Checked = (CurrentLanguage == AppLanguage.Thai);
            _langEngItem.Checked = (CurrentLanguage == AppLanguage.English);

            parentMenu.DropDownItems.Add(_langThaiItem);
            parentMenu.DropDownItems.Add(_langEngItem);
        }

        private void SetLanguage(AppLanguage lang)
        {
            CurrentLanguage = lang;
            UpdateLanguageUI();
        }

        private void UpdateLanguageUI()
        {
            if (_langThaiItem != null) _langThaiItem.Checked = (CurrentLanguage == AppLanguage.Thai);
            if (_langEngItem != null) _langEngItem.Checked = (CurrentLanguage == AppLanguage.English);

            string currentKey = (_keyboardHook != null && _keyboardHook.CurrentMode == GlobalKeyboardHook.ShortcutMode.CtrlShiftA) ? "Ctrl+Shift+A" : (_keyboardHook != null ? _keyboardHook.CurrentMode.ToString() : "F1");

            if (CurrentLanguage == AppLanguage.Thai)
            {
                if (_captureItem != null) _captureItem.Text = "📸 จับภาพหน้าจอ (Capture)";
                if (_hotkeyMenu != null) _hotkeyMenu.Text = "⚙️ ตั้งค่าปุ่มลัด (Hotkey)";
                if (_formatMenu != null) _formatMenu.Text = "📁 ฟอร์แมตไฟล์ (Save Format)";
                if (_fmtPngItem != null) _fmtPngItem.Text = "PNG (*.png) - คุณภาพสูงสุด (Lossless)";
                if (_fmtJpgItem != null) _fmtJpgItem.Text = "JPG (*.jpg) - ไฟล์ขนาดเล็ก (Small Size)";
                if (_fmtBmpItem != null) _fmtBmpItem.Text = "BMP (*.bmp) - บิตแมป (Bitmap)";
                if (_langMenu != null) _langMenu.Text = "🌐 ภาษา (Language)";
                if (_websiteItem != null) _websiteItem.Text = "🌐 1ThaiAi.com (ไปที่เว็บ)";
                if (_exitItem != null) _exitItem.Text = "❌ Exit (ปิดโปรแกรม)";
                if (_notifyIcon != null) _notifyIcon.Text = string.Format("1ThaiAi Capture (กด {0} เพื่อจับภาพ)", currentKey);
            }
            else
            {
                if (_captureItem != null) _captureItem.Text = "📸 Capture Screen";
                if (_hotkeyMenu != null) _hotkeyMenu.Text = "⚙️ Hotkey Settings";
                if (_formatMenu != null) _formatMenu.Text = "📁 Save Image Format";
                if (_fmtPngItem != null) _fmtPngItem.Text = "PNG (*.png) - Best Quality (Lossless)";
                if (_fmtJpgItem != null) _fmtJpgItem.Text = "JPG (*.jpg) - Small File Size";
                if (_fmtBmpItem != null) _fmtBmpItem.Text = "BMP (*.bmp) - Bitmap";
                if (_langMenu != null) _langMenu.Text = "🌐 Language";
                if (_websiteItem != null) _websiteItem.Text = "🌐 1ThaiAi.com (Visit Website)";
                if (_exitItem != null) _exitItem.Text = "❌ Exit";
                if (_notifyIcon != null) _notifyIcon.Text = string.Format("1ThaiAi Capture (Press {0} to capture)", currentKey);
            }

            // Update Hotkey submenu labels
            if (_hotkeyMenu != null)
            {
                UpdateHotkeySubmenuLabels();
            }
        }

        private void UpdateHotkeySubmenuLabels()
        {
            if (_hotkeyMenu == null || _hotkeyMenu.DropDownItems.Count == 0) return;

            string[] thaiLabels = new string[] { "F1 (ค่าเริ่มต้น)", "F2", "F4", "PrintScreen (PrtScn)", "Ctrl + Shift + A" };
            string[] engLabels = new string[] { "F1 (Default)", "F2", "F4", "PrintScreen (PrtScn)", "Ctrl + Shift + A" };

            string[] labels = (CurrentLanguage == AppLanguage.Thai) ? thaiLabels : engLabels;

            for (int i = 0; i < _hotkeyMenu.DropDownItems.Count && i < labels.Length; i++)
            {
                _hotkeyMenu.DropDownItems[i].Text = labels[i];
            }
        }

        private void CreateHotkeySubmenu(ToolStripMenuItem parentMenu)
        {
            var options = new[]
            {
                new { Mode = GlobalKeyboardHook.ShortcutMode.F1, Label = "F1 (ค่าเริ่มต้น)" },
                new { Mode = GlobalKeyboardHook.ShortcutMode.F2, Label = "F2" },
                new { Mode = GlobalKeyboardHook.ShortcutMode.F4, Label = "F4" },
                new { Mode = GlobalKeyboardHook.ShortcutMode.PrintScreen, Label = "PrintScreen (PrtScn)" },
                new { Mode = GlobalKeyboardHook.ShortcutMode.CtrlShiftA, Label = "Ctrl + Shift + A" }
            };

            foreach (var opt in options)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(opt.Label);
                var mode = opt.Mode;
                item.Checked = (_keyboardHook != null && _keyboardHook.CurrentMode == mode) || (mode == GlobalKeyboardHook.ShortcutMode.F1);

                item.Click += (s, e) =>
                {
                    _keyboardHook.CurrentMode = mode;
                    foreach (ToolStripMenuItem sibling in parentMenu.DropDownItems)
                    {
                        sibling.Checked = (sibling == item);
                    }
                    string keyName = mode == GlobalKeyboardHook.ShortcutMode.CtrlShiftA ? "Ctrl+Shift+A" : mode.ToString();
                    if (CurrentLanguage == AppLanguage.Thai)
                    {
                        _notifyIcon.Text = string.Format("1ThaiAi Capture (กด {0} เพื่อจับภาพ)", keyName);
                    }
                    else
                    {
                        _notifyIcon.Text = string.Format("1ThaiAi Capture (Press {0} to capture)", keyName);
                    }
                };

                parentMenu.DropDownItems.Add(item);
            }
        }

        private void TriggerCapture()
        {
            // If already capturing, do not open multiple overlays
            if (_currentOverlay != null && !_currentOverlay.IsDisposed && _currentOverlay.Visible)
            {
                _currentOverlay.Activate();
                return;
            }

            try
            {
                // Capture all monitors (Virtual Screen)
                Rectangle bounds = SystemInformation.VirtualScreen;
                Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

                using (Graphics g = Graphics.FromImage(screenshot))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
                }

                _currentOverlay = new CaptureOverlayForm(screenshot, bounds, WEBSITE_URL);
                _currentOverlay.FormClosed += (s, e) => _currentOverlay = null;
                _currentOverlay.Show();
                _currentOverlay.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาดในการจับภาพหน้าจอ: " + ex.Message, "1ThaiAi Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenWebsite()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = WEBSITE_URL,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("ไม่สามารถเปิดเบราว์เซอร์ได้: " + ex.Message, "1ThaiAi Capture", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExitApplication()
        {
            if (_keyboardHook != null)
            {
                _keyboardHook.Dispose();
            }
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Application.Exit();
        }

        /// <summary>
        /// Renders crisp 20x14 Thai National Flag Bitmap
        /// </summary>
        private static Bitmap CreateThaiFlagBitmap()
        {
            int w = 20, h = 14;
            Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.None;
                float u = (float)h / 6.0f;

                // Red Top
                using (Brush red = new SolidBrush(Color.FromArgb(217, 37, 43)))
                    g.FillRectangle(red, 0, 0, w, u);
                // White Top
                using (Brush white = new SolidBrush(Color.FromArgb(245, 245, 245)))
                    g.FillRectangle(white, 0, u, w, u);
                // Blue Middle (2 units)
                using (Brush blue = new SolidBrush(Color.FromArgb(40, 45, 85)))
                    g.FillRectangle(blue, 0, u * 2, w, u * 2);
                // White Bottom
                using (Brush white = new SolidBrush(Color.FromArgb(245, 245, 245)))
                    g.FillRectangle(white, 0, u * 4, w, u);
                // Red Bottom
                using (Brush red = new SolidBrush(Color.FromArgb(217, 37, 43)))
                    g.FillRectangle(red, 0, u * 5, w, h - (u * 5));

                // 1px Border
                using (Pen border = new Pen(Color.FromArgb(120, 0, 0, 0), 1f))
                    g.DrawRectangle(border, 0, 0, w - 1, h - 1);
            }
            return bmp;
        }

        /// <summary>
        /// Renders crisp 20x14 English / UK Flag Bitmap
        /// </summary>
        private static Bitmap CreateEnglishFlagBitmap()
        {
            int w = 20, h = 14;
            Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Blue Field
                using (Brush blue = new SolidBrush(Color.FromArgb(1, 33, 105)))
                    g.FillRectangle(blue, 0, 0, w, h);

                // White Diagonals
                using (Pen whitePen = new Pen(Color.White, 3f))
                {
                    g.DrawLine(whitePen, 0, 0, w, h);
                    g.DrawLine(whitePen, 0, h, w, 0);
                }

                // Red Diagonals
                using (Pen redPen = new Pen(Color.FromArgb(200, 16, 46), 1.5f))
                {
                    g.DrawLine(redPen, 0, 0, w, h);
                    g.DrawLine(redPen, 0, h, w, 0);
                }

                // White Cross
                using (Brush white = new SolidBrush(Color.White))
                {
                    g.FillRectangle(white, (w / 2) - 2, 0, 4, h);
                    g.FillRectangle(white, 0, (h / 2) - 2, w, 4);
                }

                // Red Cross
                using (Brush red = new SolidBrush(Color.FromArgb(200, 16, 46)))
                {
                    g.FillRectangle(red, (w / 2) - 1, 0, 2, h);
                    g.FillRectangle(red, 0, (h / 2) - 1, w, 2);
                }

                // 1px Border
                using (Pen border = new Pen(Color.FromArgb(120, 0, 0, 0), 1f))
                    g.DrawRectangle(border, 0, 0, w - 1, h - 1);
            }
            return bmp;
        }

        /// <summary>
        /// Retrieves the embedded application icon or generates a stylish camera icon dynamically
        /// </summary>
        private static Icon CreateAppIcon()
        {
            try
            {
                Icon exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (exeIcon != null)
                {
                    return exeIcon;
                }
            }
            catch { }

            int size = 32;
            using (Bitmap bmp = new Bitmap(size, size))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Gradient Background Circle
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new Rectangle(0, 0, size, size),
                    Color.FromArgb(0, 150, 255),
                    Color.FromArgb(0, 80, 200),
                    LinearGradientMode.ForwardDiagonal))
                {
                    g.FillEllipse(brush, 1, 1, size - 2, size - 2);
                }

                // Inner Crosshair / Aperture
                using (Pen borderPen = new Pen(Color.FromArgb(255, 255, 255), 2f))
                {
                    g.DrawEllipse(borderPen, 7, 7, size - 14, size - 14);
                }

                // Center Point
                using (Brush centerBrush = new SolidBrush(Color.FromArgb(255, 220, 0)))
                {
                    g.FillEllipse(centerBrush, 13, 13, 6, 6);
                }

                // Lens Marks
                using (Pen markPen = new Pen(Color.FromArgb(255, 255, 255), 2f))
                {
                    g.DrawLine(markPen, 16, 3, 16, 6);
                    g.DrawLine(markPen, 16, 26, 16, 29);
                    g.DrawLine(markPen, 3, 16, 6, 16);
                    g.DrawLine(markPen, 26, 16, 29, 16);
                }

                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }
    }

    /// <summary>
    /// Global Low-Level Keyboard Hook (WH_KEYBOARD_LL)
    /// Intercepts and SUPPRESSES the hotkey so Windows/Apps never trigger default handlers (e.g. Bing Help on F1)
    /// </summary>
    public class GlobalKeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private readonly SynchronizationContext _syncContext;

        public event Action Triggered;

        public enum ShortcutMode
        {
            F1,
            F2,
            F4,
            PrintScreen,
            CtrlShiftA
        }

        public ShortcutMode CurrentMode { get; set; }

        public GlobalKeyboardHook()
        {
            _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            CurrentMode = ShortcutMode.F1;
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    bool match = false;

                    switch (CurrentMode)
                    {
                        case ShortcutMode.F1:
                            match = (vkCode == (int)Keys.F1);
                            break;
                        case ShortcutMode.F2:
                            match = (vkCode == (int)Keys.F2);
                            break;
                        case ShortcutMode.F4:
                            match = (vkCode == (int)Keys.F4);
                            break;
                        case ShortcutMode.PrintScreen:
                            match = (vkCode == (int)Keys.PrintScreen || vkCode == 44);
                            break;
                        case ShortcutMode.CtrlShiftA:
                            if (vkCode == (int)Keys.A)
                            {
                                bool ctrl = (GetAsyncKeyState((int)Keys.ControlKey) & 0x8000) != 0;
                                bool shift = (GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
                                match = (ctrl && shift);
                            }
                            break;
                    }

                    if (match)
                    {
                        // Asynchronously trigger capture on UI message loop
                        if (_syncContext != null)
                        {
                            _syncContext.Post(delegate
                            {
                                if (Triggered != null)
                                {
                                    Triggered();
                                }
                            }, null);
                        }

                        // RETURN 1 to EAT / SUPPRESS KEYPRESS!
                        // This prevents Windows or any foreground application from opening Bing Help!
                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }
    }

    #region Annotation Models

    public enum AnnotationTool
    {
        None,
        Pen,
        Arrow,
        Rectangle
    }

    public abstract class BaseAnnotation
    {
        public Color Color { get; set; }
        public float Thickness { get; set; }
        public abstract void Draw(Graphics g);
        public virtual void Translate(int dx, int dy) { }
    }

    public class PenAnnotation : BaseAnnotation
    {
        public System.Collections.Generic.List<Point> Points = new System.Collections.Generic.List<Point>();

        public override void Draw(Graphics g)
        {
            if (Points.Count < 2) return;
            using (Pen pen = new Pen(Color, Thickness))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawLines(pen, Points.ToArray());
            }
        }

        public override void Translate(int dx, int dy)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i] = new Point(Points[i].X + dx, Points[i].Y + dy);
            }
        }
    }

    public class ArrowAnnotation : BaseAnnotation
    {
        public Point Start { get; set; }
        public Point End { get; set; }

        public override void Translate(int dx, int dy)
        {
            Start = new Point(Start.X + dx, Start.Y + dy);
            End = new Point(End.X + dx, End.Y + dy);
        }

        public override void Draw(Graphics g)
        {
            if (Start == End) return;

            double dx = End.X - Start.X;
            double dy = End.Y - Start.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 3) return;

            // Normalized direction and perpendicular vector
            double ux = dx / length;
            double uy = dy / length;
            double px = -uy;
            double py = ux;

            // Bold, large arrowhead proportions
            double headLength = Math.Min(28.0, Math.Max(18.0, length * 0.45));
            double headWidth = headLength * 0.90;

            PointF tip = new PointF(End.X, End.Y);
            PointF p1 = new PointF((float)(End.X - ux * headLength + px * (headWidth / 2.0)), (float)(End.Y - uy * headLength + py * (headWidth / 2.0)));
            PointF p2 = new PointF((float)(End.X - ux * headLength - px * (headWidth / 2.0)), (float)(End.Y - uy * headLength - py * (headWidth / 2.0)));
            PointF pBack = new PointF((float)(End.X - ux * (headLength * 0.70)), (float)(End.Y - uy * (headLength * 0.70)));

            // 1. Draw Bold Shaft Line (connects smoothly into the base of the arrowhead)
            using (Pen pen = new Pen(Color, Thickness))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, (float)Start.X, (float)Start.Y, pBack.X, pBack.Y);
            }

            // 2. Draw Prominent Filled Arrowhead with sleek barbed back
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddPolygon(new PointF[] { tip, p1, pBack, p2 });
                using (Brush brush = new SolidBrush(Color))
                {
                    g.FillPath(brush, path);
                }
                using (Pen borderPen = new Pen(Color, 1.5f))
                {
                    borderPen.LineJoin = LineJoin.Round;
                    g.DrawPath(borderPen, path);
                }
            }
        }
    }

    public class RectAnnotation : BaseAnnotation
    {
        public Rectangle Rect { get; set; }

        public override void Translate(int dx, int dy)
        {
            Rect = new Rectangle(Rect.X + dx, Rect.Y + dy, Rect.Width, Rect.Height);
        }

        public override void Draw(Graphics g)
        {
            if (Rect.Width <= 0 || Rect.Height <= 0) return;
            using (Pen pen = new Pen(Color, Thickness))
            {
                pen.LineJoin = LineJoin.Miter;
                g.DrawRectangle(pen, Rect);
            }
        }
    }

    #endregion

    /// <summary>
    /// Fullscreen Borderless Transparent Overlay with Darkened Screen,
    /// Drag-to-Select box, Annotation Tools (Pen, Arrow, Rect, Undo), Dimension Badge, and Floating Toolbar
    /// </summary>
    public class CaptureOverlayForm : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct IconInfo
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref IconInfo icon);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetIconInfo(IntPtr hIcon, out IconInfo piconinfo);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private readonly Bitmap _fullScreenshot;
        private readonly Bitmap _darkenedScreenshot;
        private readonly Rectangle _screenBounds;
        private readonly string _websiteUrl;

        private enum SelectionDragMode
        {
            None,
            Create,
            Move,
            ResizeTopLeft,
            ResizeTop,
            ResizeTopRight,
            ResizeRight,
            ResizeBottomRight,
            ResizeBottom,
            ResizeBottomLeft,
            ResizeLeft
        }

        private const int HandleSize = 8;
        private const int HitTolerance = 8;

        // Selection State
        private bool _isSelecting = false;
        private bool _hasSelection = false;
        private Point _startPoint = Point.Empty;
        private Rectangle _selectionRect = Rectangle.Empty;
        private SelectionDragMode _dragMode = SelectionDragMode.None;
        private Point _dragStartPoint = Point.Empty;
        private Rectangle _dragStartRect = Rectangle.Empty;

        // Annotation State
        private AnnotationTool _currentTool = AnnotationTool.None;
        private readonly System.Collections.Generic.List<BaseAnnotation> _annotations = new System.Collections.Generic.List<BaseAnnotation>();
        private BaseAnnotation _activeAnnotation = null;
        private Color _drawColor = Color.FromArgb(245, 30, 30); // Bold Bright Vivid Red
        private float _penThickness = 5.0f;
        private float _arrowThickness = 6.0f;
        private float _rectThickness = 4.0f;
        private readonly Cursor _penCursor;

        // UI Controls
        private Panel _topToolbarPanel;
        private Panel _bottomToolbarPanel;
        private Label _dimBadgeLabel;

        // Tool Buttons for state styling
        private Button _btnPen;
        private Button _btnArrow;
        private Button _btnRect;
        private Button _btnUndo;

        public CaptureOverlayForm(Bitmap screenshot, Rectangle screenBounds, string websiteUrl)
        {
            _fullScreenshot = screenshot;
            _screenBounds = screenBounds;
            _websiteUrl = websiteUrl;

            // Generate custom Pen Cursor
            _penCursor = CreatePenCursor();

            // Precompute darkened screenshot for buttery smooth 60fps dragging
            _darkenedScreenshot = CreateDarkenedBitmap(_fullScreenshot);

            InitializeOverlayProperties();
            InitializeToolbar();
        }

        private static Cursor CreateCustomCursor(Bitmap bmp, int xHotspot, int yHotspot)
        {
            IntPtr hIcon = bmp.GetHicon();
            IconInfo info = new IconInfo();
            GetIconInfo(hIcon, out info);
            info.xHotspot = xHotspot;
            info.yHotspot = yHotspot;
            info.fIcon = false; // false = cursor
            IntPtr hCursor = CreateIconIndirect(ref info);
            DestroyIcon(hIcon);
            return new Cursor(hCursor);
        }

        private static Cursor CreatePenCursor()
        {
            try
            {
                using (Bitmap bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // Pen body pointing down-left to (1, 30)
                    GraphicsPath body = new GraphicsPath();
                    body.AddPolygon(new PointF[] {
                        new PointF(5, 23),
                        new PointF(9, 27),
                        new PointF(27, 9),
                        new PointF(23, 5)
                    });

                    GraphicsPath tip = new GraphicsPath();
                    tip.AddPolygon(new PointF[] {
                        new PointF(1, 30),
                        new PointF(5, 23),
                        new PointF(9, 27)
                    });

                    GraphicsPath cap = new GraphicsPath();
                    cap.AddPolygon(new PointF[] {
                        new PointF(23, 5),
                        new PointF(27, 9),
                        new PointF(29, 7),
                        new PointF(27, 3)
                    });

                    // 1. Outer Dark Shadow for contrast on any background
                    using (Pen shadowPen = new Pen(Color.FromArgb(180, 0, 0, 0), 2.5f))
                    {
                        shadowPen.LineJoin = LineJoin.Round;
                        g.DrawPath(shadowPen, tip);
                        g.DrawPath(shadowPen, body);
                        g.DrawPath(shadowPen, cap);
                    }

                    // 2. Fills
                    using (Brush woodBrush = new SolidBrush(Color.FromArgb(240, 210, 160)))
                    {
                        g.FillPath(woodBrush, tip);
                    }

                    // Graphite tip
                    using (GraphicsPath graphite = new GraphicsPath())
                    {
                        graphite.AddPolygon(new PointF[] {
                            new PointF(1, 30),
                            new PointF(3, 27),
                            new PointF(5, 29)
                        });
                        using (Brush darkBrush = new SolidBrush(Color.FromArgb(30, 30, 30)))
                        {
                            g.FillPath(darkBrush, graphite);
                        }
                    }

                    // Vivid Yellow Body
                    using (Brush yellowBrush = new SolidBrush(Color.FromArgb(255, 195, 0)))
                    {
                        g.FillPath(yellowBrush, body);
                    }

                    // Red Cap
                    using (Brush redBrush = new SolidBrush(Color.FromArgb(235, 45, 45)))
                    {
                        g.FillPath(redBrush, cap);
                    }

                    // Inner crisp borders
                    using (Pen stroke = new Pen(Color.FromArgb(30, 30, 30), 1f))
                    {
                        g.DrawPath(stroke, tip);
                        g.DrawPath(stroke, body);
                        g.DrawPath(stroke, cap);
                    }

                    return CreateCustomCursor(bmp, 1, 30);
                }
            }
            catch
            {
                return Cursors.Cross;
            }
        }

        private void InitializeOverlayProperties()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = _screenBounds.Location;
            Size = _screenBounds.Size;
            TopMost = true;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            Cursor = Cursors.Cross;
            KeyPreview = true;
            BackColor = Color.Black;
        }

        private static Bitmap CreateDarkenedBitmap(Bitmap source)
        {
            Bitmap dimmed = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(dimmed))
            {
                // Draw original screenshot
                g.DrawImage(source, 0, 0);

                // Draw semi-transparent dark overlay (alpha: 130)
                using (SolidBrush darkBrush = new SolidBrush(Color.FromArgb(130, 0, 0, 0)))
                {
                    g.FillRectangle(darkBrush, 0, 0, dimmed.Width, dimmed.Height);
                }
            }
            return dimmed;
        }

        private void InitializeToolbar()
        {
            bool isThai = (TrayAppContext.CurrentLanguage == AppLanguage.Thai);

            // Tooltips
            ToolTip tip = new ToolTip();
            tip.AutoPopDelay = 6000;
            tip.InitialDelay = 250;
            tip.ReshowDelay = 100;
            tip.ShowAlways = true;

            // 1. TOP TOOLBAR: Drawing & Annotation Tools (✏️ ปากกา, ➡️ ลูกศร, 🔲 กรอบ, ↩️ ย้อนกลับ)
            _topToolbarPanel = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(28, 30, 38),
                Padding = new Padding(5),
                Visible = false
            };

            _topToolbarPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen borderPen = new Pen(Color.FromArgb(0, 168, 255), 1.5f))
                {
                    Rectangle r = _topToolbarPanel.ClientRectangle;
                    r.Width -= 1;
                    r.Height -= 1;
                    e.Graphics.DrawRectangle(borderPen, r);
                }
            };

            FlowLayoutPanel topFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            _btnPen = CreateModernButton(isThai ? "✏️ ปากกา" : "✏️ Pen", Color.FromArgb(50, 52, 65), Color.FromArgb(70, 72, 90), (s, e) => ToggleTool(AnnotationTool.Pen));
            _btnArrow = CreateModernButton(isThai ? "➡️ ลูกศร" : "➡️ Arrow", Color.FromArgb(50, 52, 65), Color.FromArgb(70, 72, 90), (s, e) => ToggleTool(AnnotationTool.Arrow));
            _btnRect = CreateModernButton(isThai ? "🔲 กรอบ" : "🔲 Box", Color.FromArgb(50, 52, 65), Color.FromArgb(70, 72, 90), (s, e) => ToggleTool(AnnotationTool.Rectangle));
            _btnUndo = CreateModernButton(isThai ? "↩️ ย้อนกลับ" : "↩️ Undo", Color.FromArgb(50, 52, 65), Color.FromArgb(70, 72, 90), (s, e) => PerformUndo());

            tip.SetToolTip(_btnPen, isThai ? "ปากกา : วาดเส้นอิสระสีแดงคมชัด" : "Pen : Freehand drawing");
            tip.SetToolTip(_btnArrow, isThai ? "ลูกศร : ลากเส้นลูกศรชี้ตำแหน่ง" : "Arrow : Draw directional arrow");
            tip.SetToolTip(_btnRect, isThai ? "กรอบ : วาดกรอบสี่เหลี่ยมเน้นข้อความ" : "Box : Draw rectangle box");
            tip.SetToolTip(_btnUndo, isThai ? "ย้อนกลับ : ยกเลิกการวาดล่าสุด (Ctrl+Z)" : "Undo : Revert last annotation (Ctrl+Z)");

            topFlow.Controls.Add(_btnPen);
            topFlow.Controls.Add(_btnArrow);
            topFlow.Controls.Add(_btnRect);
            topFlow.Controls.Add(_btnUndo);

            _topToolbarPanel.Controls.Add(topFlow);
            Controls.Add(_topToolbarPanel);

            // 2. BOTTOM TOOLBAR: Action Buttons (📋 คัดลอก, 💾 บันทึก, 🌐 1ThaiAi, ✕ ปิด)
            _bottomToolbarPanel = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(28, 30, 38),
                Padding = new Padding(5),
                Visible = false
            };

            _bottomToolbarPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen borderPen = new Pen(Color.FromArgb(0, 168, 255), 1.5f))
                {
                    Rectangle r = _bottomToolbarPanel.ClientRectangle;
                    r.Width -= 1;
                    r.Height -= 1;
                    e.Graphics.DrawRectangle(borderPen, r);
                }
            };

            FlowLayoutPanel bottomFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            Button btnCopy = CreateModernButton(isThai ? "📋 คัดลอก" : "📋 Copy", Color.FromArgb(0, 130, 230), Color.FromArgb(0, 160, 255), (s, e) => PerformCopy());
            Button btnSave = CreateModernButton(isThai ? "💾 บันทึก" : "💾 Save", Color.FromArgb(40, 167, 69), Color.FromArgb(46, 190, 79), (s, e) => PerformSave());
            Button btnWeb = CreateModernButton("🌐 1ThaiAi", Color.FromArgb(108, 92, 231), Color.FromArgb(128, 110, 245), (s, e) => OpenWebsite());
            Button btnClose = CreateModernButton(isThai ? "✕ ปิด" : "✕ Close", Color.FromArgb(220, 53, 69), Color.FromArgb(240, 73, 89), (s, e) => CloseOverlay());

            tip.SetToolTip(btnCopy, isThai ? "คัดลอกรูปภาพลง Clipboard (กด Enter)" : "Copy screenshot to Clipboard (Enter)");
            tip.SetToolTip(btnSave, isThai ? "บันทึกภาพเป็นไฟล์รูปภาพ (PNG/JPG/BMP)" : "Save screenshot to image file");
            tip.SetToolTip(btnWeb, isThai ? "เปิดเว็บไซต์ 1ThaiAi.com" : "Visit 1ThaiAi.com website");
            tip.SetToolTip(btnClose, isThai ? "ปิดและยกเลิก (กด Esc)" : "Cancel & Close (Esc)");

            bottomFlow.Controls.Add(btnCopy);
            bottomFlow.Controls.Add(btnSave);
            bottomFlow.Controls.Add(btnWeb);
            bottomFlow.Controls.Add(btnClose);

            _bottomToolbarPanel.Controls.Add(bottomFlow);
            Controls.Add(_bottomToolbarPanel);

            // Dimension Badge
            _dimBadgeLabel = new Label
            {
                AutoSize = true,
                BackColor = Color.FromArgb(200, 20, 20, 25),
                ForeColor = Color.FromArgb(240, 240, 240),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Padding = new Padding(4, 2, 4, 2),
                Visible = false
            };
            Controls.Add(_dimBadgeLabel);
        }

        private Button CreateModernButton(string text, Color baseColor, Color hoverColor, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = baseColor,
                FlatStyle = FlatStyle.Flat,
                Height = 32,
                AutoSize = true,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(2),
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = true
            };

            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) =>
            {
                if ((string)btn.Tag != "active")
                    btn.BackColor = hoverColor;
            };
            btn.MouseLeave += (s, e) =>
            {
                if ((string)btn.Tag != "active")
                    btn.BackColor = baseColor;
            };
            btn.Click += onClick;
            return btn;
        }

        private void ToggleTool(AnnotationTool tool)
        {
            if (_currentTool == tool)
            {
                _currentTool = AnnotationTool.None;
            }
            else
            {
                _currentTool = tool;
            }
            UpdateToolButtonStyles();
        }

        private void UpdateToolButtonStyles()
        {
            Color defaultBg = Color.FromArgb(50, 52, 65);
            Color activeBg = Color.FromArgb(235, 45, 45); // Highlight red for active drawing tool

            SetButtonState(_btnPen, _currentTool == AnnotationTool.Pen, defaultBg, activeBg);
            SetButtonState(_btnArrow, _currentTool == AnnotationTool.Arrow, defaultBg, activeBg);
            SetButtonState(_btnRect, _currentTool == AnnotationTool.Rectangle, defaultBg, activeBg);

            if (_currentTool == AnnotationTool.Pen)
            {
                Cursor = _penCursor ?? Cursors.Cross;
            }
            else if (_currentTool == AnnotationTool.Arrow || _currentTool == AnnotationTool.Rectangle)
            {
                Cursor = Cursors.Cross;
            }
            else
            {
                Point clientPt = PointToClient(Cursor.Position);
                SelectionDragMode hit = GetHitTest(clientPt);
                Cursor = GetCursorForDragMode(hit);
            }
        }

        private SelectionDragMode GetHitTest(Point pt)
        {
            if (!_hasSelection || _selectionRect.Width <= 0 || _selectionRect.Height <= 0)
                return SelectionDragMode.None;

            Rectangle r = _selectionRect;
            int tol = Math.Min(HitTolerance, Math.Max(4, Math.Min(r.Width / 3, r.Height / 3)));

            // 1. Check 4 Corners first (corners take priority)
            if (Math.Abs(pt.X - r.Left) <= tol && Math.Abs(pt.Y - r.Top) <= tol)
                return SelectionDragMode.ResizeTopLeft;
            if (Math.Abs(pt.X - r.Right) <= tol && Math.Abs(pt.Y - r.Top) <= tol)
                return SelectionDragMode.ResizeTopRight;
            if (Math.Abs(pt.X - r.Left) <= tol && Math.Abs(pt.Y - r.Bottom) <= tol)
                return SelectionDragMode.ResizeBottomLeft;
            if (Math.Abs(pt.X - r.Right) <= tol && Math.Abs(pt.Y - r.Bottom) <= tol)
                return SelectionDragMode.ResizeBottomRight;

            // 2. Check 4 Edges
            if (Math.Abs(pt.Y - r.Top) <= tol && pt.X >= r.Left - tol && pt.X <= r.Right + tol)
                return SelectionDragMode.ResizeTop;
            if (Math.Abs(pt.Y - r.Bottom) <= tol && pt.X >= r.Left - tol && pt.X <= r.Right + tol)
                return SelectionDragMode.ResizeBottom;
            if (Math.Abs(pt.X - r.Left) <= tol && pt.Y >= r.Top - tol && pt.Y <= r.Bottom + tol)
                return SelectionDragMode.ResizeLeft;
            if (Math.Abs(pt.X - r.Right) <= tol && pt.Y >= r.Top - tol && pt.Y <= r.Bottom + tol)
                return SelectionDragMode.ResizeRight;

            // 3. Inside Selection Rect (Move mode)
            if (r.Contains(pt))
                return SelectionDragMode.Move;

            return SelectionDragMode.None;
        }

        private Cursor GetCursorForDragMode(SelectionDragMode mode)
        {
            switch (mode)
            {
                case SelectionDragMode.ResizeTopLeft:
                case SelectionDragMode.ResizeBottomRight:
                    return Cursors.SizeNWSE;
                case SelectionDragMode.ResizeTopRight:
                case SelectionDragMode.ResizeBottomLeft:
                    return Cursors.SizeNESW;
                case SelectionDragMode.ResizeTop:
                case SelectionDragMode.ResizeBottom:
                    return Cursors.SizeNS;
                case SelectionDragMode.ResizeLeft:
                case SelectionDragMode.ResizeRight:
                    return Cursors.SizeWE;
                case SelectionDragMode.Move:
                    return Cursors.SizeAll;
                default:
                    return Cursors.Cross;
            }
        }

        private void UpdateDimensionBadge(Rectangle r)
        {
            if (r.Width <= 0 || r.Height <= 0)
            {
                _dimBadgeLabel.Visible = false;
                return;
            }

            _dimBadgeLabel.Text = string.Format("{0} × {1} px", r.Width, r.Height);
            int badgeX = r.Left;
            int badgeY = r.Top - 24;
            if (badgeY < 5) badgeY = r.Top + 5;
            if (badgeX + _dimBadgeLabel.PreferredSize.Width > ClientSize.Width - 5)
                badgeX = ClientSize.Width - _dimBadgeLabel.PreferredSize.Width - 5;
            if (badgeX < 5) badgeX = 5;

            _dimBadgeLabel.Location = new Point(badgeX, badgeY);
            _dimBadgeLabel.Visible = true;
            _dimBadgeLabel.BringToFront();
        }

        private void SetButtonState(Button btn, bool isActive, Color defaultBg, Color activeBg)
        {
            if (btn == null) return;
            btn.Tag = isActive ? "active" : null;
            btn.BackColor = isActive ? activeBg : defaultBg;
        }

        private void PerformUndo()
        {
            if (_annotations.Count > 0)
            {
                _annotations.RemoveAt(_annotations.Count - 1);
                Invalidate();
            }
        }

        #region Mouse & Keyboard Interaction

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                // 1. If user has chosen an annotation tool and clicks inside the selection area
                if (_hasSelection && _currentTool != AnnotationTool.None && _selectionRect.Contains(e.Location))
                {
                    _startPoint = e.Location;
                    if (_currentTool == AnnotationTool.Pen)
                    {
                        PenAnnotation penAnn = new PenAnnotation
                        {
                            Color = _drawColor,
                            Thickness = _penThickness
                        };
                        penAnn.Points.Add(e.Location);
                        _activeAnnotation = penAnn;
                    }
                    else if (_currentTool == AnnotationTool.Arrow)
                    {
                        _activeAnnotation = new ArrowAnnotation
                        {
                            Color = _drawColor,
                            Thickness = _arrowThickness,
                            Start = e.Location,
                            End = e.Location
                        };
                    }
                    else if (_currentTool == AnnotationTool.Rectangle)
                    {
                        _activeAnnotation = new RectAnnotation
                        {
                            Color = _drawColor,
                            Thickness = _rectThickness,
                            Rect = new Rectangle(e.Location, Size.Empty)
                        };
                    }
                    Invalidate();
                    return;
                }

                // 2. If user already has a selection and clicks on handles/borders or inside to move
                if (_hasSelection && _currentTool == AnnotationTool.None)
                {
                    SelectionDragMode hit = GetHitTest(e.Location);
                    if (hit != SelectionDragMode.None)
                    {
                        _dragMode = hit;
                        _dragStartPoint = e.Location;
                        _dragStartRect = _selectionRect;

                        // Temporarily hide toolbars during drag/resize for clean view
                        _topToolbarPanel.Visible = false;
                        _bottomToolbarPanel.Visible = false;

                        // Show live dimension badge
                        UpdateDimensionBadge(_selectionRect);
                        return;
                    }
                }

                // 3. Otherwise, start a new selection rectangle
                _dragMode = SelectionDragMode.Create;
                _isSelecting = true;
                _hasSelection = false;
                _currentTool = AnnotationTool.None;
                UpdateToolButtonStyles();
                _annotations.Clear();
                _startPoint = e.Location;
                _selectionRect = new Rectangle(e.Location, Size.Empty);

                _topToolbarPanel.Visible = false;
                _bottomToolbarPanel.Visible = false;
                _dimBadgeLabel.Visible = false;
                Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                // If dragging/resizing, cancel drag and restore previous rect
                if (_dragMode != SelectionDragMode.None && _dragMode != SelectionDragMode.Create)
                {
                    _selectionRect = _dragStartRect;
                    _dragMode = SelectionDragMode.None;
                    PositionToolbar();
                    _dimBadgeLabel.Visible = false;
                    Invalidate();
                    return;
                }

                // If tool is active, right click returns to normal mode
                if (_currentTool != AnnotationTool.None)
                {
                    _currentTool = AnnotationTool.None;
                    UpdateToolButtonStyles();
                    return;
                }

                // If annotations exist, right click undos last annotation
                if (_annotations.Count > 0)
                {
                    PerformUndo();
                    return;
                }

                // Right click cancels selection if active, or closes overlay
                if (_hasSelection)
                {
                    _hasSelection = false;
                    _selectionRect = Rectangle.Empty;
                    _topToolbarPanel.Visible = false;
                    _bottomToolbarPanel.Visible = false;
                    _dimBadgeLabel.Visible = false;
                    Cursor = Cursors.Cross;
                    Invalidate();
                }
                else
                {
                    CloseOverlay();
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // 1. Active Annotation Drawing
            if (_activeAnnotation != null)
            {
                // Constrain drawing within selection rect bounds
                int cx = Math.Max(_selectionRect.Left, Math.Min(e.X, _selectionRect.Right));
                int cy = Math.Max(_selectionRect.Top, Math.Min(e.Y, _selectionRect.Bottom));
                Point constrainedPoint = new Point(cx, cy);

                PenAnnotation penAnn = _activeAnnotation as PenAnnotation;
                if (penAnn != null)
                {
                    penAnn.Points.Add(constrainedPoint);
                }
                else
                {
                    ArrowAnnotation arrowAnn = _activeAnnotation as ArrowAnnotation;
                    if (arrowAnn != null)
                    {
                        arrowAnn.End = constrainedPoint;
                    }
                    else
                    {
                        RectAnnotation rectAnn = _activeAnnotation as RectAnnotation;
                        if (rectAnn != null)
                        {
                            int rx = Math.Min(_startPoint.X, constrainedPoint.X);
                            int ry = Math.Min(_startPoint.Y, constrainedPoint.Y);
                            int rw = Math.Abs(_startPoint.X - constrainedPoint.X);
                            int rh = Math.Abs(_startPoint.Y - constrainedPoint.Y);
                            rectAnn.Rect = new Rectangle(rx, ry, rw, rh);
                        }
                    }
                }

                Invalidate();
                return;
            }

            // 2. Initial Selection Dragging
            if (_dragMode == SelectionDragMode.Create || _isSelecting)
            {
                int x = Math.Min(_startPoint.X, e.X);
                int y = Math.Min(_startPoint.Y, e.Y);
                int width = Math.Abs(_startPoint.X - e.X);
                int height = Math.Abs(_startPoint.Y - e.Y);

                _selectionRect = new Rectangle(x, y, width, height);
                UpdateDimensionBadge(_selectionRect);
                Invalidate();
                return;
            }

            // 3. Moving the Selection Box
            if (_dragMode == SelectionDragMode.Move)
            {
                int dx = e.X - _dragStartPoint.X;
                int dy = e.Y - _dragStartPoint.Y;

                int newX = _dragStartRect.X + dx;
                int newY = _dragStartRect.Y + dy;

                if (newX < 0) newX = 0;
                if (newY < 0) newY = 0;
                if (newX + _dragStartRect.Width > ClientSize.Width)
                    newX = ClientSize.Width - _dragStartRect.Width;
                if (newY + _dragStartRect.Height > ClientSize.Height)
                    newY = ClientSize.Height - _dragStartRect.Height;

                int actualDx = newX - _selectionRect.X;
                int actualDy = newY - _selectionRect.Y;

                _selectionRect = new Rectangle(newX, newY, _dragStartRect.Width, _dragStartRect.Height);

                if (actualDx != 0 || actualDy != 0)
                {
                    foreach (BaseAnnotation ann in _annotations)
                    {
                        ann.Translate(actualDx, actualDy);
                    }
                }

                UpdateDimensionBadge(_selectionRect);
                Invalidate();
                return;
            }

            // 4. Resizing the Selection Box
            if (_dragMode != SelectionDragMode.None)
            {
                int left = _dragStartRect.Left;
                int top = _dragStartRect.Top;
                int right = _dragStartRect.Right;
                int bottom = _dragStartRect.Bottom;

                const int minSize = 10;

                // Horizontal
                switch (_dragMode)
                {
                    case SelectionDragMode.ResizeLeft:
                    case SelectionDragMode.ResizeTopLeft:
                    case SelectionDragMode.ResizeBottomLeft:
                        left = Math.Min(Math.Max(0, e.X), right - minSize);
                        break;

                    case SelectionDragMode.ResizeRight:
                    case SelectionDragMode.ResizeTopRight:
                    case SelectionDragMode.ResizeBottomRight:
                        right = Math.Max(Math.Min(ClientSize.Width, e.X), left + minSize);
                        break;
                }

                // Vertical
                switch (_dragMode)
                {
                    case SelectionDragMode.ResizeTop:
                    case SelectionDragMode.ResizeTopLeft:
                    case SelectionDragMode.ResizeTopRight:
                        top = Math.Min(Math.Max(0, e.Y), bottom - minSize);
                        break;

                    case SelectionDragMode.ResizeBottom:
                    case SelectionDragMode.ResizeBottomLeft:
                    case SelectionDragMode.ResizeBottomRight:
                        bottom = Math.Max(Math.Min(ClientSize.Height, e.Y), top + minSize);
                        break;
                }

                _selectionRect = new Rectangle(left, top, right - left, bottom - top);
                UpdateDimensionBadge(_selectionRect);
                Invalidate();
                return;
            }

            // 5. Hovering (no mouse button pressed) - Update Cursor dynamically
            if (_hasSelection && _currentTool == AnnotationTool.None)
            {
                SelectionDragMode hit = GetHitTest(e.Location);
                Cursor = GetCursorForDragMode(hit);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            // Finalize Active Annotation
            if (_activeAnnotation != null)
            {
                _annotations.Add(_activeAnnotation);
                _activeAnnotation = null;
                Invalidate();
                return;
            }

            // Finalize Selection or Drag/Resize
            if (_dragMode != SelectionDragMode.None || _isSelecting)
            {
                _isSelecting = false;
                _dragMode = SelectionDragMode.None;

                // Only consider valid selection if larger than 8x8 pixels
                if (_selectionRect.Width > 8 && _selectionRect.Height > 8)
                {
                    _hasSelection = true;
                    _dimBadgeLabel.Visible = false; // Hide badge when selection finishes
                    PositionToolbar();
                    if (_currentTool == AnnotationTool.None)
                    {
                        Cursor = GetCursorForDragMode(GetHitTest(e.Location));
                    }
                }
                else
                {
                    _hasSelection = false;
                    _selectionRect = Rectangle.Empty;
                    _topToolbarPanel.Visible = false;
                    _bottomToolbarPanel.Visible = false;
                    _dimBadgeLabel.Visible = false;
                    Cursor = Cursors.Cross;
                }
                Invalidate();
            }
        }

        private void PositionToolbar()
        {
            _topToolbarPanel.Visible = true;
            _topToolbarPanel.BringToFront();

            _bottomToolbarPanel.Visible = true;
            _bottomToolbarPanel.BringToFront();

            // 1. Position Top Toolbar (Drawing & Annotation Tools) ABOVE selection box
            int topWidth = _topToolbarPanel.PreferredSize.Width;
            int topHeight = _topToolbarPanel.PreferredSize.Height;
            int topX = _selectionRect.Left;
            int topY = _selectionRect.Top - topHeight - 8;

            if (topX < 5) topX = 5;
            if (topX + topWidth > ClientSize.Width - 10) topX = ClientSize.Width - topWidth - 10;
            if (topX < 5) topX = 5;

            // If goes off top of screen, place inside selection box at top
            if (topY < 5)
            {
                topY = _selectionRect.Top + 6;
            }

            _topToolbarPanel.Location = new Point(topX, topY);

            // 2. Position Bottom Toolbar (Action Buttons) BELOW selection box
            int bottomWidth = _bottomToolbarPanel.PreferredSize.Width;
            int bottomHeight = _bottomToolbarPanel.PreferredSize.Height;
            int bottomX = _selectionRect.Right - bottomWidth;
            int bottomY = _selectionRect.Bottom + 8;

            if (bottomX < 5) bottomX = _selectionRect.Left;
            if (bottomX + bottomWidth > ClientSize.Width - 10) bottomX = ClientSize.Width - bottomWidth - 10;
            if (bottomX < 5) bottomX = 5;

            // If goes off bottom of screen, place inside selection box at bottom
            if (bottomY + bottomHeight > ClientSize.Height - 10)
            {
                bottomY = _selectionRect.Bottom - bottomHeight - 6;
                if (bottomY < topY + topHeight + 4)
                {
                    bottomY = topY + topHeight + 4;
                }
            }

            _bottomToolbarPanel.Location = new Point(bottomX, bottomY);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Escape)
            {
                if (_currentTool != AnnotationTool.None)
                {
                    _currentTool = AnnotationTool.None;
                    UpdateToolButtonStyles();
                }
                else
                {
                    CloseOverlay();
                }
            }
            else if (e.KeyCode == Keys.Enter && _hasSelection)
            {
                PerformCopy();
            }
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                PerformUndo();
            }
        }

        #endregion

        #region Painting & Rendering

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw darkened background
            g.DrawImage(_darkenedScreenshot, 0, 0);

            // Draw bright clear original screenshot in selected region
            if ((_isSelecting || _hasSelection || _dragMode != SelectionDragMode.None) && _selectionRect.Width > 0 && _selectionRect.Height > 0)
            {
                g.DrawImage(_fullScreenshot, _selectionRect, _selectionRect, GraphicsUnit.Pixel);

                // Clip & Draw all saved annotations
                Region originalClip = g.Clip;
                g.SetClip(_selectionRect);

                foreach (BaseAnnotation ann in _annotations)
                {
                    ann.Draw(g);
                }

                // Draw currently active dragging annotation
                if (_activeAnnotation != null)
                {
                    _activeAnnotation.Draw(g);
                }

                g.Clip = originalClip;

                // Draw stylish glowing/sharp selection borders
                using (Pen outerPen = new Pen(Color.FromArgb(0, 168, 255), 2f))
                {
                    g.DrawRectangle(outerPen, _selectionRect);
                }

                using (Pen innerPen = new Pen(Color.FromArgb(220, 255, 255, 255), 1f))
                {
                    innerPen.DashStyle = DashStyle.Dash;
                    Rectangle innerRect = new Rectangle(_selectionRect.X + 1, _selectionRect.Y + 1, Math.Max(0, _selectionRect.Width - 2), Math.Max(0, _selectionRect.Height - 2));
                    g.DrawRectangle(innerPen, innerRect);
                }

                // Draw Anchor Dots on corners and edges
                DrawSelectionHandles(g, _selectionRect);
            }
        }

        private void DrawSelectionHandles(Graphics g, Rectangle r)
        {
            int hSize = HandleSize;
            using (Brush handleBrush = new SolidBrush(Color.FromArgb(0, 168, 255)))
            using (Pen handleBorder = new Pen(Color.White, 1.5f))
            {
                Point[] handles;
                if (r.Width > 24 && r.Height > 24)
                {
                    handles = new Point[]
                    {
                        new Point(r.Left, r.Top),
                        new Point(r.Left + r.Width / 2, r.Top),
                        new Point(r.Right, r.Top),
                        new Point(r.Right, r.Top + r.Height / 2),
                        new Point(r.Right, r.Bottom),
                        new Point(r.Left + r.Width / 2, r.Bottom),
                        new Point(r.Left, r.Bottom),
                        new Point(r.Left, r.Top + r.Height / 2)
                    };
                }
                else
                {
                    handles = new Point[]
                    {
                        new Point(r.Left, r.Top),
                        new Point(r.Right, r.Top),
                        new Point(r.Right, r.Bottom),
                        new Point(r.Left, r.Bottom)
                    };
                }

                foreach (Point pt in handles)
                {
                    Rectangle hr = new Rectangle(pt.X - hSize / 2, pt.Y - hSize / 2, hSize, hSize);
                    g.FillRectangle(handleBrush, hr);
                    g.DrawRectangle(handleBorder, hr);
                }
            }
        }

        #endregion

        #region Actions (Copy, Save, Web, Close)

        private Bitmap GetSelectedCroppedImage()
        {
            if (!_hasSelection || _selectionRect.Width <= 0 || _selectionRect.Height <= 0)
                return null;

            Bitmap cropped = new Bitmap(_selectionRect.Width, _selectionRect.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 1. Draw base screenshot cropped region
                g.DrawImage(_fullScreenshot, new Rectangle(0, 0, _selectionRect.Width, _selectionRect.Height), _selectionRect, GraphicsUnit.Pixel);

                // 2. Draw annotations translated to cropped coordinates
                g.TranslateTransform(-_selectionRect.X, -_selectionRect.Y);
                foreach (BaseAnnotation ann in _annotations)
                {
                    ann.Draw(g);
                }
            }
            return cropped;
        }

        private void PerformCopy()
        {
            using (Bitmap cropped = GetSelectedCroppedImage())
            {
                if (cropped != null)
                {
                    Clipboard.SetImage(cropped);
                }
            }
            CloseOverlay();
        }

        private void PerformSave()
        {
            using (Bitmap cropped = GetSelectedCroppedImage())
            {
                if (cropped == null)
                {
                    CloseOverlay();
                    return;
                }

                // Hide overlay temporarily while showing SaveFileDialog
                Hide();

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    bool isThai = (TrayAppContext.CurrentLanguage == AppLanguage.Thai);
                    sfd.Title = isThai ? "บันทึกภาพหน้าจอ - 1ThaiAi Capture" : "Save Screenshot - 1ThaiAi Capture";
                    sfd.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg;*.jpeg)|*.jpg|Bitmap Image (*.bmp)|*.bmp";

                    string defaultExt = "png";
                    int filterIndex = 1;
                    if (TrayAppContext.DefaultFormat == ImageSaveFormat.JPG)
                    {
                        defaultExt = "jpg";
                        filterIndex = 2;
                    }
                    else if (TrayAppContext.DefaultFormat == ImageSaveFormat.BMP)
                    {
                        defaultExt = "bmp";
                        filterIndex = 3;
                    }

                    sfd.FilterIndex = filterIndex;
                    sfd.DefaultExt = defaultExt;
                    sfd.FileName = string.Format("Capture_{0:yyyyMMdd_HHmmss}.{1}", DateTime.Now, defaultExt);

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        ImageFormat format = ImageFormat.Png;
                        string ext = Path.GetExtension(sfd.FileName).ToLower();
                        if (ext == ".jpg" || ext == ".jpeg") format = ImageFormat.Jpeg;
                        else if (ext == ".bmp") format = ImageFormat.Bmp;

                        cropped.Save(sfd.FileName, format);
                    }
                }
            }

            CloseOverlay();
        }

        private void OpenWebsite()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _websiteUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("ไม่สามารถเปิดเว็บเบราว์เซอร์ได้: " + ex.Message, "1ThaiAi Capture", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                CloseOverlay();
            }
        }

        private void CloseOverlay()
        {
            Close();
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_penCursor != null) _penCursor.Dispose();
                if (_fullScreenshot != null) _fullScreenshot.Dispose();
                if (_darkenedScreenshot != null) _darkenedScreenshot.Dispose();
                if (_topToolbarPanel != null) _topToolbarPanel.Dispose();
                if (_bottomToolbarPanel != null) _bottomToolbarPanel.Dispose();
                if (_dimBadgeLabel != null) _dimBadgeLabel.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
