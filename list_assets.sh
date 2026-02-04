#!/bin/bash -e

# script for github actions to prepare published files as release assets
echo -n '[' >> assets.json
for p in _publish/* ;
do
    platform=${p##*/}
    for path in "${p}"/* ;
    do
        name=${path##*/}
        archive="${name} ${platform} build.zip"
        echo "${archive} - ${name} - ${platform}"
        zip -j "${archive}" "${path}"
        #line='{"path":"'"${archive}"'","name":"'"${archive}"'","label":"'"${archive}"'"},'
        line='{"path":"'"${archive}"'"},'
        echo -n "${line}" >> assets.json
    done
done
echo -n ']' >> assets.json
