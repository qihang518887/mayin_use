# AstrBot + Playwright 镜像

基于官方 AstrBot 镜像安装 Playwright 和 Chromium，供需要浏览器自动化的插件使用。

GitHub Actions 定时检查上游 AstrBot 镜像；检测到更新后，构建并推送包含 `linux/amd64` 与 `linux/arm64` 的镜像 `mayin/astrbot_playwright`。

Dockerfile 位于本目录，自动构建流程见 [工作流](../.github/workflows/astrbot_playwright.yml)。
