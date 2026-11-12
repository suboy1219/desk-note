# 便签（DeskNote）

Windows 桌面极简便签：一个能换行的文本框，窗口可自由拉伸，`Alt+F3` 呼出并置顶，点到别的软件就沉到最底层。

## 功能

- **一个文本框**：支持换行、Tab、滚动，输入即自动保存（约 0.6 秒防抖）。
- **可自由拉伸**：任意拖拽窗口边缘改变大小，最小 300×220。
- **`Alt+F3` 全局快捷键**：任何程序里按一下，便签立刻弹到最前面并获得焦点；再按一次收起。
- **置顶 / 靠后**：呼出时窗口置顶（TopMost）；点击其它软件后自动取消置顶并沉到窗口最底层。
- **亮色 / 暗色双主题**：底部右侧按钮或 `Ctrl+T` 切换，标题栏同步变色，选择会记住。
- **不占任务栏、不进 Alt+Tab**：窗口是工具窗口，任务栏里没有按钮，只能靠 `Alt+F3` 呼出。
- **随电脑开机自启**：登录后静默启动（不弹窗），按 `Alt+F3` 才出现。
- **单实例**：重复启动不会开第二个窗口。

## 使用

桌面双击 `便签` 图标（快捷方式指向 `dist\DeskNote.exe`）。

- `Alt+F3`：呼出 / 收起（全局，任何界面都有效）
- `Esc`：收起（沉到底层）
- `Ctrl+S`：立即保存（平时不用按，自动保存）
- `Ctrl+T`：切换亮色 / 暗色主题（也可点右下角「暗色 / 亮色」按钮）
- 右上角 `X` / `Alt+F4`：退出程序

> 因为不在任务栏显示，收起后请用 `Alt+F3` 唤回。若 `Alt+F3` 被别的软件占用导致注册失败，程序会自动退回任务栏显示并在状态栏提示，避免无法唤出。

## 开机自启

启动项是一个快捷方式，位置：

```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\便签.lnk
```

参数带 `--startup`，表示启动后先隐藏，等你按 `Alt+F3`。想取消自启，删掉这个快捷方式即可（也可以在「设置 → 应用 → 启动」里关掉）。手动双击桌面图标启动时不会隐藏，窗口会直接出现。

## 数据保存位置

```
%APPDATA%\DeskNote\note.txt
```

纯文本 UTF-8，可以直接用记事本打开、备份或替换。主题偏好保存在同目录 `settings.json`（`{"Theme":"dark"}`），删掉即可恢复默认亮色。

## 构建

```powershell
cd D:\github\desk-note
dotnet publish DeskNote.csproj -c Release -r win-x64 -p:SelfContained=false -p:PublishSingleFile=true -o dist
```

输出 `dist\DeskNote.exe`（约 240 KB，依赖已安装的 .NET 10 桌面运行时）。

如需完全免运行时的绿色文件，把 `-p:SelfContained=false` 换成 `-p:SelfContained=true`，体积约 100 MB。

## 文件说明

| 文件 | 作用 |
| --- | --- |
| `Program.cs` | 入口，单实例互斥 |
| `NoteForm.cs` | 窗口、文本框、自动保存、快捷键与置顶逻辑 |
| `Interop.cs` | Win32 互操作（RegisterHotKey / SetWindowPos 等） |
| `app.ico` | 图标（多尺寸，exe 与窗口/任务栏共用） |
| `dist\DeskNote.exe` | 发布产物 |

## 已知限制

- `Alt+F3` 是全局热键，若被其它软件（如显卡驱动面板、录屏工具）占用，启动后底部状态栏会提示注册失败；关闭冲突软件后重启本程序即可。
- 内容以纯文本保存，不支持图片、富文本、多个便签页。
