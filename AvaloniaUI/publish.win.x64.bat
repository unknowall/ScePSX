dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=None -p:DebugSymbols=false --self-contained true

copy /Y ..\LIBS\SDL2\win64\SDL2.dll bin\Release\net8.0\win-x64\publish\

pause