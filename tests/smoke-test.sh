#!/bin/bash

function quit() {
  echo "Error at statement above line $1";
  exit $2;
}

echo 'create collection ...'
curl --fail -X POST 'http://localhost:8080/admin/api/v1/collection' \
  -H 'Content-Type: text/turtle' -d '<my-collection> a <https://w3id.org/ldes#EventStream> .'
code=$? && if [ $code != 0 ] ; then quit $LINENO $code ; fi
echo '... done.'
echo ''

echo 'retrieve collection ...'
curl --fail 'http://localhost:8080/admin/api/v1/collection/my-collection' -H 'accept: */*'
code=$? && if [ $code != 0 ] ; then quit $LINENO $code ; fi
echo '... done.'
echo ''

echo 'retrieve LDES ...'
curl --fail -i 'http://localhost:8080/feed/my-collection' -H 'accept: */*'
code=$? && if [ $code != 0 ] ; then quit $LINENO $code ; fi
echo '... done.'
echo ''

echo 'create view ...'
curl --fail -X POST 'http://localhost:8080/admin/api/v1/collection/my-collection/view' -H 'Content-Type: text/turtle' \
  --data-raw '@prefix tree: <https://w3id.org/tree#> . <my-collection/my-view> a tree:Node; tree:fragmentationStrategy (); tree:pageSize "100"^^<http://www.w3.org/2001/XMLSchema#integer> .'
code=$? && if [ $code != 0 ] ; then quit $LINENO $code ; fi
echo '... done.'
echo ''

echo 'ingest member ...'
curl --fail -X POST 'http://localhost:8080/data/my-collection' \
  -H 'Content-Type: text/turtle' -d '<http://en.wikipedia.org/wiki/Mickey_Mouse> a <http://schema.org/Person> .'
code=$? && if [ $code != 0 ] ; then quit $LINENO $code ; fi
echo ''
echo '... done.'
echo ''

echo 'ensuring fragmented (waiting for 10 seconds) ...'
sleep 10s
echo '... done.'
echo ''

echo 'retrieve event source ...'
curl --fail -i 'http://localhost:8080/feed/my-collection/_' -H 'accept: */*'
code=$? && if [ $code != 0 ] ; then quit $LINENO $code ; fi
echo '... done.'
echo ''

echo 'retrieve view ...'
curl --fail -i 'http://localhost:8080/feed/my-collection/my-view' -H 'accept: application/n-triples'
code=$? && if [ $code != 0 ] ; then quit $LINENO $code ; fi
echo '... done.'
echo ''

echo 'delete collection ...'
curl --fail -X DELETE 'http://localhost:8080/admin/api/v1/collection/my-collection'
code=$? && if [ $code != 0 ] ; then quit $LINENO $code ; fi
echo '... done.'
echo ''
