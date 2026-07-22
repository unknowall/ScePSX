dotnet publish -c Release -r osx-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=None -p:DebugSymbols=false --self-contained true

copy /Y ..\LIBS\SDL2\osx\libSDL2.dylib bin\Release\net8.0\osx-x64\publish\

pause