#!/bin/bash
export SCRIPT_PATH=$(dirname -- "$( readlink -f -- "${BASH_SOURCE:-$0}"; )")
export LDES_PATH=$SCRIPT_PATH/ldes

curl --fail -X POST 'http://localhost:8080/admin/api/v1/collection' -H 'Content-Type: text/turtle' -d "@$LDES_PATH/connections.ttl"
code=$?
if [ $code != 0 ] 
    then exit $code
fi

curl --fail -X POST 'http://localhost:8080/admin/api/v1/collection/connections/view' -H 'Content-Type: text/turtle' -d "@$LDES_PATH/connections.by-page.ttl"
code=$?
if [ $code != 0 ] 
    then exit $code
fi
