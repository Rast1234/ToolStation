#!/bin/bash -e

# script for github actions to list published files as release assets with readable labels
echo -n '['
for p in _publish/* ;
do
    platform=${p##*/}
    for path in "${p}"/* ;
    do
        name=${file##*/}
        archive="${name} ${platform} build.zip"
        zip -j "${archive}" "${path}"
        line='{"path":"'"${archive}"'","name":"'"${archive}"'","label":"'"${archive}"'"},'
        echo -n "${line}"
    done
done
echo -n ']'
