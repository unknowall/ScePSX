<h2>ScePSX - A Lightweight PS1 emulator Fully Developed in C#</h2>

![Platform](https://img.shields.io/badge/platform-Windows%20|%20Linux%20|%20macOS%20|%20Android-blue) ![GitHub Release](https://img.shields.io/github/v/release/unknowall/ScePSX?label=Release) ![Language](https://img.shields.io/github/languages/top/unknowall/ScePSX) ![Build Status](https://img.shields.io/github/actions/workflow/status/unknowall/ScePSX/build-core.yml?branch=master&label=Build%20Core&logo=github)
 ![downloads](https://img.shields.io/github/downloads/unknowall/ScePSX/total.svg) [![Gitee Repo](https://img.shields.io/badge/Gitee-Mirror-FFB71B)](https://gitee.com/unknowall/ScePSX)

<details>
<summary>🌐 点击查看中文说明 / Chinese README</summary>

#### ScePSX 是一个完全使用 C# 编写的 PlayStation 模拟器，注重跨平台支持和轻量级设计。

> 通过Wiki深入了解：https://zread.ai/unknowall/ScePSX

## ✨ 主要功能

| 功能 | 说明 |
|------|------|
| **跨平台支持** | 支持 Windows、Linux、macOS、Android 系统 |
| **即时存档/读档** | 支持随时保存和加载游戏进度 |
| **PGXP 几何精度增强** | 软件及硬件后端均支持，调整即时生效，无需重启 |
| **多渲染后端** | 支持 D2D、D3D、OpenGL、Vulkan 渲染器动态切换 |
| **ReShade 集成** | D3D、OpenGL、Vulkan 后端支持 ReShade 后处理效果 |
| **分辨率调节** | 硬件后端支持 4K 输出，软件后端支持 xBR、JINC 等插值算法 |
| **内存工具** | 提供内存编辑和搜索功能 |
| **金手指支持** | 支持作弊码功能 |
| **网络对战** | 支持联机对战功能 |
| **存档管理** | 支持多存档管理 |

## 📊 性能表现 (WinUI版)

| 渲染后端 | 内存占用 | 硬件建议 | 渲染模式 |
|---------|---------|---------|---------|
| D2D | ~32MB | 较低配置 | software |
| D3D | ~52MB | 较低配置 | software |
| OpenGL | ~86MB / ~138MB | 主流配置 | software / OpenGL |
| Vulkan | ~120MB / ~143MB | 较高配置 | software / Vulkan |

> PGXP 在软件、OpenGL、Vulkan 后端生效

### 最低配置参考
- Intel 赛扬 3215U 平台可稳定 60 FPS
- 测试条件：不使用 GameDB、ReShade、PGXP

### 渲染后端要求
- **OpenGL**: 需支持 3.3 及以上版本
- **Vulkan**: 需支持 1.1 及以上版本

## 🛠️ 使用说明

### 1. BIOS 设置
> **注意**: 模拟器不包含 BIOS 文件

```
ScePSX/
├── bios/
│   └── SCPH1001.bin  (放入 BIOS 文件)
├── saves/            (存档目录)
└── ScePSX.exe
```

### 2. ReShade 使用
- 支持 OpenGL、Vulkan 后端（D3D 需手动安装 ReShade）
- 游戏中按 **Home** 键打开 ReShade 设置界面
- 内置多款预设 Shader

### 3. 多光盘游戏
- **记忆卡1**: 各光盘独立存档
- **记忆卡2**: 所有光盘共用存档（推荐用于多碟游戏）

### 4. 控制设置
- **键盘**: 文件菜单中自定义按键
- **手柄**: 即插即用，无需额外设置

## ❓ 常见问题

### Q: 游戏无法启动？
A: 请检查：
1. BIOS 文件是否正确放置
2. 游戏镜像格式是否支持（.bin/.cue、.img/.cue、.iso、.chd）

### Q: 如何添加更多 ReShade 滤镜？
A: 从 [ReShade官网](https://reshade.me/) 下载 Shader 文件，放入 reshade 目录：
```
ScePSX/
├── reshade/          (滤镜存放目录)
└── ScePSX.exe
```

### Q: 游戏兼容性如何？
A: 绝大部分主流 PS1 游戏均可运行。

### Q: CPU 占用过高？
A: 建议使用 D2D 渲染器或降低内部分辨率。

### Q: 如何提升画质？
A: 按 F11 切换分辨率，按 Home 键配置 ReShade 滤镜。

### Q: 音频不同步？
A: 尝试调整音频缓冲区大小或更换输出设备。

### Q: 支持哪些区域版本？
A: 支持 NTSC-J、NTSC-U、PAL 格式。

### Q: 手柄不支持震动？
A: 支持震动的游戏需按 F10 切换到模拟手柄模式。

## 🌍 跨平台支持

| 平台 | 架构 | 备注 |
|------|------|------|
| **Android** | x64 / arm64-v8a / armeabi-v7a | 需 Android 5.0+，推荐 9.0+ |
| **Windows** | x86 / x64 / arm | Avalonia 版仅 x64，免 .NET 运行时 |
| **Linux** | x64 / arm / arm64 / riscv64 / loongarch64 | 树莓派 Zero 选 arm 版本 |
| **macOS** | x64 / arm64 | Vulkan 需 MoltenVK（不推荐） |

> 如需其他平台支持，可参考 AvaloniaUI 目录下脚本自行编译

## 🔧 编译说明

- **框架要求**: .NET 8.0
- **核心依赖**: 修改版 MessagePack（位于 `ScePSX/Utils/MessagePack`），请勿通过 NuGet 安装
- **UI 框架**: Avalonia UI 11.3.11（AvaloniaUI/Android）
- **Android 环境**: .NET MAUI + Android SDK 33
- **SDL 库**: 预编译文件位于 `SDLLib`（Android 版不使用）
- **.NET 版本**: 使用低于 8.0 的框架需手动修改项目文件
- **参考项目**: Core的部分代码基于 [ProjectPSX](https://github.com/BluestormDNA/ProjectPSX) 重构

## 🤝 贡献指南

欢迎通过以下方式参与项目：
- **提交 Issue**: [报告问题或提出建议](https://github.com/unknowall/ScePSX/issues)
- **提交 PR**: Fork 项目后提交 Pull Request

## 📥 下载

| 版本 | 大小 | 说明 |
|------|------|------|
| **WinUI轻量版** | 1.05 MB | 除 ReShade 外所有功能 |
| **WinUI完整版** | 5.63 MB | 包含 ReShade 等功能 |
| **AvaloniaUI版** | 12~30 MB | 跨平台 UI 版本 |
| **Android版** | 21 MB | 包含 arm64-v8a/x86_64 |
| **GameDB** | - | 可选，自动识别游戏配置 |
| **ControllerDB** | - | 可选，支持更多手柄 |

[⬇️ 下载最新版本](https://github.com/unknowall/ScePSX/releases)

## ⚖️ 法律声明
ScePSX 为MIT开源项目，请确保您拥有合法的游戏 ROM 和 BIOS 文件。

</details>
  
#### ScePSX is a lightweight, cross-platform PlayStation 1 emulator written **entirely in C#**.
> Dive deeper via the Wiki: https://deepwiki.com/unknowall/ScePSX

## ✨ Key Features

| Feature | Description |
|---------|-------------|
| **Cross-platform** | Supports Windows, Linux, macOS, and Android |
| **Save States** | Save and load game progress anytime |
| **PGXP Geometry Enhancement** | Supported on both software and hardware backends, adjustments take effect immediately without restart |
| **Multiple Render Backends** | Dynamic switching between D2D, D3D, OpenGL, and Vulkan renderers |
| **ReShade Integration** | ReShade post-processing effects supported on D3D, OpenGL, and Vulkan backends |
| **Resolution Scaling** | Up to 4K output on hardware backends; xBR, JINC upscaling algorithms on software backend |
| **Memory Tools** | Memory editing and searching functionality |
| **Cheat Support** | Cheat code functionality |
| **Netplay** | Online multiplayer support |
| **Save Management** | Multiple save file management |

## 📊 Performance (WinUI Version)

| Render Backend | Memory Usage | Hardware Target | Render Mode |
|----------------|--------------|-----------------|-------------|
| D2D | ~32MB | Low-end | software |
| D3D | ~52MB | Low-end | software |
| OpenGL | ~86MB / ~138MB | Mainstream | software / OpenGL |
| Vulkan | ~120MB / ~143MB | High-end | software / Vulkan |

> PGXP is available on software, OpenGL, and Vulkan backends

### Minimum Requirements Reference
- Stable 60 FPS on Intel Celeron 3215U
- Test conditions: No GameDB, ReShade, or PGXP

### Render Backend Requirements
- **OpenGL**: Version 3.3 or higher
- **Vulkan**: Version 1.1 or higher

## 🛠️ Usage Guide

### 1. BIOS Setup
> **Note**: Emulator does not include BIOS files

```
ScePSX/
├── bios/
│   └── SCPH1001.bin  (Place your BIOS file here)
├── saves/            (Save directory)
└── ScePSX.exe
```

### 2. ReShade Usage
- Supported on OpenGL and Vulkan backends (D3D requires manual ReShade installation)
- Press **Home** key in-game to open ReShade configuration interface
- Multiple preset shaders included

### 3. Multi-Disc Games
- **Memory Card 1**: Independent saves per disc
- **Memory Card 2**: Shared saves across all discs (recommended for multi-disc games)

### 4. Controls
- **Keyboard**: Customize keybindings in File menu
- **Controller**: Plug and play, no additional setup required

## ❓ Frequently Asked Questions

### Q: Game won't start?
A: Please check:
1. BIOS file is correctly placed
2. Game image format is supported (.bin/.cue, .img/.cue, .iso, .chd)

### Q: How to add more ReShade shaders?
A: Download shader files from [ReShade website](https://reshade.me/) and place them in the reshade directory:
```
ScePSX/
├── reshade/          (Shader directory)
└── ScePSX.exe
```

### Q: How is game compatibility?
A: The vast majority of mainstream PS1 games are playable.

### Q: High CPU usage?
A: Try using the D2D renderer or lowering internal resolution.

### Q: How to improve graphics quality?
A: Press F11 to cycle resolutions, press Home to configure ReShade filters.

### Q: Audio desync?
A: Try adjusting audio buffer size or changing output device.

### Q: Which region versions are supported?
A: Supports NTSC-J, NTSC-U, and PAL formats.

### Q: Controller vibration not working?
A: For games with vibration support, press F10 to switch to emulated controller mode.

## 🌍 Cross-Platform Support

| Platform | Architectures | Notes |
|----------|---------------|-------|
| **Android** | x64 / arm64-v8a / armeabi-v7a | Requires Android 5.0+, recommended 9.0+ |
| **Windows** | x86 / x64 / arm | Avalonia version x64 only, no .NET runtime required |
| **Linux** | x64 / arm / arm64 / riscv64 / loongarch64 | Raspberry Pi Zero select arm version |
| **macOS** | x64 / arm64 | Vulkan requires MoltenVK (not recommended) |

> For additional platform support, refer to scripts in the AvaloniaUI directory for self-compilation

## 🔧 Build Instructions

- **Framework**: .NET 8.0
- **Core Dependency**: Modified MessagePack (located at `ScePSX/Utils/MessagePack`), do **not** install via NuGet
- **UI Framework**: Avalonia UI 11.3.11 (AvaloniaUI/Android)
- **Android Environment**: .NET MAUI + Android SDK 33
- **SDL Library**: Precompiled binaries in `SDLLib` (not used by Android version)
- **.NET Version**: Using frameworks below 8.0 requires manual project file modification
- **Reference Project**: Core code partially refactored from [ProjectPSX](https://github.com/BluestormDNA/ProjectPSX)

## 🤝 Contributing

Contributions are welcome through the following channels:
- **Submit an Issue**: [Report bugs or suggest features](https://github.com/unknowall/ScePSX/issues)
- **Submit a PR**: Fork the project and submit a Pull Request

## 📥 Downloads

| Version | Size | Description |
|---------|------|-------------|
| **WinUI Lite** | 1.05 MB | All features except ReShade |
| **WinUI Full** | 5.63 MB | Includes ReShade and all features |
| **AvaloniaUI** | 12~30 MB | Cross-platform UI version |
| **Android** | 21 MB | Includes arm64-v8a/x86_64 |
| **GameDB** | - | Optional, auto game configuration |
| **ControllerDB** | - | Optional, additional controller support |

[⬇️ Download Latest Release](https://github.com/unknowall/ScePSX/releases)

## ⚖️ Legal Disclaimer
ScePSX is an open-source project, **for educational and research purposes only**.  
Please ensure you own legitimate copies of game ROMs and BIOS files.
