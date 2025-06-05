<h2>ScePSX - A Lightweight PS1 emulator Fully Developed in C#</h2>

![License](https://img.shields.io/badge/license-MIT-blue) ![GitHub Release](https://img.shields.io/github/v/release/unknowall/ScePSX?label=Release) ![Language](https://img.shields.io/github/languages/top/unknowall/ScePSX) ![Build Status](https://img.shields.io/badge/build-passing-brightgreen) ![downloads](https://img.shields.io/github/downloads/unknowall/ScePSX/total.svg) [![Gitee Repo](https://img.shields.io/badge/Gitee-Mirror-FFB71B)](https://gitee.com/unknowall/ScePSX)
<details>
<summary><h3> 🌐 English Version</h3></summary>

## Key Features 🎮
- **Save States**: Save and load game progress at any time.
- **Multi-Renderer Support**: Dynamically switch between D2D, D3D, OpenGL, and Vulkan renderers to adapt to different hardware configurations.
- **ReShade Integration**: ReShade post-processing effects supported on D3D, OpenGL, and Vulkan for enhanced graphics.
- **Resolution Scaling**: Hardware backend supports up to 4K native resolution output, while the software backend improves visuals through xBR and JINC scaling.
- **Memory Tools**: Memory editing and search functionality for advanced users to modify game behavior.
- **Cheat Support**: Enable cheat codes to unlock hidden content or adjust game difficulty.
- **Online Multiplayer**: Supports networked gameplay to relive classic gaming experiences.
- **Save Management**: Easily manage multiple save files.

## Performance Overview 🚀

| Rendering Mode | Memory Usage | Recommended Hardware | Backend Mode          |
|----------------|--------------|----------------------|-----------------------|
| D2D            | ~32MB        | Older Machines       | Software              |
| D3D            | ~52MB        | Older Devices        | Software              |
| OpenGL         | ~86MB / ~138MB | Modern Devices     | Software / OpenGL     |
| Vulkan         | ~120MB / ~143MB | Modern Devices     | Software / Vulkan     |

> **Smooth Performance Test**: Runs at 60 FPS on an Intel Celeron i3 3215u. *No gamedb, no reshade.*

> **Hardware Backend**: Better native graphics quality, lower CPU usage  
> OpenGL requires a GPU supporting OpenGL 3.3+  
> Vulkan requires a GPU supporting Vulkan 1.1+

_Figure 1: Main Interface(UI text follows system language)_  
![psx 1 eng](https://github.com/user-attachments/assets/a1e52f58-12e7-42ec-b819-965a0ce82caf)


_Figure 2：ReShade(UI text follows system language)_<br>
![psx 3](https://github.com/user-attachments/assets/4ccdf2d6-f79f-4dd5-a131-9365bfc878b6)

### How to Use 🛠️

#### 1. Setting Up BIOS 🔑
> **Note**: Due to legal restrictions, the emulator does not include BIOS files. Please obtain a legal BIOS file.
- Extract the BIOS file (e.g., `SCPH1001.BIN`) from your PlayStation console.
- Place the file in the emulator's `bios` folder:
/ScePSx<br>
├── bios/<br>
│ └── SCPH1001.bin<br>
├── saves/<br>
└── ScePSX.exe<br>

#### 2. Using ReShade 🎨
- ReShade is available in OpenGL and Vulkan rendering modes.
- > For D3D, ReShade needs to be installed separately.
- Press **Home** to open the ReShade settings interface.
- Load pre-configured Shader files (several presets are available).

#### 3. Multi-Disc Games 📀
- **Memory Card 1**: Each disc uses its own memory card.
- **Memory Card 2**: Shared across all discs, recommended for multi-disc games.

#### 4. Controller Settings ⌨️🎮
- Keyboard settings can be configured in the File menu.
- Controllers are plug-and-play, no additional setup required.

## Frequently Asked Questions ❓

### Q: Why can't I start the game?
A: Ensure the following:
1. The BIOS file is correctly set up.
2. The game image file format is correct (e.g., `.bin/.cue`, `.img/.cue`, or `.iso`).

### Q: How do I get more ReShade Shaders?
A: Visit the [ReShade Official Website](https://reshade.me/) to download Shader files and place them in the `reshade/` folder:

### Q: What games are supported by the emulator?
A: Most common games are supported.

### Q: What should I do if CPU usage is too high?
A: If CPU usage is high, try using the D2D renderer or reduce the internal resolution.

### Q: How can I improve native graphics quality?
A: Press F11 multiple times and use the Home key to select ReShade for enhanced graphics.

### Q: How do I fix audio desynchronization issues?
A: Try adjusting the audio buffer size or switching the audio output device.

### Q: Does it support all PS1 region versions?
A: Yes, it supports NTSC-J, NTSC-U, and PAL formats.

### Q: Why isn't the controller vibrating?
A: For games that support vibration feedback, you need to press **F10** to switch to **analog mode**

### Q: Why does Vulkan use the most memory?
A: Because it requires extra memory to store:
- 3 out-of-order command buffers
- 5 unsigned synchronization protocols
- 11 validation layer jokes
- Developer's precious hair samples

### Q: Is cross-platform support available?
A: Currently, only Windows is supported. Future plans include Linux/macOS support via .NET MAUI or Avalonia.

## How to Build
1. The project is based on .NET 8.0 framework.
2. SDL declarations are included in the code. Place the SDL2 DLL in the build directory.
3. For OpenGL, install the OpenGL.NET NuGet package (.NET 4.7 framework, may have compatibility issues) or manually add dependencies using OpenGL.dll (.NET 8.0 compiled).
4. For Vulkan, use the vk NuGet package or manually add dependencies using vk.dll.
5. If using a framework below .NET 8.0, modify the project file manually.
6. Some core code is based on https://github.com/BluestormDNA/ProjectPSX.

## How to Contribute 🤝
We welcome contributions to ScePSX, including code submissions, issue reporting, or documentation improvements. Here’s how you can participate:
- **Submit Issues**: Report problems or suggestions on the [Issues](https://github.com/unknowall/ScePSX/issues) page.
- **Submit PRs**: Fork the project and submit Pull Requests.
- **Translation Support**: If you’re fluent in other languages, help translate README or UI text.

# Downloads 📥
- **Lightweight Version (1.51 MB)**: Core features only, ideal for quick testing.
- **Full Version (8.02 MB)**: Includes all features (e.g., ReShade integration).
- **GameDB Database**: Optional download for automatic game configuration recognition.
- **ControllerDB Database**: Optional download for extended controller support.

[Click here to download the latest version](https://github.com/unknowall/ScePSX/releases)

### Legal Disclaimer ⚖️
ScePSX is an open-source project intended solely for learning and research purposes. Ensure you have legal game ROMs and BIOS files and comply with relevant laws and regulations.
</details>

## 主要功能 🎮
- **即时存档/读档**: 随时保存和加载游戏进度。
- **多渲染器支持**: 动态切换 D2D、D3D、OpenGL、Vulkan 渲染器，适配不同硬件配置。
- **ReShade 集成**: D3D、OpenGL、Vulkan 支持 ReShade 后处理效果，增强画质。
- **分辨率调节**: 硬件后端可输出4K原生分辨率，软件后端可通过xBR,JINC提升视觉体验。
- **内存工具**: 提供内存编辑和搜索功能，适合高级用户修改游戏行为。
- **金手指支持**: 开启作弊功能，解锁隐藏内容或调整游戏难度。
- **网络对战**: 支持联机对战，重温经典游戏乐趣。
- **存档管理**: 方便管理多个游戏存档。

**项目已同步至 Gitee 以及 Gitcode 国内用户可优先访问以加速下载。镜像仓库自动同步更新，确保内容一致**

## 性能表现 🚀

| 渲染模式 | 内存占用 | 推荐硬件 | 后端模式          |
|----------|----------|----------|-------------------|
| D2D      | ~32MB    | 老机器   | software          |
| D3D      | ~52MB    | 较老设备 | software          |
| OpenGL   | ~86MB / ~138MB   | 现代设备 | software / OpenGL |
| Vulkan   | ~120MB / ~143MB  | 现代设备 | software / Vulkan          |

> **流畅运行测试**: 在 Intel 赛扬 i3 3215u 上以 60 FPS 流畅运行。*不使用gamedb, 不使用reshade

> **硬件后端**: 更好的原生画质，更低的CPU使用率<br>
> OpenGL 需支持OpenGL 3.3以上的显卡<br>
> Vulkan 需支持Vulkan 1.1以上的显卡<br>

_图1：主界面 (UI文本跟随系统语言)_<br>
![psx 1](https://github.com/user-attachments/assets/6166e262-a587-4d26-ad2a-d74e05697ccc)

<!-- ![ogl](https://github.com/user-attachments/assets/fad3885b-f0eb-4168-a4ab-60e2d75b79f0) -->

_图2：ReShade界面 (UI文本跟随系统语言)_<br>
![psx 3](https://github.com/user-attachments/assets/4ccdf2d6-f79f-4dd5-a131-9365bfc878b6)

<!-- ![捕获233](https://github.com/user-attachments/assets/fb0ba1a7-3dc8-428a-8d79-25d1e03677a9) -->

### 如何使用 🛠️

#### 1. 设置 BIOS 🔑
> **注意**: 由于法律限制，模拟器不附带 BIOS 文件，请自行获取合法 BIOS。
- 比如从你的 PlayStation 主机中提取 BIOS 文件（如 SCPH1001.BIN）
- 将文件放入模拟器的 `bios` 文件夹中：
- /ScePSx
- ├── bios/
- │ └── SCPH1001.bin
- ├── saves/
- └── ScePSX.exe

#### 2. 使用 ReShade 🎨
- ReShade 在 OpenGL、Vulkan 渲染模式下可用
- >D3D需额外安装reShade。
- 按 **Home 键** 打开 ReShade 设置界面。
- 可加载预设的 Shader 文件（已有多款可供选择）。
  
#### 3. 多光盘游戏 📀
- **存储卡1**: 每张光盘独立使用。
- **存储卡2**: 所有光盘共用，推荐用于多光盘游戏。
  
#### 4. 控制设置 ⌨️🎮
- 键盘设置在文件菜单中完成。
- 手柄无需额外设置，即插即用。
  
## 常见问题 ❓

### Q: 为什么无法启动游戏？
A: 请确保：
1. 已正确设置 BIOS 文件。
2. 游戏镜像文件格式正确（如 `.bin/.cue` 或 `.img/.cue` 或 `.iso`）。

### Q: 如何获取更多 ReShade Shader？
A: 访问 [ReShade 官方网站](https://reshade.me/) 下载 Shader 文件，并将其放入 `reshade/` 文件夹中。
- /ScePSx
- ├── reshade/
- │ └── 放在这里
- ├── saves/
- └── ScePSX.exe

### Q: 模拟器支持哪些游戏？
A: 绝大部分常见的游戏都已支持。

### Q: CPU 占用较高怎么办？
A: 如果 CPU 占用过高，建议使用 D2D 渲染器或降低内部分辨率。

### Q: 如何获得更好的画质
A: 多按几下F11，建议配合home键选择ReShade增强画质

### Q: 如何解决音效不同步的问题？
A: 尝试调整音频缓冲区大小，或更换音频输出设备。

### Q: 是否支持 PS1 的所有区域版本？
A: 是的，支持 NTSC-J、NTSC-U 和 PAL 格式的游戏。

### Q: 为什么手柄不会震动？
A: 对于支持震动的游戏，您需要按下F10切换至模拟手柄。

### Q: 为什么 Vulkan 的内存占用最高？
A: 因为它需要额外内存来存储：  
- 3个时间线错乱的命令缓冲
- 5份未签署的同步协议
- 11个验证层冷笑话
- 开发者珍贵的头发样本

### Q: 是否支持跨平台？
A: 目前仅支持 Windows，未来计划通过 .NET MAUI 或 Avalonia 实现 Linux/macOS 支持。

## 如何编译
1. 项目是.net 8.0 框架
2. SDL 声明文件已经在代码中包含，把SDL2的DLL放到生成目录中即可
3. OpenGL 可以安装 OpenGL.NET NuGet包(.net 4.7 框架，存在兼容性问题)<br>
   或手动添加依赖项使用 OpenGL.dll (.net 8.0 编译)
5. Vulkan 使用 vk NuGet包，或手动添加依赖项使用 vk.dll
6. 如果使用低于 .net 8.0 框架，可手动修改项目文件
7. Core的部分代码基于 https://github.com/BluestormDNA/ProjectPSX

## 如何贡献 🤝
欢迎为 ScePSX 提交代码、报告问题或改进文档！以下是参与方式：
- **提交 Issue**: 在 [Issues](https://github.com/unknowall/ScePSX/issues) 页面报告问题或提出建议。
- **提交 PR**: Fork 本项目并提交 Pull Request。
- **翻译支持**: 如果你熟悉其他语言，欢迎帮助翻译 README 或 UI 文本。

- 国内的朋友可以在下面这里提出汉化ROM兼容性问题(感谢miku233, lzsgodmax转载)

- ![老男人](https://img.shields.io/badge/Oldman-Emu-老男人) [讨论贴 https://bbs.oldmantvg.net/thread-77207.htm](htps://bbs.oldmantvg.net/thread-77207.htm)
- ![chinaemu](https://img.shields.io/badge/China-Emu-org) [讨论贴 http://bbs.chinaemu.org/read-htm-tid-129832.html]([htps://bbs.oldmantvg.net/thread-77207.htm](http://bbs.chinaemu.org/read-htm-tid-129832.html))

# 下载 📥

- **轻量版 (1.51 MB)**: 仅包含核心功能，适合快速体验。
- **完整版 (8.02 MB)**: 包含所有功能（如 ReShade 集成）。
- **GameDB 数据库**: 可选下载，自动识别和加载游戏配置。
- **ControllerDB 数据库**: 可选下载，自动识别更多手柄外设。

[点击这里下载最新版本](https://github.com/unknowall/ScePSX/releases)

### 法律声明 ⚖️
ScePSX 是一个开源项目，仅用于学习和研究目的。请确保您拥有合法的游戏 ROM 和 BIOS 文件，遵守相关法律法规。



