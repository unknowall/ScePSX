dotnet publish -c Release -r linux-arm64 -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=None -p:DebugSymbols=false --self-contained true

pause