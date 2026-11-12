using System.Drawing;
using System.Windows.Forms;

namespace DeskNote;

internal sealed class HotkeyDialog : Form
{
    private readonly TextBox _display;
    private readonly Label _message;
    private readonly bool _dark;

    private uint _modifiers;
    private uint _keyCode;

    public HotkeyDialog(uint modifiers, uint key, bool dark)
    {
        _modifiers = modifiers;
        _keyCode = key;
        _dark = dark;

        var paper = dark ? Color.FromArgb(30, 30, 34) : Color.FromArgb(251, 250, 247);
        var ink = dark ? Color.FromArgb(228, 228, 232) : Color.FromArgb(33, 33, 33);
        var field = dark ? Color.FromArgb(23, 23, 27) : Color.FromArgb(241, 240, 235);
        var muted = dark ? Color.FromArgb(140, 140, 150) : Color.FromArgb(128, 128, 124);
        var hover = dark ? Color.FromArgb(46, 46, 53) : Color.FromArgb(231, 230, 224);

        Text = "更换快捷键";
        ShowInTaskbar = false;
        TopMost = true;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(376, 176);
        BackColor = paper;
        ForeColor = ink;
        Font = new Font("Microsoft YaHei UI", 9f);
        KeyPreview = true;
        ImeMode = ImeMode.Disable;

        var title = new Label
        {
            Text = "按下新的快捷键组合（需包含 Ctrl 或 Alt）",
            Bounds = new Rectangle(16, 14, 344, 20),
            BackColor = paper,
            ForeColor = ink,
        };

        _display = new TextBox
        {
            ReadOnly = true,
            TextAlign = HorizontalAlignment.Center,
            Bounds = new Rectangle(16, 40, 344, 34),
            Font = new Font("Microsoft YaHei UI", 13f),
            BackColor = field,
            ForeColor = ink,
            BorderStyle = BorderStyle.FixedSingle,
            TabStop = false,
            ShortcutsEnabled = false,
            ImeMode = ImeMode.Disable,
            Text = Describe(modifiers, key),
        };

        _message = new Label
        {
            Bounds = new Rectangle(16, 80, 344, 18),
            BackColor = paper,
            ForeColor = muted,
            Text = "Esc 取消；更换后点击「保存」生效。",
        };

        var defaultButton = CreateButton("恢复默认", 16, 112, 92, paper, ink, hover);
        var cancelButton = CreateButton("取消", 216, 112, 64, paper, ink, hover);
        var saveButton = CreateButton("保存", 288, 112, 72, paper, ink, hover);

        defaultButton.Click += (_, _) =>
        {
            _modifiers = NativeMethods.ModAlt;
            _keyCode = NativeMethods.VkF3;
            _display.Text = Describe(_modifiers, _keyCode);
            _message.Text = "已恢复默认 Alt+F3，点击「保存」生效。";
        };

        cancelButton.DialogResult = DialogResult.Cancel;
        CancelButton = cancelButton;

        saveButton.Click += (_, _) =>
        {
            if (!IsValid(_modifiers, _keyCode))
            {
                _message.Text = IsReserved(_modifiers, _keyCode)
                    ? "Alt+F4 是关闭窗口的系统组合，请换一个。"
                    : "请先按下包含 Ctrl 或 Alt 的组合键。";
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };
        AcceptButton = saveButton;

        Controls.AddRange([title, _display, _message, defaultButton, cancelButton, saveButton]);
    }

    public uint Modifiers => _modifiers;

    public uint KeyCode => _keyCode;

    public static string Describe(uint modifiers, uint key)
    {
        var parts = new List<string>();
        if ((modifiers & NativeMethods.ModControl) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((modifiers & NativeMethods.ModAlt) != 0)
        {
            parts.Add("Alt");
        }

        if ((modifiers & NativeMethods.ModShift) != 0)
        {
            parts.Add("Shift");
        }

        if ((modifiers & NativeMethods.ModWin) != 0)
        {
            parts.Add("Win");
        }

        parts.Add(KeyName(key));
        return string.Join("+", parts);
    }

    public static bool IsValid(uint modifiers, uint key)
    {
        const uint allowed = NativeMethods.ModAlt | NativeMethods.ModControl | NativeMethods.ModShift;
        return key != 0
            && (modifiers & (NativeMethods.ModAlt | NativeMethods.ModControl)) != 0
            && (modifiers & ~allowed) == 0
            && !IsModifierKey((Keys)key)
            && !IsReserved(modifiers, key);
    }

    private static bool IsReserved(uint modifiers, uint key) =>
        modifiers == NativeMethods.ModAlt && key == (uint)Keys.F4;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        var enabled = _dark ? 1 : 0;
        if (NativeMethods.DwmSetWindowAttribute(
                Handle,
                NativeMethods.DwmUseImmersiveDarkMode,
                ref enabled,
                sizeof(int)) != 0)
        {
            enabled = _dark ? 1 : 0;
            NativeMethods.DwmSetWindowAttribute(
                Handle,
                NativeMethods.DwmUseImmersiveDarkModeLegacy,
                ref enabled,
                sizeof(int));
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Escape && e.Modifiers == Keys.None)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        if (e.Modifiers == Keys.None && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab))
        {
            return;
        }

        e.SuppressKeyPress = true;
        e.Handled = true;

        if (IsModifierKey(e.KeyCode))
        {
            _message.Text = "请继续按下一个普通按键（如字母、数字或 F1-F12）。";
            return;
        }

        if (e.KeyCode == Keys.F4 && e.Modifiers == Keys.Alt)
        {
            _message.Text = "Alt+F4 是关闭窗口的系统组合，请换一个。";
            return;
        }

        var modifiers = 0u;
        if ((e.Modifiers & Keys.Control) != 0)
        {
            modifiers |= NativeMethods.ModControl;
        }

        if ((e.Modifiers & Keys.Alt) != 0)
        {
            modifiers |= NativeMethods.ModAlt;
        }

        if ((e.Modifiers & Keys.Shift) != 0)
        {
            modifiers |= NativeMethods.ModShift;
        }

        if (modifiers == 0)
        {
            _message.Text = "至少需要 Ctrl、Alt 或 Shift 之一。";
            return;
        }

        if ((modifiers & (NativeMethods.ModAlt | NativeMethods.ModControl)) == 0)
        {
            _message.Text = "请加入 Ctrl 或 Alt（Shift 可作为附加修饰键）。";
            return;
        }

        _modifiers = modifiers;
        _keyCode = (uint)e.KeyCode;
        _display.Text = Describe(_modifiers, _keyCode);
        _message.Text = "点击「保存」应用。";
    }

