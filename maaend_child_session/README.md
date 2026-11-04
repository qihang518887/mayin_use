# MaaEnd 桌面分身

通过 Windows 原生 Child Session 在独立桌面运行 MaaEnd，主桌面可继续使用。

## 使用

1. 解压 `MaaEnd桌面分身.zip`。
2. 首次运行可先打开 `环境检测.bat`：
   - 若提示服务禁用或策略未开启，双击 `一键开启RDP.bat` 一键修复；
   - 若当前 Windows 账户有登录密码，运行 `一键免密.bat` 保存本机凭据。
3. 将 `MaaEnd.exe` 或其快捷方式拖到 `启动桌面分身.bat`，或双击启动脚本。
4. 通过宿主窗口隐藏到托盘、恢复窗口或注销分身。

凭据由 Windows DPAPI 加密保存在本机 `session_cred.dat`，不要分享该文件。分辨率为 1600×900，宿主按原像素显示，不做缩放。
