using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace DeskNote;

internal sealed class NoteForm : Form
{
    private const int HotKeyId = 0x4E31;
    private const int SaveDelayMs = 600;

    private static readonly Palette LightPalette = new(
        Paper: Color.FromArgb(251, 250, 247),
        Ink: Color.FromArgb(33, 33, 33),
        Footer: Color.FromArgb(241, 240, 235),
        Muted: Color.FromArgb(128, 128, 124),
        Hover: Color.FromArgb(231, 230, 224),
        Hint: Color.FromArgb(165, 165, 160));

    private static readonly Palette DarkPalette = new(
        Paper: Color.FromArgb(30, 30, 34),
        Ink: Color.FromArgb(228, 228, 232),
        Footer: Color.FromArgb(23, 23, 27),
        Muted: Color.FromArgb(140, 140, 150),
        Hover: Color.FromArgb(46, 46, 53),
        Hint: Color.FromArgb(140, 140, 150));

    private readonly TextBox _editor;
    private readonly Label _hint;
    private readonly Label _status;
    private readonly Button _themeButton;
    private readonly Panel _footer;
    private readonly Panel _editorHost;
    private readonly System.Windows.Forms.Timer _saveTimer;
    private readonly ToolTip _toolTip = new();
    private readonly string _notePath;
    private readonly string _settingsPath;

    private Palette _palette = LightPalette;
    private readonly bool _startHidden;
    private bool _dark;
    private bool _hotkeyRegistered;
    private bool _suppressDeactivate;
    private bool _dirty;

