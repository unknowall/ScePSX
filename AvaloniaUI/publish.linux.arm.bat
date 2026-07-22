dotnet publish -c Release -r linux-arm -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=None -p:DebugSymbols=false --self-contained true

copy /Y ..\LIBS\SDL2\armhf\libSDL2.so bin\Release\net8.0\linux-arm\publish\

pause