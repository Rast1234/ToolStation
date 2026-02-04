#!/bin/bash -e

# script for github actions to list published files as release assets

# _publish/$platform/binary.whatever

echo '['
for p in _publish/* ;
do
    platform=${p##*/}
    for file in "${p}"/* ;
    do
        artifact=${file##*/}
        #echo "${p} - ${x} - ${platform} - ${artifact}"
        line='  {"path":"'"${file}"'","name":"'"${artifact}"'","label":"'"${platform}"' build"},'
        echo "${line}"
    done
done
echo ']'
