dotnet publish -c Release -r osx-arm64 -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=None -p:DebugSymbols=false --self-contained true

pause