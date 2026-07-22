dotnet publish -c Release -r linux-arm64 -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=None -p:DebugSymbols=false --self-contained true

REM Note: No arm64 libSDL2.so available - requires separate download from SDL website

pause