    public NoteForm(bool startHidden = false)
    {
        _startHidden = startHidden;

        var appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DeskNote");
        _notePath = Path.Combine(appDir, "note.txt");
        _settingsPath = Path.Combine(appDir, "settings.json");
        _dark = LoadTheme() == "dark";

        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "便签";
        Font = new Font("Microsoft YaHei UI", 10.5f);
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(420, 540);
        MinimumSize = new Size(300, 220);
        ShowInTaskbar = false;

        Icon = LoadAppIcon();

        _editor = new TextBox
        {
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            WordWrap = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 10.5f),
            HideSelection = false,
        };
        _hint = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(2, 3, 0, 0),
            Font = new Font("Microsoft YaHei UI", 10.5f),
            Text = "随手记点什么…（支持换行，自动保存）",
            Cursor = Cursors.IBeam,
        };
        _hint.Click += (_, _) => _editor.Focus();

        _editor.TextChanged += (_, _) =>
        {
            _hint.Visible = _editor.TextLength == 0 && !_editor.Focused;
            MarkDirty();
        };
        _editor.Enter += (_, _) => _hint.Visible = false;
        _editor.Leave += (_, _) => _hint.Visible = _editor.TextLength == 0;

        _editorHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 12, 9, 9),
        };
        _editorHost.Controls.Add(_editor);
        _editorHost.Controls.Add(_hint);
        _hint.BringToFront();

        _status = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Microsoft YaHei UI", 9f),
            Text = "Alt+F3 呼出 · 失焦自动靠后 · 内容自动保存",
            AutoEllipsis = true,
        };

        _themeButton = new Button
        {
            Dock = DockStyle.Right,
            Width = 58,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 9f),
            TextAlign = ContentAlignment.MiddleCenter,
            TabStop = false,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        _themeButton.FlatAppearance.BorderSize = 0;
        _themeButton.Click += (_, _) => ToggleTheme();
        _toolTip.SetToolTip(_themeButton, "切换亮色 / 暗色（Ctrl+T）");

        _footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            Padding = new Padding(12, 0, 6, 0),
        };
        _footer.Controls.Add(_status);
        _footer.Controls.Add(_themeButton);

        Controls.Add(_editorHost);
        Controls.Add(_footer);

        _saveTimer = new System.Windows.Forms.Timer { Interval = SaveDelayMs };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            SaveNote();
        };

        ActiveControl = _editor;
        ApplyTheme();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LoadNote();
    }

    protected override void SetVisibleCore(bool value)
    {
        if (!IsHandleCreated && _startHidden)
        {
            CreateHandle();
            value = false;
        }

        base.SetVisibleCore(value);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ApplyTitleBarTheme();
        ShowInFront();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        ApplyTitleBarTheme();

        _hotkeyRegistered = NativeMethods.RegisterHotKey(
            Handle,
            HotKeyId,
            NativeMethods.ModAlt,
            NativeMethods.VkF3);

        if (!_hotkeyRegistered)
        {
            ShowInTaskbar = true;
            _status.ForeColor = Color.FromArgb(196, 92, 78);
            _status.Text = "Alt+F3 注册失败：已被占用，暂在任务栏显示";
            Text = "便签（Alt+F3 不可用）";
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (_hotkeyRegistered)
        {
            NativeMethods.UnregisterHotKey(Handle, HotKeyId);
            _hotkeyRegistered = false;
        }

        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotKey && m.WParam.ToInt32() == HotKeyId)
        {
            ToggleWindow();
            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (!_suppressDeactivate)
        {
            TopMost = true;
        }
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);

        if (_suppressDeactivate || !Visible)
        {
            return;
        }

        SaveNote();
        TopMost = false;
        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HwndBottom,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SaveNote();
        base.OnFormClosing(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            HideToBack();
            return true;
        }

        if (keyData == (Keys.Control | Keys.S))
        {
            SaveNote();
            return true;
        }

        if (keyData == (Keys.Control | Keys.T))
        {
            ToggleTheme();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static Icon? LoadAppIcon()
    {
        try
        {
            using var stream = typeof(NoteForm).Assembly.GetManifestResourceStream("DeskNote.app.ico");
            if (stream is null)
            {
                return null;
            }

            return new Icon(stream, 32, 32);
        }
        catch (Exception ex) when (ex is IOException || ex is ArgumentException)
        {
            return null;
        }
    }

    private void ToggleTheme()
    {
        _dark = !_dark;
        ApplyTheme();
        SaveTheme();
    }

    private void ApplyTheme()
    {
        _palette = _dark ? DarkPalette : LightPalette;

        BackColor = _palette.Paper;
        _editorHost.BackColor = _palette.Paper;
        _editor.BackColor = _palette.Paper;
        _editor.ForeColor = _palette.Ink;

        _hint.BackColor = _palette.Paper;
        _hint.ForeColor = _palette.Hint;

        _footer.BackColor = _palette.Footer;
        _status.BackColor = _palette.Footer;
        _status.ForeColor = _palette.Muted;

        _themeButton.BackColor = _palette.Footer;
        _themeButton.ForeColor = _palette.Muted;
        _themeButton.FlatAppearance.MouseOverBackColor = _palette.Hover;
        _themeButton.FlatAppearance.MouseDownBackColor = _palette.Hover;
        _themeButton.Text = _dark ? "亮色" : "暗色";

        _toolTip.SetToolTip(_themeButton, _dark ? "切换到亮色主题（Ctrl+T）" : "切换到暗色主题（Ctrl+T）");

        if (IsHandleCreated)
        {
            ApplyTitleBarTheme();
            NativeMethods.SetWindowTheme(_editor.Handle, _dark ? "DarkMode_Explorer" : "Explorer", null);
        }

        Invalidate(true);
    }

    private void ApplyTitleBarTheme()
    {
        var enabled = _dark ? 1 : 0;
        var result = NativeMethods.DwmSetWindowAttribute(
            Handle,
            NativeMethods.DwmUseImmersiveDarkMode,
            ref enabled,
            sizeof(int));

        if (result != 0)
        {
            enabled = _dark ? 1 : 0;
            NativeMethods.DwmSetWindowAttribute(
                Handle,
                NativeMethods.DwmUseImmersiveDarkModeLegacy,
                ref enabled,
                sizeof(int));
        }

        NativeMethods.SetWindowPos(
            Handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove
            | NativeMethods.SwpNoSize
            | NativeMethods.SwpNoZOrder
            | NativeMethods.SwpNoActivate
            | NativeMethods.SwpFrameChanged);
    }

    private string LoadTheme()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return "light";
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath));
            return settings?.Theme == "dark" ? "dark" : "light";
        }
        catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
        {
            return "light";
        }
    }

    private void SaveTheme()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            var json = JsonSerializer.Serialize(
                new AppSettings { Theme = _dark ? "dark" : "light" },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            _status.ForeColor = Color.FromArgb(196, 92, 78);
            _status.Text = "保存主题设置失败：" + ex.Message;
        }
    }

    private void ToggleWindow()
    {
        if (Visible && ContainsFocus)
        {
            HideToBack();
            return;
        }

        ShowInFront();
    }

    private void ShowInFront()
    {
        _suppressDeactivate = true;
        try
        {
            if (!Visible)
            {
                Show();
            }

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            TopMost = true;
            ForceForeground();
            _editor.Focus();
        }
        finally
        {
            _suppressDeactivate = false;
        }
    }

    private void HideToBack()
    {
        _suppressDeactivate = true;
        try
        {
            SaveNote();
            Hide();
        }
        finally
        {
            _suppressDeactivate = false;
        }
    }

    private void ForceForeground()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        var foregroundThread = NativeMethods.GetWindowThreadProcessId(foreground, IntPtr.Zero);
        var currentThread = NativeMethods.GetCurrentThreadId();
        var attached = false;

        try
        {
            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                attached = NativeMethods.AttachThreadInput(currentThread, foregroundThread, true);
            }

            NativeMethods.SetWindowPos(
                Handle,
                NativeMethods.HwndTopMost,
                0,
                0,
                0,
                0,
                NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpShowWindow);

            NativeMethods.SetForegroundWindow(Handle);
            Activate();
        }
        finally
        {
            if (attached)
            {
                NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
    }

    private void MarkDirty()
    {
        _dirty = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void LoadNote()
    {
        try
        {
            if (File.Exists(_notePath))
            {
                _editor.Text = File.ReadAllText(_notePath);
                _dirty = false;
            }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            _status.ForeColor = Color.FromArgb(196, 92, 78);
            _status.Text = "读取便签失败：" + ex.Message;
        }

        _hint.Visible = _editor.TextLength == 0 && !_editor.Focused;
        _editor.SelectionStart = _editor.TextLength;
        _editor.SelectionLength = 0;
    }

    private void SaveNote()
    {
        if (!_dirty)
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(_notePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_notePath, _editor.Text);
            _dirty = false;
            _status.ForeColor = _palette.Muted;
            _status.Text = "已保存 " + DateTime.Now.ToString("HH:mm:ss") + " · Alt+F3 呼出";
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            _status.ForeColor = Color.FromArgb(196, 92, 78);
            _status.Text = "保存失败：" + ex.Message;
        }
    }

    private sealed record Palette(
        Color Paper,
        Color Ink,
        Color Footer,
        Color Muted,
        Color Hover,
        Color Hint);

    private sealed class AppSettings
    {
        public string Theme { get; set; } = "light";
    }
}
