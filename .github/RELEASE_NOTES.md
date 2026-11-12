## 中文

DeskNote 首个正式版本：一个支持自动保存、全局快捷键和亮暗主题的 Windows 桌面便签。

- `Alt+F3` 全局呼出 / 收起，失焦后自动靠后。
- 多行纯文本、约 0.6 秒防抖自动保存。
- `Ctrl+T` 切换亮色 / 暗色，记住主题偏好。
- 可拉伸窗口、单实例、支持 `--startup` 静默启动。
- 中英文使用文档及亮暗界面示意图。

**下载：** Windows 10 / 11 x64。推荐 `standalone.zip`（包含 .NET 运行时）；`framework-dependent.zip` 体积更小，需要 .NET 10 Desktop Runtime x64。解压后运行 `DeskNote.exe`，无需安装。应用界面目前为中文。

便签保存在 `%APPDATA%\DeskNote\note.txt`。更新时只需替换程序文件。开机自启请参照 README 创建带 `--startup` 参数的启动快捷方式。

## English

The first stable release of DeskNote: a small Windows desktop notepad with automatic saving, a global shortcut, and light/dark themes.

- Global `Alt+F3` shortcut to show or hide; moves behind other windows on focus loss.
- Multiline plain text, saved automatically after about 0.6 seconds of inactivity.
- `Ctrl+T` theme switching with a remembered preference.
- Resizable window, single-instance operation, and hidden launch with `--startup`.
- Chinese and English documentation with light/dark UI illustrations.

**Download:** Windows 10 / 11 x64. Choose `standalone.zip` for the bundled .NET runtime, or `framework-dependent.zip` if .NET 10 Desktop Runtime x64 is already installed. Extract and run `DeskNote.exe`; no installer is required. The app interface is currently in Chinese.

Notes are stored in `%APPDATA%\DeskNote\note.txt`. Replace the program files to update. See the README for an optional startup shortcut with the `--startup` argument.

---

`SHA256SUMS.txt` contains SHA-256 hashes for both ZIP files. / 校验文件包含两个压缩包的 SHA-256 哈希值。
