dotnet publish -c Release -r linux-loongarch64 -p:PublishSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false --self-contained true

pause