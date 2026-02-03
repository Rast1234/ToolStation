#!/bin/bash -ex

# get tag version "vX" from github or use placeholder
raw_version="${GITHUB_REF_NAME:-v1}"
version="${raw_version//v}"
echo "$raw_version -> $version"
if !  [ "${raw_version}" == v"${version}" ] ; then
    echo "Tag is not in format 'vX': ${raw_version}"
    exit 2
fi
if !  [ "$version" -eq "$version" ] 2>/dev/null; then
    echo "Tag is not in format 'vX': ${version}"
    exit 3
fi

echo "Version is ${version}"

# building eboot.bin requires ps3toolchain
# cd homebrew; make npdrm

dotnet build -warnaserror

# relevant options
# -noconlog
# -p:IncludeNativeLibrariesForSelfExtract=true
# -p:PublishAot=false

for project in */*.csproj ;
do
    dotnet publish "${project}" -p:version="${version}" -o "_publish/runtime-dependent" -c Release -p:DebugType=None -p:DebugSymbols=false -p:PublishSingleFile=true --no-self-contained
    for platform in linux-x64 win-x64 osx-arm64 ;
    do
        dotnet publish "${project}" -p:version="${version}" -o "_publish/${platform}" -r "${platform}" -c Release -p:DebugType=None -p:DebugSymbols=false -p:PublishSingleFile=true --self-contained -p:PublishTrimmed=true
    done
done

ls -lRh _publish