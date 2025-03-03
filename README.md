<h2>这是一个小巧可用的 PS1 模拟器</h2>

1. 即时存档、读档
2. D2D1, D3D和OpenGL 渲染器，OpenGL可加载着色器，D3D, OpenGL 兼容ReShade
3. 内部分辨率可调节
4. 内存编辑及内存搜索功能
5. 支持金手指
6. 支持双手柄、双键盘
7. 独立的游戏存档及即时存档
8. 支持网络对战



如何使用:

1. BIOS 目录下至少有一个可用BIOS，启动后在文件菜单中选中
2. 键盘设置在文件菜单里，手柄无需设置
3. 自带的 ReShade 需用 OpenGL 渲染器，按 Home 键打开
4. 多光盘游戏请使用存储卡2，存储卡1各盘独立，存储卡2共用
5. 老机器请用D2D渲染器
6. 控制台有详尽的运行日志，想看可以在系统设置里打开

如何编译：

1. 项目是.net 8.0 框架
2. SDL 声明文件已经在代码中包含，把SDL2的DLL放到生成目录中即可
3. OpenGL 可以安装 OpenGL.NET NuGet包(.net 4.7 框架，存在兼容性问题)，或手动添加依赖项使用 OpenGL.dll (.net 8.0 编译)
4. 如果使用低于 .net 8.0 框架，可手动修改项目文件

![new01](https://github.com/user-attachments/assets/869a7da4-a96d-4596-b31f-e64abc097aee)![捕获](https://github.com/user-attachments/assets/6fbc1076-ddc0-4c3f-843a-cbfd63f4d4f8)

以下截图是老版本的

![read2](https://github.com/user-attachments/assets/4e3209e6-04a3-4aab-9072-eb3514d3e381)

![read1](https://github.com/user-attachments/assets/1688f0ec-bd7b-441d-a818-0c06b4e235c4)

![read3](https://github.com/user-attachments/assets/fc688b7c-5852-4213-a58b-e4bd56ab459d)


Core部分代码基于 https://github.com/BluestormDNA/ProjectPSX
