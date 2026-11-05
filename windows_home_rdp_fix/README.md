# Windows 家庭版 RDP 修复

家庭版（`Core*`）没有远程桌面主机功能，Child Session 方案在家庭版上不可用。本工具把系统切到专业版并完成激活，补齐 RDP 主机。

版本切换走 Microsoft-Activation-Scripts 客户端的同一条路：用 SPP 的 `SoftwareLicensingService.InstallProductKey` 装入专业版通用密钥，不是 `dism /Set-Edition`。密钥运行时由系统 `pkeyhelper.dll`（`SkuGetProductKeyForEdition`）现取，脚本里没有密钥字面量；取不到时才退回 `key.txt` 或手动输入。

激活直接调用 MAS 3.12 原版 `HWID_Activation.cmd /HWID`。本仓库不收录该脚本，它只放在 Release 的 `MaaEnd桌面分身.zip` 里，用时解压到本脚本同一目录。

## 用法

1. 双击 `修复家庭版RDP.bat`，会请求管理员权限。脚本取密钥并装入专业版密钥。
2. 重启电脑。
3. 再运行一次同一个脚本。此时已是专业版，脚本会调用同目录的 `HWID_Activation.cmd /HWID` 完成激活（需要联网）。
4. `修复家庭版RDP.bat /check` 只读当前版本，不改系统、不提权。

## 边界

- 两步不能合并：第一步只切版本，第二步才激活，中间必须先重启。
- 家庭版以外的版本直接跳过切版本，只做激活。
- 版本切换不可逆（除重装），执行前先存盘。
- 等价的手动操作：`设置 > 系统 > 激活 > 更改产品密钥`。
