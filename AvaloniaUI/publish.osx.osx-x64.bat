dotnet publish -c Release -r osx-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=None -p:DebugSymbols=false --self-contained true -o ./bin/Release/Publish

pause