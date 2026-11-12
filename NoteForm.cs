using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using WpfApplicationCommands = System.Windows.Input.ApplicationCommands;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfContextMenu = System.Windows.Controls.ContextMenu;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDataObject = System.Windows.DataObject;
using WpfDataObjectPastingEventArgs = System.Windows.DataObjectPastingEventArgs;
using WpfDocument = System.Windows.Documents.FlowDocument;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfFormattedText = System.Windows.Media.FormattedText;
using WpfFlowDirection = System.Windows.FlowDirection;
using WpfKey = System.Windows.Input.Key;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfKeyboard = System.Windows.Input.Keyboard;
using WpfMenuItem = System.Windows.Controls.MenuItem;
using WpfModifierKeys = System.Windows.Input.ModifierKeys;
using WpfParagraph = System.Windows.Documents.Paragraph;
using WpfRichTextBox = System.Windows.Controls.RichTextBox;
using WpfRun = System.Windows.Documents.Run;
using WpfScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility;
using WpfScrollBar = System.Windows.Controls.Primitives.ScrollBar;
using WpfScrollViewer = System.Windows.Controls.ScrollViewer;
using WpfSeparator = System.Windows.Controls.Separator;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfStyle = System.Windows.Style;
using WpfTextRange = System.Windows.Documents.TextRange;
using WpfThickness = System.Windows.Thickness;
using WpfTypeface = System.Windows.Media.Typeface;
using WpfVisualTreeHelper = System.Windows.Media.VisualTreeHelper;
using WpfXamlReader = System.Windows.Markup.XamlReader;

namespace DeskNote;

internal sealed class NoteForm : Form
{
    private const int HotKeyId = 0x4E31;
    private const int SaveDelayMs = 600;
    private static readonly double[] EntrySizes = [14, 15, 14.5, 15, 14, 14.5];

    private static readonly Palette LightPalette = new(
        Paper: Color.FromArgb(251, 250, 247),
        Ink: Color.FromArgb(33, 33, 33),
        Footer: Color.FromArgb(241, 240, 235),
        Muted: Color.FromArgb(128, 128, 124),
        Hover: Color.FromArgb(231, 230, 224),
        Hint: Color.FromArgb(165, 165, 160),
        Tints:
        [
            Color.FromArgb(255, 242, 222),
            Color.FromArgb(228, 241, 252),
            Color.FromArgb(230, 246, 233),
            Color.FromArgb(252, 233, 231),
            Color.FromArgb(242, 235, 250),
            Color.FromArgb(238, 241, 245),
        ],
        EntryInks:
        [
            Color.FromArgb(45, 38, 28),
            Color.FromArgb(30, 38, 48),
            Color.FromArgb(32, 42, 34),
            Color.FromArgb(46, 32, 33),
            Color.FromArgb(40, 33, 48),
            Color.FromArgb(36, 38, 40),
        ]);

    private static readonly Palette DarkPalette = new(
        Paper: Color.FromArgb(30, 30, 34),
        Ink: Color.FromArgb(228, 228, 232),
        Footer: Color.FromArgb(23, 23, 27),
        Muted: Color.FromArgb(140, 140, 150),
        Hover: Color.FromArgb(46, 46, 53),
        Hint: Color.FromArgb(140, 140, 150),
        Tints:
        [
            Color.FromArgb(54, 46, 32),
            Color.FromArgb(33, 48, 66),
            Color.FromArgb(36, 53, 41),
            Color.FromArgb(57, 37, 39),
            Color.FromArgb(47, 39, 61),
            Color.FromArgb(41, 45, 52),
        ],
        EntryInks:
        [
            Color.FromArgb(236, 229, 217),
            Color.FromArgb(217, 227, 238),
            Color.FromArgb(220, 234, 223),
            Color.FromArgb(238, 220, 220),
            Color.FromArgb(228, 220, 238),
            Color.FromArgb(225, 227, 230),
        ]);

    private readonly WpfRichTextBox _editor;
    private readonly ElementHost _editorElementHost;
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
    private bool _loading;
    private string _colorSignature = string.Empty;
    private string _savedText = string.Empty;

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

        _editor = new WpfRichTextBox
        {
            BorderThickness = new WpfThickness(0),
            Padding = new WpfThickness(0),
            FontFamily = new WpfFontFamily("Microsoft YaHei UI"),
            FontSize = 14,
            AcceptsTab = true,
            VerticalScrollBarVisibility = WpfScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = WpfScrollBarVisibility.Auto,
            Document = CreateDocument(string.Empty),
        };
        _editor.ContextMenu = CreateContextMenu();
        WpfDataObject.AddPastingHandler(_editor, OnPaste);

