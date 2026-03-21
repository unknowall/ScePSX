dotnet publish -c Release -r linux-loongarch64 -p:PublishSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false --self-contained true -p:RuntimeFrameworkVersion=8.0.22

copy /Y ..\LIBS\SDL2\loongarch64\libSDL2.so bin\Release\net8.0\linux-loongarch64\publish\

pause