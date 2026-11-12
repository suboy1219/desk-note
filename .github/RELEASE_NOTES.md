## 中文

DeskNote v1.1.0：条目分组配色与横向滚动。

- 编辑区域改用 WPF 控件，便签仍以 UTF-8 纯文本保存。
- 相邻条目轮换使用 6 种低饱和底色，亮色 / 暗色各一套，文字深浅和字号随条目轻微变化。
- 名称下面紧跟的链接或列表项会自动分组，空行可强制分开条目。
- 超长行不自动换行，支持横向滚动；滚动条随主题配色。
- 粘贴仅保留纯文本，提供撤销、重做、剪切、复制、粘贴、全选右键菜单。
- 保留全局快捷键、自动保存、主题切换和静默启动；同步更新中英文文档。

**下载：** Windows 10 / 11 x64。推荐 `standalone.zip`（包含 .NET 运行时）；`framework-dependent.zip` 体积更小，需要 .NET 10 Desktop Runtime x64。解压后运行 `DeskNote.exe`，无需安装。应用界面目前为中文。

便签保存在 `%APPDATA%\DeskNote\note.txt`。更新时只需替换程序文件。开机自启请参照 README 创建带 `--startup` 参数的启动快捷方式。

## English

DeskNote v1.1.0: tinted entry groups and horizontal scrolling.

- A WPF editing surface; notes continue to be saved as UTF-8 plain text.
- Six alternating, low-saturation entry backgrounds for each theme, with subtle variations in text tone and size.
- Links and list items directly below a name are grouped automatically; blank lines separate entries.
- Long lines scroll horizontally instead of wrapping, with theme-aware scrollbars.
- Plain-text paste and a context menu for undo, redo, cut, copy, paste, and select all.
- Global shortcuts, automatic saving, theme switching, and hidden startup remain available; Chinese and English documentation has been updated.

**Download:** Windows 10 / 11 x64. Choose `standalone.zip` for the bundled .NET runtime, or `framework-dependent.zip` if .NET 10 Desktop Runtime x64 is already installed. Extract and run `DeskNote.exe`; no installer is required. The app interface is currently in Chinese.

Notes are stored in `%APPDATA%\DeskNote\note.txt`. Replace the program files to update. See the README for an optional startup shortcut with the `--startup` argument.

---

`SHA256SUMS.txt` contains SHA-256 hashes for both ZIP files. / 校验文件包含两个压缩包的 SHA-256 哈希值。
