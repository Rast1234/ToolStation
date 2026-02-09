#!/bin/bash -ex

# get tag version "vX" from github or use placeholder
raw_version="${GITHUB_REF_NAME:-v1}"
version="${raw_version//v}"
if !  [ "${raw_version}" == v"${version}" ] ; then
    echo "Tag is not in format 'vX': ${raw_version}"
    exit 2
fi
if !  [ "$version" -eq "$version" ] 2>/dev/null; then
    echo "Tag is not in format 'vX': ${version}"
    exit 3
fi

echo "Version is [${version}]"

# building eboot.bin requires ps3toolchain
# cd homebrew; make npdrm

dotnet build -p:version="${version}" -c Release -warnaserror

# relevant options
# -noconlog
# -p:IncludeNativeLibrariesForSelfExtract=true
# -p:PublishAot=false
# --no-self-contained

for platform in linux-x64 win-x64 osx-arm64 ;
do
    dotnet publish -p:version="${version}" -c Release -p:nowarn="IL2104;" -warnaserror --artifacts-path "_artifacts" -r "${platform}" -p:DebugType=None -p:DebugSymbols=false -p:PublishSingleFile=true --self-contained -p:PublishTrimmed=true
done

dotnet pack -p:version="${version}" -c Release -warnaserror --artifacts-path "_artifacts" -p:DebugType=None -p:DebugSymbols=false
#dotnet nuget push _artifacts/package/release/*.nupkg -s "${NUGET_URL}" -k "${NUGET_KEY}"

rm -rf _artifacts/bin
rm -rf _artifacts/obj
ls -lRh _artifacts
# _artifacts/publish/PROJECT/release_PLATFORM/file
# _artifacts/package/release/*.nupkg
