# LDES-Server.net User Guide
This guide contains information on how to retrieve collections using a LDES-Server.net system, further referred to as the _server_.

Its target audience is technical users with a basic knowledge of LDES, APIs and linked-data. Please see the [LDES step-by-step guide](https://github.com/rorlic/ldes-docs/blob/main/step-by-step/README.md) for more information.

## Discovery
At this moment, you can only use the administrative API (see [LDES-Server.net Administration Guide](./admin-guide.md)) to discover the available collections and views. These will typically not be available to the regular LDES clients. For LDES discovery, the system will support [DCAT](https://semiceu.github.io/DCAT-AP/releases/3.0.0/) in the future.

> [!WARNING]
> Currently, the server does not support DCAT, which can be used for discovering the available collections and views on the server.

## Replicate & Synchronize
In order to replicate and synchronize a data collection using LDES, you need to know the URL of a collection view (or the collection URL). You can pass that initial URL to any LDES client compatible with the [LDES specification](https://w3id.org/ldes/specification). Once initialized, such a LDES client will retrieve the root node of the search tree and then follow the embedded page links to retrieve more nodes until all page links have been followed (replication) after which it will re-request any mutable pages and monitor them for changes (synchronization), typically creating an infinite stream of data members.

### Retrieve Collection
In order to retrieve a collection as LDES including its views, you can request the collection node itself.

> [!NOTE]
> To retrieve a collection node you can use the `/feed/{collectionName}` endpoint, e.g.
> ```bash
> curl http://localhost:8080/feed/my-collection -H "accept: text/turtle"
> ```

### Retrieve View
A collection node may contain one or more view nodes having a relation that contains a link to the root node. When creating a view, the server creates a unique URL based on the collection and view name.

> [!NOTE]
> To retrieve a view root node, you can request a root node using the `/feed/{collectionName}/{viewName}` endpoint, e.g.:
> ```bash
> curl http://localhost:8080/feed/my-collection/my-view -H "accept: text/turtle"
> ```

> [!TIP]
> Because an event source is unnamed, you can request an event source root node using the `/feed/{collectionName}/` endpoint, which is identical to the `/feed/{collectionName}` endpoint (not ending with a slash), i.e. the collection endpoint. In other words, when you request a collection node, the event source root node is immediately included in addition to a _link_ to the root node of each _named_ view.

### Retrieve Page
Normally you do not need to manually retrieve a view page because an LDES client will do this for you. However, if you retrieve a view or collection node using a browser or a command line HTTP tool, you can extract a page link manually and request it using the same browser or HTTP tool. When creating a page to hold data members, the server generated and assigns a GUID to the new page. If you know a page GUID then you can retrieve a page directly.

> [!TIP]
> To manually retrieve a page, you can request a (non-root) node using the `/feed/{collectionName}/{viewName}/{pageGuid}` endpoint, e.g.:
> ```bash
> curl http://localhost:8080/feed/my-collection/my-view/73f61f1d-051b-4cd2-87f7-8d887bde333f -H "accept: text/turtle"
> ```
> Because the default view is named `_`, pages of an event source are available on the  `/feed/{collectionName}/_/{pageGuid}` endpoint.

### Supported Formats
When retrieving a collection, view or page you can request the node in any of the supported [RDF formats](./admin-guide.md#supported-rdf-formats), both triple and quad formats.

Both the N-triples and N-quads format will always be returned using the [TREE profile](https://treecg.github.io/specification/profile) to allow for streaming parsing of a node, which is preferred over the regular [member extraction algorithm](https://treecg.github.io/specification/#member-extraction-algorithm) for performance reasons. But, of course, the returned node is also compatible with the latter algorithm.

