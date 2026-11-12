## 中文

DeskNote v1.2.0：自定义全局快捷键。

- 右下角新增「快捷键」按钮，可直接录入组合键并保存，也可恢复默认 `Alt+F3`。
- 组合键需包含 `Ctrl` 或 `Alt`，可搭配 `Shift`；拦截单独修饰键和 `Alt+F4`。
- 快捷键偏好与主题一起保存到 `settings.json`，兼容旧版仅含主题的设置。
- 新组合键注册失败时尝试恢复原快捷键，无法注册时保留任务栏入口。
- 状态栏、提示和窗口标题随当前快捷键更新；对话框支持亮暗主题。
- 保留条目分组配色、横向滚动和纯文本自动保存，同步更新中英文文档。

**下载：** Windows 10 / 11 x64。推荐 `standalone.zip`（包含 .NET 运行时）；`framework-dependent.zip` 体积更小，需要 .NET 10 Desktop Runtime x64。解压后运行 `DeskNote.exe`，无需安装。应用界面目前为中文。

便签保存在 `%APPDATA%\DeskNote\note.txt`。更新时只需替换程序文件。开机自启请参照 README 创建带 `--startup` 参数的启动快捷方式。

## English

DeskNote v1.2.0: customizable global shortcuts.

- A new **快捷键** button at the bottom right lets you capture and save a shortcut or restore the default `Alt+F3`.
- Combinations must include `Ctrl` or `Alt`, optionally with `Shift`; modifier-only keys and `Alt+F4` are rejected.
- The shortcut is saved alongside the theme in `settings.json`, with compatibility for older theme-only settings.
- If registration fails, DeskNote attempts to restore the previous shortcut; a taskbar entry remains available if registration is unavailable.
- Status text, tooltips, and the window title follow the current shortcut; the dialog supports light and dark themes.
- Tinted entry groups, horizontal scrolling, and plain-text automatic saving remain available; Chinese and English documentation has been updated.

**Download:** Windows 10 / 11 x64. Choose `standalone.zip` for the bundled .NET runtime, or `framework-dependent.zip` if .NET 10 Desktop Runtime x64 is already installed. Extract and run `DeskNote.exe`; no installer is required. The app interface is currently in Chinese.

Notes are stored in `%APPDATA%\DeskNote\note.txt`. Replace the program files to update. See the README for an optional startup shortcut with the `--startup` argument.

---

`SHA256SUMS.txt` contains SHA-256 hashes for both ZIP files. / 校验文件包含两个压缩包的 SHA-256 哈希值。
