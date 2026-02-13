dotnet publish -c Release  -p:PublishSingleFile=false -p:PublishTrimmed=false -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false --self-contained false -o ./bin/Release/Publish

pause