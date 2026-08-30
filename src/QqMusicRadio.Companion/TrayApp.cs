using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using QqMusicRadio.Companion.Audio;

namespace QqMusicRadio.Companion;

/// <summary>Tray icon + context menu; runs the WinForms message loop.</summary>
public sealed class TrayApp : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly System.Windows.Forms.Timer _timer;

    public TrayApp(Config config, CompanionState state, CaptureManager capture, EncoderHolder encoder, Func<string> shortStatus, int streamPort)
    {
        var menu = new ContextMenuStrip();
        var statusItem = new ToolStripMenuItem("Cab Radio") { Enabled = false };
        var deviceMenu = new ToolStripMenuItem("采集设备 / Device");
        var bitrateMenu = new ToolStripMenuItem("码率 / Bitrate");
        var openItem = new ToolStripMenuItem("打开状态页 / Status page");
        var autoItem = new ToolStripMenuItem("跟随游戏自动启停 / Follow game") { Checked = state.AutoMode, CheckOnClick = true };
        var autostartItem = new ToolStripMenuItem("开机自启 / Run at startup") { Checked = Autostart.IsEnabled(), CheckOnClick = true };
        var exitItem = new ToolStripMenuItem("退出 / Exit");

        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(deviceMenu);
        menu.Items.Add(bitrateMenu);
        menu.Items.Add(openItem);
        menu.Items.Add(autoItem);
        menu.Items.Add(autostartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        openItem.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo($"http://127.0.0.1:{streamPort}/") { UseShellExecute = true }); }
            catch { /* ignore */ }
        };
        autoItem.CheckedChanged += (_, _) =>
        {
            state.AutoMode = autoItem.Checked;
            config.AutoMode = autoItem.Checked;
            config.Save();
        };
        autostartItem.CheckedChanged += (_, _) => Autostart.SetEnabled(autostartItem.Checked);
        exitItem.Click += (_, _) => Application.Exit();

        // Bitrate switcher: 96-320 kbps, encoder rebuilt on change.
        bitrateMenu.DropDownOpening += (_, _) =>
        {
            bitrateMenu.DropDownItems.Clear();
            foreach (var br in new[] { 96, 128, 192, 256, 320 })
            {
                var item = new ToolStripMenuItem($"{br} kbps")
                {
                    Checked = config.Bitrate == br,
                    CheckOnClick = false
                };
                item.Click += (_, _) =>
                {
                    config.Bitrate = br;
                    config.Save();
                    encoder.SetBitrate(br);
                    Log.Info($"[bitrate] switched to {br}kbps");
                };
                bitrateMenu.DropDownItems.Add(item);
            }
        };

        // Device switcher: enumerate capture endpoints on menu open.
        deviceMenu.DropDownOpening += (_, _) =>
        {
            deviceMenu.DropDownItems.Clear();
            var devices = DeviceList.GetCaptureDeviceNames();
            foreach (var name in devices)
            {
                var item = new ToolStripMenuItem(name)
                {
                    Checked = name.Contains(config.DeviceName, StringComparison.OrdinalIgnoreCase),
                    CheckOnClick = false
                };
                item.Click += (_, _) =>
                {
                    config.DeviceName = name;
                    config.Save();
                    capture.SetDeviceFilter(name);
                    Log.Info($"[device] switched to '{name}'");
                };
                deviceMenu.DropDownItems.Add(item);
            }
            if (devices.Count == 0)
            {
                var none = new ToolStripMenuItem("（无可用录制设备 / No capture devices）") { Enabled = false };
                deviceMenu.DropDownItems.Add(none);
            }
        };

        _icon = new NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "Cab Radio · 驾驶室电台",
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => openItem.PerformClick();

        // Single-line status only: the menu item and tooltip stay compact.
        _timer = new System.Windows.Forms.Timer { Interval = 3000 };
        _timer.Tick += (_, _) =>
        {
            var text = shortStatus();
            statusItem.Text = text;
            _icon.Text = text.Length > 63 ? text[..63] : text;
        };
        _timer.Start();
    }

    public void Run() => Application.Run();

    public void Dispose()
    {
        _timer.Stop();
        _icon.Visible = false;
        _icon.Dispose();
    }

    private static Icon CreateTrayIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(31, 31, 46));
            using var font = new Font("Arial", 15f, FontStyle.Bold);
            g.DrawString("Q", font, Brushes.White, 8f, 7f);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }
}
