dotnet publish -c Release  -r linux-loongarch64 -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=None -p:DebugSymbols=false -o ./bin/Release/Publish

pause