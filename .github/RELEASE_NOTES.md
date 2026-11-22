## 中文

DeskNote v1.3.0：新增系统托盘图标，修复快捷键冲突时的启动崩溃。

- 左键点击托盘图标即可呼出便签；右键菜单支持显示、隐藏和退出。
- 使用 `--startup` 隐藏启动时也会显示托盘图标，快捷键不可用时仍可访问便签。
- 修复快捷键被占用时因窗口句柄重建导致的启动崩溃。
- 隐藏和退出时保存便签，退出时清理托盘图标及相关资源。
- 更新中英文使用说明，已有便签、主题和快捷键设置无需迁移。

**下载：** Windows 10 / 11 x64。推荐 `standalone.zip`（包含 .NET 运行时）；`framework-dependent.zip` 体积更小，需要 .NET 10 Desktop Runtime x64。解压后运行 `DeskNote.exe`，无需安装。应用界面目前为中文。

便签保存在 `%APPDATA%\DeskNote\note.txt`。更新前请先退出旧版本，再替换程序文件。

## English

DeskNote v1.3.0: system tray access and a startup crash fix for shortcut conflicts.

- Left-click the tray icon to bring the note to the front; right-click to show, hide, or exit.
- The tray icon is available when starting hidden with `--startup`, including when the global shortcut is unavailable.
- Fixed a startup crash caused by window handle recreation when another app has claimed the shortcut.
- Notes are saved when hiding or exiting; the tray icon and related resources are cleaned up on exit.
- Updated the Chinese and English documentation. Existing notes, themes, and shortcut preferences require no migration.

**Download:** Windows 10 / 11 x64. Choose `standalone.zip` for the bundled .NET runtime, or `framework-dependent.zip` if .NET 10 Desktop Runtime x64 is already installed. Extract and run `DeskNote.exe`; no installer is required. The app interface is currently in Chinese.

Notes are stored in `%APPDATA%\DeskNote\note.txt`. Exit the old version before replacing the program files.

---

`SHA256SUMS.txt` contains SHA-256 hashes for both ZIP files. / 校验文件包含两个压缩包的 SHA-256 哈希值。
