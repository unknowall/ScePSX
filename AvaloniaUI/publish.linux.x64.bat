dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=None -p:DebugSymbols=false --self-contained true

copy /Y ..\LIBS\SDL2\amd64\libSDL2.so bin\Release\net8.0\linux-x64\publish\

pause