    private static Button CreateButton(
        string text,
        int x,
        int y,
        int width,
        Color paper,
        Color ink,
        Color hover)
    {
        var button = new Button
        {
            Text = text,
            Bounds = new Rectangle(x, y, width, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = paper,
            ForeColor = ink,
            UseVisualStyleBackColor = false,
            TabStop = true,
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = hover;
        button.FlatAppearance.MouseOverBackColor = hover;
        button.FlatAppearance.MouseDownBackColor = hover;
        return button;
    }

    private static bool IsModifierKey(Keys key) =>
        key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu
            or Keys.LWin or Keys.RWin;

    private static string KeyName(uint key)
    {
        if (key >= (uint)Keys.D0 && key <= (uint)Keys.D9)
        {
            return ((char)('0' + (key - (uint)Keys.D0))).ToString();
        }

        if (key >= (uint)Keys.NumPad0 && key <= (uint)Keys.NumPad9)
        {
            return "Num" + (key - (uint)Keys.NumPad0);
        }

        return ((Keys)key).ToString() switch
        {
            "OemMinus" => "-",
            "OemPlus" => "=",
            "OemComma" => ",",
            "OemPeriod" => ".",
            "OemQuestion" => "/",
            "OemSemicolon" => ";",
            "OemQuotes" => "'",
            "OemOpenBrackets" => "[",
            "OemCloseBrackets" => "]",
            "OemPipe" => "\\",
            "OemTilde" => "`",
            var name => name,
        };
    }
}