        _editorElementHost = new ElementHost
        {
            Dock = DockStyle.Fill,
            Child = _editor,
        };

        _hint = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(0),
            Font = new Font("Microsoft YaHei UI", 10.5f),
            Text = "随手记点什么…（支持换行，自动保存）",
            Cursor = Cursors.IBeam,
        };
        _hint.Click += (_, _) =>
        {
            _editorElementHost.Focus();
            _editor.Focus();
        };

        _editor.TextChanged += (_, _) =>
        {
            UpdateHint();

            if (!_loading)
            {
                MarkDirty();
            }

            ApplyEntryColors();
            ScheduleDocumentWidthUpdate();
        };
        _editor.GotKeyboardFocus += (_, _) => _hint.Visible = false;
        _editor.LostKeyboardFocus += (_, _) => UpdateHint();
        _editor.PreviewKeyDown += OnEditorPreviewKeyDown;
        _editor.SizeChanged += (_, _) => ScheduleDocumentWidthUpdate();

        _editorHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 12, 9, 9),
        };
        _editorHost.Controls.Add(_editorElementHost);
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
            ApplyEntryColors();
        };

        ActiveControl = _editorElementHost;
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
        _editorElementHost.BackColor = _palette.Paper;
        _editor.Background = ToBrush(_palette.Paper);
        _editor.Foreground = ToBrush(_palette.Ink);
        _editor.CaretBrush = ToBrush(_palette.Ink);
        _editor.Document.Foreground = ToBrush(_palette.Ink);
        ApplyScrollBarStyle();

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
            ApplyEntryColors();
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
            _editorElementHost.Focus();
            _editor.Focus();
            WpfKeyboard.Focus(_editor);
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
        if (GetEditorText() == _savedText)
        {
            _dirty = false;
            return;
        }

        _dirty = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void LoadNote()
    {
        _loading = true;
        try
        {
            if (File.Exists(_notePath))
            {
                SetEditorText(File.ReadAllText(_notePath));
                _dirty = false;
            }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            _status.ForeColor = Color.FromArgb(196, 92, 78);
            _status.Text = "读取便签失败：" + ex.Message;
        }
        finally
        {
            _loading = false;
        }

        _savedText = GetEditorText();
        UpdateHint();
        ApplyEntryColors();
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

            var text = GetEditorText();
            File.WriteAllText(_notePath, NormalizeNewLines(text));
            _savedText = text;
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

    private void ApplyEntryColors()
    {
        if (_editor.Document is null)
        {
            return;
        }

        var paragraphs = _editor.Document.Blocks.OfType<WpfParagraph>().ToList();
        var signatureBuilder = new StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            signatureBuilder.Append(GetParagraphText(paragraph)).Append('\u001F');
        }

        signatureBuilder.Append(_dark ? 'd' : 'l');
        var signature = signatureBuilder.ToString();
        if (signature == _colorSignature)
        {
            return;
        }

        _colorSignature = signature;

        var entryIndex = 0;
        var groupIndex = -1;
        var previousWasUrl = false;

        foreach (var paragraph in paragraphs)
        {
            var line = GetParagraphText(paragraph).Trim();
            if (line.Length == 0)
            {
                paragraph.Background = null;
                paragraph.Margin = new WpfThickness(0);
                groupIndex = -1;
                previousWasUrl = false;
                continue;
            }

            var isUrl = StartsWithLink(line);
            var isList = StartsWithListMarker(line);
            var startsEntry = groupIndex < 0;
            if (groupIndex < 0)
            {
                groupIndex = entryIndex++;
            }
            else if (!isList && (!isUrl || previousWasUrl))
            {
                groupIndex = entryIndex++;
                startsEntry = true;
            }

            var variant = groupIndex % _palette.Tints.Length;
            paragraph.Background = ToBrush(_palette.Tints[variant]);
            paragraph.Foreground = ToBrush(_palette.EntryInks[variant]);
            paragraph.FontSize = EntrySizes[variant];
            paragraph.Margin = new WpfThickness(0, startsEntry ? 12 : 0, 0, 0);
            previousWasUrl = isUrl;
        }

        UpdateDocumentWidth();
    }

    private void UpdateDocumentWidth()
    {
        if (_editor.Document is null)
        {
            return;
        }

        var maxWidth = 0.0;
        var typeface = new WpfTypeface(
            _editor.FontFamily,
            _editor.FontStyle,
            _editor.FontWeight,
            _editor.FontStretch);
        var pixelsPerDip = WpfVisualTreeHelper.GetDpi(_editor).PixelsPerDip;

        foreach (var paragraph in _editor.Document.Blocks.OfType<WpfParagraph>())
        {
            var fontSize = paragraph.FontSize;
            if (double.IsNaN(fontSize) || fontSize <= 0)
            {
                fontSize = _editor.FontSize;
            }

            foreach (var line in GetParagraphText(paragraph).Split('\n'))
            {
                if (line.Length == 0)
                {
                    continue;
                }

                var formatted = new WpfFormattedText(
                    line,
                    CultureInfo.CurrentUICulture,
                    WpfFlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    WpfBrushes.Black,
                    pixelsPerDip);
                maxWidth = Math.Max(maxWidth, formatted.WidthIncludingTrailingWhitespace);
            }
        }

        var width = Math.Max(GetEditorViewportWidth(), maxWidth + 28);
        if (double.IsNaN(_editor.Document.PageWidth)
            || Math.Abs(_editor.Document.PageWidth - width) > 2)
        {
            _editor.Document.PageWidth = width;
        }
    }

    private void ScheduleDocumentWidthUpdate() =>
        _editor.Dispatcher.BeginInvoke(
            new Action(UpdateDocumentWidth),
            System.Windows.Threading.DispatcherPriority.Background);

    private double GetEditorViewportWidth()
    {
        if (_editor.Template?.FindName("PART_ContentHost", _editor) is WpfScrollViewer viewer
            && viewer.ViewportWidth > 0)
        {
            return viewer.ViewportWidth;
        }

        return Math.Max(_editor.ActualWidth, 240);
    }

    private WpfDocument CreateDocument(string text)
    {
        var document = new WpfDocument
        {
            PagePadding = new WpfThickness(0),
            FontFamily = new WpfFontFamily("Microsoft YaHei UI"),
            FontSize = 14,
            Foreground = ToBrush(_palette.Ink),
        };

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        foreach (var line in normalized.Split('\n'))
        {
            document.Blocks.Add(new WpfParagraph(new WpfRun(line))
            {
                Margin = new WpfThickness(0),
                Padding = new WpfThickness(0),
            });
        }

        return document;
    }

    private string GetEditorText()
    {
        var text = new WpfTextRange(_editor.Document.ContentStart, _editor.Document.ContentEnd)
            .Text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        return text.EndsWith('\n') ? text[..^1] : text;
    }

    private void SetEditorText(string text)
    {
        _editor.Document = CreateDocument(text);
        _editor.CaretPosition = _editor.Document.ContentStart;
    }

    private static string GetParagraphText(WpfParagraph paragraph) =>
        new WpfTextRange(paragraph.ContentStart, paragraph.ContentEnd)
            .Text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

    private static bool StartsWithLink(string line) =>
        line.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static bool StartsWithListMarker(string line)
    {
        if (line.StartsWith("- ")
            || line.StartsWith("* ")
            || line.StartsWith("• ")
            || line.StartsWith("· "))
        {
            return true;
        }

        var index = 0;
        while (index < line.Length && char.IsDigit(line[index]))
        {
            index++;
        }

        if (index == 0 || index + 1 >= line.Length)
        {
            return false;
        }

        return (line[index] == '.' || line[index] == ')' || line[index] == '、') && line[index + 1] == ' ';
    }

    private void UpdateHint() =>
        _hint.Visible = GetEditorText().Length == 0 && !_editor.IsKeyboardFocusWithin;

    private static void OnPaste(object sender, WpfDataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(WpfDataFormats.UnicodeText))
        {
            e.FormatToApply = WpfDataFormats.UnicodeText;
        }
        else if (e.DataObject.GetDataPresent(WpfDataFormats.Text))
        {
            e.FormatToApply = WpfDataFormats.Text;
        }
        else
        {
            e.CancelCommand();
        }
    }

    private void OnEditorPreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == WpfKey.Escape)
        {
            HideToBack();
            e.Handled = true;
        }
        else if (WpfKeyboard.Modifiers == WpfModifierKeys.Control && e.Key == WpfKey.S)
        {
            SaveNote();
            e.Handled = true;
        }
        else if (WpfKeyboard.Modifiers == WpfModifierKeys.Control && e.Key == WpfKey.T)
        {
            ToggleTheme();
            e.Handled = true;
        }
        else if (WpfKeyboard.Modifiers == WpfModifierKeys.Control
            && (e.Key == WpfKey.B || e.Key == WpfKey.I || e.Key == WpfKey.U))
        {
            e.Handled = true;
        }
    }

    private WpfContextMenu CreateContextMenu()
    {
        var menu = new WpfContextMenu();
        menu.Items.Add(CreateMenuItem("撤销", WpfApplicationCommands.Undo));
        menu.Items.Add(CreateMenuItem("重做", WpfApplicationCommands.Redo));
        menu.Items.Add(new WpfSeparator());
        menu.Items.Add(CreateMenuItem("剪切", WpfApplicationCommands.Cut));
        menu.Items.Add(CreateMenuItem("复制", WpfApplicationCommands.Copy));
        menu.Items.Add(CreateMenuItem("粘贴", WpfApplicationCommands.Paste));
        menu.Items.Add(new WpfSeparator());
        menu.Items.Add(CreateMenuItem("全选", WpfApplicationCommands.SelectAll));
        return menu;
    }

    private WpfMenuItem CreateMenuItem(string header, System.Windows.Input.RoutedUICommand command) =>
        new()
        {
            Header = header,
            Command = command,
            CommandTarget = _editor,
        };

    private static WpfSolidColorBrush ToBrush(Color color)
    {
        var brush = new WpfSolidColorBrush(
            System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }

    private void ApplyScrollBarStyle()
    {
        var track = $"{_palette.Footer.R:X2}{_palette.Footer.G:X2}{_palette.Footer.B:X2}";
        var thumb = (_dark ? "77" : "66")
            + $"{_palette.Muted.R:X2}{_palette.Muted.G:X2}{_palette.Muted.B:X2}";

        var xaml = $@"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       TargetType='ScrollBar'>
  <Style.Triggers>
    <Trigger Property='Orientation' Value='Vertical'>
      <Setter Property='Width' Value='10' />
      <Setter Property='Background' Value='#{track}' />
      <Setter Property='Template'>
        <Setter.Value>
          <ControlTemplate TargetType='ScrollBar'>
            <Grid Background='{{TemplateBinding Background}}'>
              <Track x:Name='PART_Track' IsDirectionReversed='True'>
                <Track.DecreaseRepeatButton>
                  <RepeatButton Command='{{x:Static ScrollBar.PageUpCommand}}' Opacity='0' Focusable='False' />
                </Track.DecreaseRepeatButton>
                <Track.Thumb>
                  <Thumb>
                    <Thumb.Template>
                      <ControlTemplate TargetType='Thumb'>
                        <Border CornerRadius='3' Margin='2' Background='#{thumb}' />
                      </ControlTemplate>
                    </Thumb.Template>
                  </Thumb>
                </Track.Thumb>
                <Track.IncreaseRepeatButton>
                  <RepeatButton Command='{{x:Static ScrollBar.PageDownCommand}}' Opacity='0' Focusable='False' />
                </Track.IncreaseRepeatButton>
              </Track>
            </Grid>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Trigger>
    <Trigger Property='Orientation' Value='Horizontal'>
      <Setter Property='Height' Value='10' />
      <Setter Property='Background' Value='#{track}' />
      <Setter Property='Template'>
        <Setter.Value>
          <ControlTemplate TargetType='ScrollBar'>
            <Grid Background='{{TemplateBinding Background}}'>
              <Track x:Name='PART_Track' Orientation='Horizontal' IsDirectionReversed='False'>
                <Track.DecreaseRepeatButton>
                  <RepeatButton Command='{{x:Static ScrollBar.PageLeftCommand}}' Opacity='0' Focusable='False' />
                </Track.DecreaseRepeatButton>
                <Track.Thumb>
                  <Thumb>
                    <Thumb.Template>
                      <ControlTemplate TargetType='Thumb'>
                        <Border CornerRadius='3' Margin='2' Background='#{thumb}' />
                      </ControlTemplate>
                    </Thumb.Template>
                  </Thumb>
                </Track.Thumb>
                <Track.IncreaseRepeatButton>
                  <RepeatButton Command='{{x:Static ScrollBar.PageRightCommand}}' Opacity='0' Focusable='False' />
                </Track.IncreaseRepeatButton>
              </Track>
            </Grid>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Trigger>
  </Style.Triggers>
</Style>";

        _editor.Resources[typeof(WpfScrollBar)] = (WpfStyle)WpfXamlReader.Parse(xaml);
    }

    private static string NormalizeNewLines(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");

    private sealed record Palette(
        Color Paper,
        Color Ink,
        Color Footer,
        Color Muted,
        Color Hover,
        Color Hint,
        Color[] Tints,
        Color[] EntryInks);

    private sealed class AppSettings
    {
        public string Theme { get; set; } = "light";
    }
}
