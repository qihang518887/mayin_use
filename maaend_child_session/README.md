# MaaEnd 桌面分身 (Windows Child Session 独立外挂)

基于 Windows 原生 **Child Session** 机制实现的零侵入桌面分身挂机外挂工具。

---

## 一、为什么做这个工具？

在 Windows 上使用 MaaEnd 等图像识别自动化工具挂机游戏时，通常会遇到以下痛点：
1. **抢占主屏幕与鼠标**：Win32 前台模式执行（SendInput 与前台截图）会不断抢夺鼠标焦点，导致挂机时无法正常打字、办公或打其他游戏；
2. **后台模式不稳定/不支持**：部分游戏（如《明日方舟：终末地》）对驱动级后台点击、Hook 注入、窗口句柄模式支持不佳或容易失效；
3. **改源码维护成本极高**：侵入式魔改 MaaEnd/MXU 会破坏官方版本更新机制，无法直接享受官方最新发布的功能与资源包更新；
4. **多用户 RDP 繁琐且侵入系统**：传统 RDP Wrapper 或修补 	ermsrv.dll 在 Windows 频繁更新时容易失效或被杀软报毒。

**本方案定位：零侵入的独立桌面分身外挂器**
- **与原版完全解耦**：MaaEnd 使用 100% 官方原版 Release，直接升级覆盖即可；
- **原生虚拟子会话**：在 Windows 原生 Child Session 中运行游戏与 MaaEnd，享有独立虚拟桌面与独占前台键鼠，主桌面完全不被占用；
- **随时最小化托盘**：宿主窗口可随时收起至系统托盘，实现无感伪后台挂机。

---

## 二、核心特性与技术原理

1. **Windows 原生 Child Session 虚拟桌面隔离**
   - 调用系统原生 API WTSEnableChildSessions(TRUE)，在当前控制台用户下生成隔离子会话。
   - 子会话拥有独立的 WinStation、虚拟显示器、桌面与输入队列，游戏与 MaaEnd 在子会话中直接以原版 Win32-Front（电脑端）控制器全速运行，无需任何特殊后台注入。
2. **免 RDP Wrapper / 免系统补丁**
   - 微软将 Child Session 视为本地嵌套会话，不受单机多用户并发限制。
   - 宿主通过 Windows 自带的 RDP ActiveX 控件（mstscax.dll）以本地回环连接（ConnectToChildSession），不修改任何系统核心 DLL。
3. **1:1 绝对物理像素对齐（至关重要）**
   - 显式禁用 RDP 控件的 SmartSizing，锁定 1600x900 1:1 物理像素映射，杜绝缩放插值导致的画面模糊与 OpenCV 模板识别失败。
4. **极简纯 BAT 交互与免密登录**
   - 交付脚本全部纯 BAT 化（杜绝 PowerShell 执行策略限制），纯 ASCII 换行避免 cmd 语法错位；
   - 支持将 MaaEnd.exe 或桌面快捷方式（.lnk）直接拖拽到脚本或窗口上运行，自动还原真实路径；
   - 凭据经 Windows 原生 DPAPI 加密存入本地，后续 0 弹窗秒进分身。
5. **Windows 10/11 深度兼容**
   - 内嵌 pp.manifest 声明 Win10/Win11 兼容性 GUID，声明 TargetFramework 4.8，避免 Win11 24H2/Canary 的 TLS 1.0/1.1 弃用与兼容性助手（PCA）拦截。

---

## 三、文件说明

| 文件 | 说明 |
| :--- | :--- |
| 启动桌面分身.bat | **日常启动入口**：双击秒连分身桌面；亦支持将 MaaEnd.exe 或快捷方式拖拽至该脚本上方启动 |
| 一键免密.bat | **免密配置工具**：双击输入 Windows 密码并加密存入 DPAPI，后续启动彻底免密 |
| 环境检测.bat | **6 维自检工具**：快速检测系统版本、TermService、策略、API 与凭据是否就绪 |
| ChildSessionHost.exe | 核心宿主执行体（~46KB 单文件，无外部依赖） |
| ChildSessionHost.cs | 核心宿主 C# 源码 |
| pp.manifest | Windows 10/11 原生兼容性清单（编译时内嵌） |
| MaaEnd桌面分身.zip | 预打包开箱即用成品包（包含 Exe 与 3 个 bat） |

---

## 四、使用说明

### 1. 首次使用前检测与免密
1. 双击运行 **环境检测.bat**，确认各项指标均显示 OK；
2. 双击运行 **一键免密.bat**，在弹出的窗口中输入当前 Windows 登录密码并勾选记住密码（密码仅由 DPAPI 加密存入本地，绝不明文存储或上传）。

### 2. 日常启动与挂机
- **方式 A（拖拽启动，推荐）**：
  直接将你的 MaaEnd.exe 或其**桌面快捷方式**拖拽到 启动桌面分身.bat 图标上松开。
- **方式 B（同目录双击）**：
  若将本工具解压至 MaaEnd 所在目录，直接双击 启动桌面分身.bat 即可自动拉起同目录的 MaaEnd。

### 3. 隐藏与退出
- **隐藏至后台**：点击宿主窗口顶部的【隐藏至托盘】或窗口右上角的【X】按钮，窗口将自动收起至右下角系统托盘，子会话与游戏/MaaEnd 继续在后台挂机；
- **唤醒窗口**：双击右下角系统托盘图标即可随时重新呼出分身桌面；
- **完全注销并退出**：在托盘图标上右键选择【注销分身并退出】，或点击窗口顶部的【注销分身】按钮。

---

## 五、从源码编译

本宿主为单个 C# 文件，零外部 NuGet 依赖，使用 Windows 系统自带的 .NET Framework 编译器即可编译：

`powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /r:System.Security.dll /codepage:65001 /win32manifest:app.manifest /target:winexe /out:ChildSessionHost.exe ChildSessionHost.cs
`
