# ScePSX - Lightweight PS1 emulator Fully Developed in C#

![License](https://img.shields.io/badge/license-MIT-blue) ![GitHub Release](https://img.shields.io/github/v/release/unknowall/ScePSX?label=Release) ![Language](https://img.shields.io/github/languages/top/unknowall/ScePSX) ![Build Status](https://img.shields.io/badge/build-passing-brightgreen) [![Gitee Repo](https://img.shields.io/badge/Gitee-Mirror-FFB71B)](https://gitee.com/unknowall/ScePSX)

## Key Features 🎮
- **Save States**: Save and load game progress at any time.
- **Multi-Renderer Support**: Dynamically switch between D2D, D3D, OpenGL, and Vulkan renderers to adapt to different hardware configurations.
- **ReShade Integration**: ReShade post-processing effects supported on D3D, OpenGL, and Vulkan for enhanced graphics.
- **Resolution Scaling**: Hardware backend supports up to 4K native resolution output, while the software backend improves visuals through xBR and JINC scaling.
- **Memory Tools**: Memory editing and search functionality for advanced users to modify game behavior.
- **Cheat Support**: Enable cheat codes to unlock hidden content or adjust game difficulty.
- **Online Multiplayer**: Supports networked gameplay to relive classic gaming experiences.
- **Save Management**: Easily manage multiple save files.

**The English version is available starting from Beta 0.1.0.**

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

_Figure 1: Gameplay with hardware backend_  
![ogl](https://github.com/user-attachments/assets/fad3885b-f0eb-4168-a4ab-60e2d75b79f0)

_Figure 2: ScePSX Main Interface_  
![capture](https://github.com/user-attachments/assets/88c1f283-127c-4f74-9cbe-7e64def43962)

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

### Q: My monitor is 4K. How can I improve native graphics quality?
A: Press F11 multiple times and use the Home key to select ReShade for enhanced graphics.

### Q: How do I fix audio desynchronization issues?
A: Try adjusting the audio buffer size or switching the audio output device.

### Q: Does it support all PS1 region versions?
A: Yes, it supports NTSC-J, NTSC-U, and PAL formats.

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
