#!/bin/bash -ex

# building eboot.bin requires ps3toolchain
# cd homebrew; make npdrm

dotnet build -warnaserror

# relevant options
# -noconlog
# -p:IncludeNativeLibrariesForSelfExtract=true
# -p:PublishAot=false

for project in */*.csproj ;
do
    dotnet publish "${project}" -o "_publish/runtime-dependent" -c Release -p:DebugType=None -p:DebugSymbols=false -p:PublishSingleFile=true --no-self-contained
    for platform in linux-x64 win-x64 osx-arm64 ;
    do
        dotnet publish "${project}" -o "_publish/${platform}" -r "${platform}" -c Release -p:DebugType=None -p:DebugSymbols=false -p:PublishSingleFile=true --self-contained -p:PublishTrimmed=true
    done
done

ls -lRh _publish