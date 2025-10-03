# LDES Server Caching
An [LDES Server](https://github.com/Informatievlaanderen/VSDS-LDESServer4J) allows to ingest version objects, which is essentially an immutable version of an object's state, to create one or more views on these LDES members and to retrieve them using fragments of such a view. The LDES Server can be configured to serve the collection of LDES members using a collection name of choice as well as a number of views which can also be named freely. When requesting the LDES itself the LDES Server returns information about the collection as well as information about the available view(s). When requesting a view the LDES Server returns information about the view itself and a link to the first fragment from where the complete collection of LDES members can be retrieved. 

Because an LDES contains version objects, the collection can consist of a huge number of LDES members. Serving the whole collection in one response is obviously not possible, therefore each fragment contains a limited number of LDES members and a number of links to other fragments from where more LDES members can be retrieved. An LDES Server allows to configure this LDES members limit per fragment. When a fragment contains this maximum amount of LDES members and no more fragment links will be added, it can be marked as immutable. This allows the fragment to be cached on a reverse proxy or load balancer to allow for better scaling. Note that an LDES collection will change over time as new version objects are ingested and therefore some fragments (e.g. incomplete) will not be marked as immutable but rather marked as temporary cacheable to better handle the load of the LDES server.

This demo shows the mechanisms provided in the LDES server to allow for ingesting LDES members, as well as serving and caching LDES fragments. In particular, we demonstrate the follow features:
* configuring the collection and view names
* configuring the fragment member limit
* configuring the cache time for fragments
* accepted member formats on ingest
* accepted fragment formats on request
* supported CORS and http verbs (preflight)
* supported caching features (etag, max-age, immutable, etc.)
* use [nginx](https://nginx.org/) (pronounced 'Engine-X') as cache server
* use nginx for compression of RDF types
* ability of the LDES client to handle http 304 (not modified)

## Test Setup
To demonstrate the above features we use a number of Docker containers as part of a [context](./docker-compose.yml), more specific an LDES Server with a MongoDB for its storage and an nginx [configured](./nginx.conf) to show the cacheability capabilities.

## Test Execution and Verification
> **Note** that we use a **bash shell** for executing all commands in this demo. In particular a PowerShell terminal *does not have* a compatible [curl](https://curl.se/) command. On windows, please use a [git](https://git-scm.com/downloads/win) bash, a [Mingw-w64](https://www.mingw-w64.org/) environment, a [WSL](https://learn.microsoft.com/en-us/windows/wsl/install) etc.

To start all the systems in the context execute the following command:
```bash
clear
docker compose up -d --wait
chmod +x ./server/seed.sh
sh ./server/seed.sh
```

### Verify URL Naming Strategy
As shown in the [test setup](#test-setup) the LDES Server allows to specify the collection name and the view name. Based on these configurable settings, the LDES server will accept requests on the URL `http://localhost:8080/feed/<collection-name>/<view-name>`. E.g. if you keep the default settings, the collection is available at http://localhost:8080/occupancy and the view at http://localhost:8080/occupancy/by-page, or using bash commands:
```bash
curl http://localhost:8080/feed/occupancy/by-page
```
this results in:
```
@prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>.
@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#>.
@prefix xsd: <http://www.w3.org/2001/XMLSchema#>.
@prefix ldes: <https://w3id.org/ldes#>.
@prefix tree: <https://w3id.org/tree#>.
@prefix dct: <http://purl.org/dc/terms/>.
@prefix prov: <http://www.w3.org/ns/prov#>.

<http://ldes-server:8080/feed/occupancy> a ldes:EventStream;
                                         ldes:timestampPath dct:modified;
                                         ldes:versionOfPath dct:isVersionOf;
                                         tree:view <http://ldes-server:8080/feed/occupancy/by-page>.
<http://ldes-server:8080/feed/occupancy/by-page> a tree:Node.
```

### Verify Acceptable Fragment Formats
The LDES Server allows to request the collection (including the views and fragments) as the following formats: N-triples, N-quads, Turtle and JSON-LD.
> **Note** that you can use `-H` or `--header` which is equivalent.
```
curl -I -H "accept: text/turtle" http://localhost:8080/feed/occupancy/by-page
curl -I -H "accept: application/n-quads" http://localhost:8080/feed/occupancy/by-page
curl -I -H "accept: application/ld+json" http://localhost:8080/feed/occupancy/by-page
curl -I -H "accept: application/n-triples" http://localhost:8080/feed/occupancy/by-page
```
this results in (something similar to):
```text
HTTP/1.1 200 OK
Server: nginx/1.28.0
Date: Sat, 09 Aug 2025 12:17:15 GMT
Content-Type: text/turtle; charset=utf-8
Connection: keep-alive
Cache-Control: public, max-age=10
ETag: 9301EFDBD7B3F491F0154B4DA5F61DFD
Last-Modified: Sat, 09 Aug 2025 12:17:15 GMT
X-Cache-Status: MISS
```
```text
HTTP/1.1 200 OK
Server: nginx/1.28.0
Date: Sat, 09 Aug 2025 12:17:15 GMT
Content-Type: application/n-quads; charset=utf-8; profile="https://w3id.org/tree/profile"
Connection: keep-alive
Cache-Control: public, max-age=10
ETag: 15489A839B472D15E142C848B2362427
Last-Modified: Sat, 09 Aug 2025 12:17:15 GMT
X-Cache-Status: MISS
```
```text
HTTP/1.1 200 OK
Server: nginx/1.28.0
Date: Sat, 09 Aug 2025 12:17:15 GMT
Content-Type: application/ld+json; charset=utf-8
Connection: keep-alive
Cache-Control: public, max-age=10
ETag: 09694CDAEE98B8A151E51E0888D37E63
Last-Modified: Sat, 09 Aug 2025 12:17:15 GMT
X-Cache-Status: MISS
```
```text
HTTP/1.1 200 OK
Server: nginx/1.28.0
Date: Sat, 09 Aug 2025 12:17:15 GMT
Content-Type: application/n-triples; charset=utf-8; profile="https://w3id.org/tree/profile"
Connection: keep-alive
Cache-Control: public, max-age=10
ETag: C403FB98DBD2B9EEFB61A5E906B26217
Last-Modified: Sat, 09 Aug 2025 12:17:15 GMT
X-Cache-Status: MISS
```

### Verify Acceptable Member Formats
The LDES Server allows to ingest the following RDF formats: N-triples, Turtle and JSON-LD.
> **Note** that you can use `-i` or `--include`, `-X` or `--request`, `-d` or `--data` which are pairwise equivalent.
```
curl -i -X POST http://localhost:8080/data/occupancy -H 'Content-Type: text/turtle' -d '@data/member.ttl'
curl -i -X POST http://localhost:8080/data/occupancy -H 'Content-Type: application/ld+json' -d '@data/member.jsonld'
curl -i -X POST http://localhost:8080/data/occupancy -H 'Content-Type: application/n-triples' -d '@data/member.nt'
```
these commands all results in (something similar to):
```
HTTP/1.1 201 Created
Server: nginx/1.28.0
Date: Sat, 09 Aug 2025 12:19:28 GMT
Content-Type: application/json; charset=utf-8
Transfer-Encoding: chunked
Connection: keep-alive
```

### Verify CORS and Supported HTTP Verbs
The LDES Server is setup to accept requests from anywhere and, if needed, access should be restricted in another way. You can easily verify CORS using:
```bash
curl -i -X OPTIONS -H "Origin: http://www.example.com" -H "Access-Control-Request-Method: GET HEAD" http://localhost:8080/feed/occupancy
```
this results in:
```
HTTP/1.1 204 No Content
Server: nginx/1.28.0
Date: Sat, 09 Aug 2025 12:21:00 GMT
Connection: keep-alive
Access-Control-Allow-Methods: GET HEAD
Access-Control-Allow-Origin: *
```
> **Note** that in the above response the allowed HTTP verbs are both GET and HEAD, as requested.

You can verify this using:
```bash
curl -I http://localhost:8080/feed/occupancy/by-page
```
> **Note** that you can use `-I` or `--head` which are equivalent.

Which results in something similar to:
```
HTTP/1.1 200 OK
Server: nginx/1.28.0
Date: Sat, 09 Aug 2025 12:21:40 GMT
Content-Type: text/turtle; charset=utf-8
Connection: keep-alive
Cache-Control: public, max-age=10
ETag: FF6901C7C6FA5CA1F334CD8B6B985AFB
Last-Modified: Sat, 09 Aug 2025 12:21:40 GMT
X-Cache-Status: EXPIRED
```
> **Note** that the fragment information also includes an `ETag` which can be used by the proxy server (nginx) for caching purposes.

### Verify Caching Features
To verify which caching features the LDES Server provides you can again use:
```bash
curl -I http://localhost:8080/feed/occupancy/by-page
```
this results in:
```
HTTP/1.1 200 OK
Server: nginx/1.28.0
Date: Sat, 09 Aug 2025 12:22:10 GMT
Content-Type: text/turtle; charset=utf-8
Connection: keep-alive
Cache-Control: public, max-age=10
ETag: CC1037E8E82A283EDA98066F0A1A6B72
Last-Modified: Sat, 09 Aug 2025 12:22:02 GMT
X-Cache-Status: HIT
```
Notice that the headers include `Cache-Control: public,max-age=10` which indicates that the response can be cached (publicly) for a duration of 10 seconds. In addition the header `ETag: "CC1037E8E82A283EDA98066F0A1A6B72"` defines a unique hash which can be used to verify that the content did not change, using `--head` or `-I` which do not request the content, only the headers.

### Verify Actual Caching
The above caching features allow to setup nginx (or another system) as a caching server for the LDES Server. The nginx [configuration](./nginx.conf) is setup as a proxy server for the LDES server and with a cache named `static-cache` which stores the responses for any HTTP verb. In addition it adds a `X-Cache-Status` header to its responses to indicate a cache `Miss`, `Hit` or `Expired`.

Nginx returns `X-Cache-Status: Miss` if the requested URL was not available and it queried the LDES server for the response. It returns `X-Cache-Status: Hit` if the response was found it the cache and it was not yet expired. Finally, it returns `X-Cache-Status: Expired` if the response in cache was expired and the LDES was queried for the response.

A requested resource is considered expired if it has been in the cache for more than the lesser of the nginx setting (`proxy_cache_valid`, set to 60 minutes in the nginx [configuration](./nginx.conf)) and the `max-age` value from the `Cache-Control` in the headers. This max-age value is added by the LDES server based on the configured values for mutable (`REST_MAXAGE`, set to 60 seconds in the [Docker compose](./docker-compose.yml) file) and immutable(`REST_MAXAGEIMMUTABLE`, set to 604800 seconds in the [Docker compose](./docker-compose.yml) file, which is 420 days) fragments. Summarized, in our case:

* if you request a resource for the first time, you get `X-Cache-Status: Miss` and nginx requests the resource from the LDES server and caches it locally
* if you re-request a mutable resource within 60 seconds, nginx gets it from cache and returns `X-Cache-Status: Hit`
* if you re-request an immutable resource within 60 minutes, nginx gets it from cache and returns `X-Cache-Status: Hit`
* if you re-request a mutable resource after 60 seconds, nginx requests the resource from the LDES server, replaces the version in the cache with the response and returns `X-Cache-Status: Expired`
* if you re-request an immutable resource after 60 minutes, nginx requests the resource from the LDES server, replaces the version in the cache with the response and returns `X-Cache-Status: Expired`

> **Note**: to test the expiration behavior you need to use the [LDES view](http://localhost:8080/occupancy/by-page) becuse the [LDES itself](http://localhost:8080/occupancy) is immutable:

The nginx server is configured to listen to http://localhost:8080 by default, configurable in your `user.env`. The nginx server will forward any request to the LDES server and forward its response to the requester. However, because it is setup for caching, it will first verify if it does not have a response cached for the incoming request and, if so, return the response from cache. 

You can verify this by requesting a fragment twice:
```bash
curl -I http://localhost:8080/feed/occupancy/
```
initially results in:
```
HTTP/1.1 200 OK
Server: nginx/1.28.0
Date: Sat, 09 Aug 2025 12:23:46 GMT
Content-Type: text/turtle; charset=utf-8
Connection: keep-alive
X-Cache-Status: MISS
```
When re-requested:
```bash
curl -I http://localhost:8080/feed/occupancy
```
the result is:
```
HTTP/1.1 200 OK
Server: nginx/1.28.0
Date: Sat, 09 Aug 2025 12:24:04 GMT
Content-Type: text/turtle; charset=utf-8
Connection: keep-alive
X-Cache-Status: HIT
```
> **Note** that the cache did not contain a cached response the first time (`X-Cache-Status: MISS`) but it did the second time (`X-Cache-Status: HIT`).

### Verify Nginx Compression Setup
The RDF file formats are rather verbose and therefore may benefit from compression during network transport. For this reason we have [configured](./nginx.conf) our nginx server to do gzip compression (`gzip on`) for the supported RDF types (`gzip_types application/n-triples application/ld+json text/turtle application/n-quads`) and switched it on (`gzip_static on`) for all forwarded requests. We can easily verify this if we request compression:
```bash
curl -I -H "Accept-Encoding: gzip" http://localhost:8080/feed/occupancy/by-page
```
which results in:
```
HTTP/1.1 200 OK
Server: nginx/1.28.0
Date: Sat, 09 Aug 2025 12:24:28 GMT
Content-Type: text/turtle; charset=utf-8
Connection: keep-alive
Cache-Control: public, max-age=10
Last-Modified: Sat, 09 Aug 2025 12:24:28 GMT
X-Cache-Status: EXPIRED
Content-Encoding: gzip
```
> **Note** the presence of the header `Content-Encoding: gzip` which indicates that the content is compressed using gzip. If we request the content, curl will warn us about the compressed, binary content:
```bash
curl -H "Accept-Encoding: gzip" http://localhost:8080/feed/occupancy/by-page
```
results in:
```
Warning: Binary output can mess up your terminal. Use "--output -" to tell 
Warning: curl to output it to your terminal anyway, or consider "--output 
Warning: <FILE>" to save to a file.
```
if we do this:
```bash
curl -H "Accept-Encoding: gzip" http://localhost:8080/feed/occupancy/by-page --output view.ttl.gz
```
we receive the file and see:
```
  % Total    % Received % Xferd  Average Speed   Time    Time     Time  Current
                                 Dload  Upload   Total   Spent    Left  Speed
100   361    0   361    0     0  36968      0 --:--:-- --:--:-- --:--:-- 40111
```
We can use the following command to unzip it:
```bash
gzip -d view.ttl.gz
```
which results in a [turtle file](./view.ttl):
```bash
cat view.ttl
```
To remove the temporary, downloaded file:
```bash
rm view.ttl
```

## Test Teardown
Stop all systems, i.e.:
```bash
docker compose down
```
