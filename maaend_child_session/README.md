# MaaEnd 桌面分身

通过 Windows 原生 Child Session 在独立桌面运行 MaaEnd，主桌面可继续使用。

## 使用

1. 从 [Releases](../../releases) 下载 `MaaEnd-ChildSession-v1.0.0.zip` 并解压。
2. 首次运行建议先打开 `环境检测.bat`：
   - 若系统为家庭版（第 1 项报 FAIL），运行 `修复家庭版RDP.bat` 切到专业版。它会要求重启，重启后再运行一次同一脚本，自动调用包内的 `HWID_Activation.cmd` 完成激活；
   - 若服务禁用或策略未开启（第 3、4 项），运行 `一键开启RDP.bat` 一键修复；
   - 若当前 Windows 账户有登录密码，运行 `一键免密.bat` 保存本机凭据。
3. 将 `MaaEnd.exe` 或其快捷方式拖到 `启动桌面分身.bat`，或双击启动脚本。
4. 通过宿主窗口隐藏到托盘、恢复窗口或注销分身。

凭据由 Windows DPAPI 加密保存在本机 `session_cred.dat`，不要分享该文件。分辨率为 1600×900，宿主按原像素显示，不做缩放。

## 目录

| 文件 | 说明 |
| --- | --- |
| `ChildSessionHost.cs` | 宿主源码（单文件，系统自带 `.NET 4.0 csc.exe` 编译）。 |
| `ChildSessionHost.exe` | 编译后的宿主。 |
| `启动桌面分身.bat` | 日常启动入口。 |
| `一键开启RDP.bat` | 解除 TermService 禁用、开启本地监听与 Child Session。 |
| `修复家庭版RDP.bat` | 家庭版转专业版 + 激活。 |
| `一键免密.bat` | 保存本机 DPAPI 凭据。 |
| `环境检测.bat` | 环境自检。 |

