#!/bin/bash -e

# script for github actions to prepare published files as release assets
echo -n '[' >> assets.json
for p in _artifacts/publish/*/release_* ;
do
    # p = _artifacts/publish/PROJECT/release_PLATFORM
    archive=$(echo $p | sed -r 's|_artifacts/publish/(.+)/release_(.+)/?$|\1.\2.zip|')
    zip -j "${archive}" "${p}"
    line='{"path":"'"${archive}"'"},'
    echo -n "${line}" >> assets.json
done
echo -n ']' >> assets.json
