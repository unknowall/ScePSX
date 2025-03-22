<h2>这是一个完全用 c# 开发，小巧可用的 PS1 模拟器</h2>

![License](https://img.shields.io/badge/license-MIT-blue) ![GitHub Release](https://img.shields.io/github/v/release/unknowall/ScePSX?label=Release) ![Language](https://img.shields.io/github/languages/top/unknowall/ScePSX) ![Build Status](https://img.shields.io/badge/build-passing-brightgreen) [![Gitee Repo](https://img.shields.io/badge/Gitee-Mirror-FFB71B)](https://gitee.com/unknowall/ScePSX)
## 主要功能 🎮
- **即时存档/读档**: 随时保存和加载游戏进度。
- **多渲染器支持**: 动态切换 D2D、D3D、OpenGL、Vulkan 渲染器，适配不同硬件配置。
- **ReShade 集成**: D3D、OpenGL、Vulkan 支持 ReShade 后处理效果，增强画质。
- **分辨率调节**: 硬件后端可达4K原生分辨率，软件后端可通过xBR,JINC提升视觉体验。
- **内存工具**: 提供内存编辑和搜索功能，适合高级用户修改游戏行为。
- **金手指支持**: 开启作弊功能，解锁隐藏内容或调整游戏难度。
- **网络对战**: 支持联机对战，重温经典游戏乐趣。
- **存档管理**: 方便管理多个游戏存档。

<b>the english version is available starting from Beta 0.1.0.</b>

**项目已同步至 Gitee，国内用户可优先访问以加速下载。镜像仓库自动同步更新，确保内容一致**

## 性能表现 🚀

| 渲染模式 | 内存占用 | 推荐硬件 | 后端模式          |
|----------|----------|----------|-------------------|
| D2D      | ~32MB    | 老机器   | software          |
| D3D      | ~52MB    | 较老设备 | software          |
| OpenGL   | ~86MB / ~138MB   | 现代设备 | software / OpenGL |
| Vulkan   | ~120MB   | 现代设备 | software          |

> **流畅运行测试**: 在 Intel 赛扬 i3 3215u 上以 60 FPS 流畅运行。*不使用gamedb, 不使用reshade

> **硬件后端**: 更好的原生画质，更低的CPU使用率，但需支持OpenGL 3.3以上的显卡 (不支持的已经很难找的到了)

_图1：使用硬件后端运行游戏的画面效果_<br>
![ogl](https://github.com/user-attachments/assets/fad3885b-f0eb-4168-a4ab-60e2d75b79f0)

_图2：ScePSX 主界面展示_<br>
![捕获](https://github.com/user-attachments/assets/88c1f283-127c-4f74-9cbe-7e64def43962)

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
- 可加载预设的 Shader 文件（位于 `ReShade/` 文件夹中）。
  
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

### Q: 我的显示器是4K的，需要更好的原生画质
A: 多按几下F11，建议配合home键选择ReShade增强画质

### Q: 如何解决音效不同步的问题？
A: 尝试调整音频缓冲区大小，或更换音频输出设备。

### Q: 是否支持 PS1 的所有区域版本？
A: 是的，支持 NTSC-J、NTSC-U 和 PAL 格式的游戏。

### Q: 是否支持跨平台？
A: 目前仅支持 Windows，未来计划通过 .NET MAUI 或 Avalonia 实现 Linux/macOS 支持。

## 如何编译
1. 项目是.net 8.0 框架
2. SDL 声明文件已经在代码中包含，把SDL2的DLL放到生成目录中即可
3. OpenGL 可以安装 OpenGL.NET NuGet包(.net 4.7 框架，存在兼容性问题)，或手动添加依赖项使用 OpenGL.dll (.net 8.0 编译)
4. Vulkan 使用 vk NuGet包，或手动添加依赖项使用 vk.dll
5. 如果使用低于 .net 8.0 框架，可手动修改项目文件
6. Core部分代码基于 https://github.com/BluestormDNA/ProjectPSX

## 如何贡献 🤝
欢迎为 ScePSX 提交代码、报告问题或改进文档！以下是参与方式：
- **提交 Issue**: 在 [Issues](https://github.com/unknowall/ScePSX/issues) 页面报告问题或提出建议。
- **提交 PR**: Fork 本项目并提交 Pull Request。
- **翻译支持**: 如果你熟悉其他语言，欢迎帮助翻译 README 或 UI 文本。

# 下载 📥

- **轻量版 (1.50 MB)**: 仅包含核心功能，适合快速体验。
- **完整版 (7.99 MB)**: 包含所有功能（如 ReShade 集成）。
- **GameDB 数据库**: 可选下载，自动识别和加载游戏配置。

[点击这里下载最新版本](https://github.com/unknowall/ScePSX/releases)

### 法律声明 ⚖️
ScePSX 是一个开源项目，仅用于学习和研究目的。请确保您拥有合法的游戏 ROM 和 BIOS 文件，遵守相关法律法规。



