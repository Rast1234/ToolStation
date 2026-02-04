#!/bin/bash -e

# script for github actions to list published files as release assets with readable labels
echo -n '['
for p in _publish/* ;
do
    platform=${p##*/}
    platform="${platform^^}"
    for file in "${p}"/* ;
    do
        artifact=${file##*/}
        #echo "${p} - ${x} - ${platform} - ${artifact}"
        line='{"path":"'"${file}"'","name":"'"${artifact}"'","label":"'"${artifact} ${platform}"' Build"},'
        echo -n "${line}"
    done
done
echo -n ']'
