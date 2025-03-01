<h2>这是一个小巧可用的 PS1 模拟器</h2>

1. 支持即时存档
2. 支持D3D和OpenGL渲染，OpenGL可加载着色器，两者都兼容ReShade
3. 支持xBR放大，支持 2, 4 , 6， 8 倍内部分辨率
4. 内存编辑及内存搜索功能
5. 支持金手指
6. 支持多轴手柄
7. 每个游戏都有独立的游戏存档以及即时存档
8. 多光盘游戏请使用存储卡2，存储卡1独立，存储卡2共用



如何使用:

1. BIOS 目录下至少有一个可用BIOS，启动后在文件菜单中选中
2. 按键设置在文件菜单查看、设置（默认按键：12 WSAD UIJK QERT）
3. 如有需要， F9 键打开控制台查看日志
4. ReShade 按 Home 键打开

如何编译：

1. 项目是.net 8.0 框架
2. SDL 声明文件已经在代码中包含，把SDL2的DLL放到生成目录中即可
3. OpenGL 可以安装NuGet包： OpenGL.NET (不推荐，只支持 .net 4.7 框架)，或者手动添加依赖项使用 OpenGL.dll (.net 8.0 编译)
4. 如果使用低于 .net 8.0 框架，可手动修改项目文件，最低 .net 6.0 以上

![read2](https://github.com/user-attachments/assets/4e3209e6-04a3-4aab-9072-eb3514d3e381)

![read1](https://github.com/user-attachments/assets/1688f0ec-bd7b-441d-a818-0c06b4e235c4)

![read3](https://github.com/user-attachments/assets/fc688b7c-5852-4213-a58b-e4bd56ab459d)


Core部分代码基于 https://github.com/BluestormDNA/ProjectPSX
