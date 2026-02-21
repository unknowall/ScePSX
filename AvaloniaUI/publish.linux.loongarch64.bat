dotnet publish -c Release -r linux-loongarch64 -p:PublishSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false --self-contained true -p:RuntimeFrameworkVersion=8.0.22

pause