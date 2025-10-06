# LDES-Server.net Administrator Guide
This guide contains information that can help you set up and maintain a LDES-Server.net system, further referred to as the _server_.

Its target audience is technical users with a basic knowledge of LDES, docker compose, APIs and linked-data. Please see the [LDES step-by-step guide](https://github.com/rorlic/ldes-docs/blob/main/step-by-step/README.md) for more information.

## Deploy Server
> TODO:
> * describe retrieve docker image
> * describe configure docker compose

## Setup Server
As the server is an ASP.NET core web application, it uses an `appsettings.json` file for its configuration. Any required configuration can be provided to the server by defining a logical environment (e.g. `MY_ENV`), setting the ASP.NET environment variable `ASPNETCORE_ENVIRONMENT` to that value (e.g. `ASPNETCORE_ENVIRONMENT=MY_ENV`) and ensuring the configuration settings file is named `appsettings.MY_ENV.json` and is available in the server's working directory (`/ldes-server.net`). Any variable already present in the `appsettings.json` file is overriden by settings in this custom settings file. All the server configuration in a custom configuration file can be set under the `$/LdesServer` node:
```json
{
  "LdesServer": {
    ...
  }
}
```

> [!TIP] 
> You can also override settings in both configuration files by setting an environment variable named as the variable path with a colon (`:`) as a delimiter, e.g. `Logging:LogLevel:Default=Information`. 

### Database Connection
The server requires a database to store collections, views, members and pages. Currently, a Postgres database is required and the server must be configured with a connection string for full access, allowing DDL commands. Please see the [Postgres documentation](https://www.npgsql.org/doc/connection-string-parameters.html) for the exact format of the connection string.

To configure the **database connection string**, you can set the `$/ConnectionStrings/Postgres` value in a custom configuration file:
  ```json
  {
    "ConnectionStrings": {
      "Postgres": "Host=localhost;Port=5432;Database=my_db;Username=my_user;Password=my_pwd;Persist Security Info=true"
    }
  }
  ```

> [!TIP]
> As indicated before, you can set the `ConnectionStrings:Postgres` as an environment variable as well, e.g. in a docker compose file:
> ```yaml
> services:
>   ldes-server:
>     environment:
>       - ConnectionStrings:Postgres=Host=localhost;Port=5432;Database=my_db;Username=my_user;Password=my_pwd;Persist Security Info=true
> ```

### Core Settings
There are a number of settings related to ingesting and retrieving data that you can to change if needed. Currently, these are the following:
* `$/LdesServer/BaseUri`: a string value that is used as the base URI for an LDES or page URL in the LDES content. The default is `http://localhost:8080/`.
* `$/LdesServer/CreateEventSource`: a boolean value to indicate if an event source (default view named `_`) should be created or not. The default is `true`.
* `$/LdesServer/DefinitionsDirectory`: a directory scanned at launch time for new collection and view definitions, see [Static Collection Definition](#static-collection-definition). No default.

> [!WARNING]
> Please set the `BaseUri` value to the external endpoint URL for your LDES server. This is important because the LDES will use this to create the view and page URL's in the LDES content.

### Supported RDF Formats
Currently the LDES server supports the following serializations (for definitions and data):
* Turtle (mime type `text/turtle`, file extension `.ttl`)
* TriG (mime type `application/x+trig`, file extension `.trig`)
* JSON-LD (mime type `application/ld+json`, file extension `.jsonld`)
* N-Triples (mime type `application/n-triples`, file extension `.nt`)
* N-Quads (mime type `application/n-quads`, file extension `.nq`)

### Fragmentation Settings
To offer a collection as a TREE structure with pages containing a limited number of collection members, the server automatically assigns members to one or more pages. This fragmentation (per view) is in fact a two-step process consisting of calculating one or more virtual buckets based on the member's content (_bucketization_) and then putting this member in one page per bucket (_pagination_). Both processes monitor any newly ingested or bucketized members, and bucketize respectively paginate them.

Both the bucketization and the pagination processes run as an interruptable process that runs as an infinite loop checking for work, performs the available work and then waits for a delay before re-checking and repeating the loop. In each run, the process will attempt to do as much work as possible up to some configurable limits.

The following parameters can be tuned:
* `LoopDelay`: the number of milliseconds to wait between bucketization or pagination runs
* `MemberBatchSize`: the maximum number of members to bucketize per view or paginate per bucket
* `DefaultPageSize`: an integer value for the maximum number of members in a page. This applies to the default view (event source) and is the default for other views if no page size is configured while creating a view. The default value is `250`.

For the fragmentation, you can specify the following values in a custom configuration file (or use environment variables) to change the defaults:
```json
{
  "LdesServer": {
    "Bucketization": {
      "LoopDelay": 2000,
      "MemberBatchSize": 3000,
    },
    "Pagination": {
      "LoopDelay": 3000,
      "MemberBatchSize": 5000,
      "DefaultPageSize": 250
    }
  }
}
```

### Serving Settings
When an LDES Client requests an LDES page from the server, it needs to indicate if that page is immutable (no more members will be added) or not. In both cases, the server will add a configurable period of time (seconds) to indicate for how long the page does not need to be requested again.

The following settings are used for that and can be tuned:
* `$/LdesServer/Serving/MaxAge`: a period of time in seconds to indicate a mutable page freshness. Defaults to `60` seconds.
* `$/LdesServer/Serving/MaxAgeImmutable`: a period of time in seconds to indicate an immutable page freshness. Defaults to `604800` seconds (one week).

### Logging
The server uses ASP.NET core web application's standard mechanism for logging. All custom logging can be enabled by setting the log level of a class or namespace starting with `AquilaSolutions.LdesServer`. As the server uses `Npgsql` for Postgres database access, you can also enable database logging.

E.g.:
```json
{
  "Logging": {
    "LogLevel": {
      "AquilaSolutions.LdesServer": "Information",
      "AquilaSolutions.LdesServer.Ingestion": "Debug",
      "AquilaSolutions.LdesServer.Bucketization": "Debug",
      "AquilaSolutions.LdesServer.Pagination": "Debug",
      "Npgsql.Command": "Information"
    }
  }
}
```

## Create Collections
The first step when creating an LDES is to define the collection that holds the member data. The server needs to know how the collection is named and how members are ingested, in addition to some optional LDES specific settings and custom formatting settings.

A large part of the ingest process consists of five steps:
* split the received linked-data message into individual entities (state or version things)
* search for each entity's identifier
* find the entity's version identifier (or create one if missing)
* create the member identifier (or use one for version things)
* create the member (incl. transforming a version thing back to a state thing)

For each of these ingest steps the server allows a number of options (algorithms). This allows to virtually handle any situation:
* ingest one or more existing version things
* ingest one or more state things
* ingest things with or without a timestamp
* etc.

In fact, the server allows to configure each collection to use a different set of these algorithms. But, do not worry: for each step there is a default so you do not need to specify all 5 options for a newly created collection. 

### Minimal Definition & Defaults
A *minimal collection definition* is similar to:
```text
@prefix ldes: <https://w3id.org/ldes#> .

<{collectionName}> a ldes:EventStream .
```

This defines a new collection named {collectionName} with the following default settings:
* `ldes:timestampPath` set to `prov:generatedAtTime`
* `ldes:versionOfPath` set to `dct:isVersionOf`
* `ingest:splitMessage` using `ingest:SplitMessageByNamedNode`
* `ingest:identifyEntity` using `ingest:IdentifyEntityBySingleNamedNode`
* `ingest:identifyVersion` using `ingest:IdentifyVersionWithIngestTimestamp`
* `ingest:identifyMember` using `ingest:IdentifyMemberByEntityIdAndVersion` (with separator `#`)
* `ingest:createMember` using `ingest:CreateMemberAsIs`

This is identical to:
```text
@prefix ldes: <https://w3id.org/ldes#> .
@prefix dct:  <http://purl.org/dc/terms/> .
@prefix prov: <http://www.w3.org/ns/prov#> .

@prefix lsdn:   <https://ldes-server.net/> .
@prefix ingest: <https://ldes-server.net/ingest#> .

<{collectionName}> a ldes:EventStream ; 
  ldes:timestampPath prov:generatedAtTime ; 
  ldes:versionOfPath dct:isVersionOf ;
  lsdn:ingestion [
    ingest:splitMessage [ a ingest:SplitMessageByNamedNode ];
    ingest:identifyEntity [ a ingest:IdentifyEntityBySingleNamedNode ];
    ingest:identifyVersion [ a ingest:IdentifyVersionWithIngestTimestamp ];
    ingest:identifyMember [ a ingest:IdentifyMemberByEntityIdAndVersion ; ingest:separator "#" ] ;
    ingest:createMember [ a ingest:CreateMemberAsIs ]
  ] .
```

> [!TIP]
> You can specify collection definitions as any of the supported RDF serialization formats, see [RDF formats](#supported-rdf-formats). However, the system will always store the collection and view definitions as Turtle.

### Message Splitting
When a message is received by the server it may contain multiple entities. In order to ingest an individual entity, the server needs to know how to split the message into the contained entities. For this *message splitting* you can choose one of the following options:
* `ingest:SplitMessageAsSingleEntity`: assume the message only contains one entity
* `ingest:SplitMessageByNamedGraph`: assume the message contains one entity in each named graph (the default graph should _not_ exist)
* `ingest:SplitMessageByNamedNode`: assume each named node (and its referenced blank nodes) is a separate entity
* `ingest:SplitMessageByPredicateAndObject`: assume each named node found by querying for a predicate (`ingest:p`) & object (`ingest:o`) combination (and its referenced blank nodes) is a separate entity

### Entity Identification
After splitting into entities, each one is handled on its own. First, the entity needs to be identified. This allows to group all versions of the same entity. The *entity identification* allows for following algorithms:
* `ingest:IdentifyEntityByEntityType`: the entity's identity is given by the (single!) subject of the given (`ingest:o`) type (`rdf:type`) 
* `ingest:IdentifyEntityByPredicateAndObject`: the entity's identity is given by the (single!) subject found when looking for a predicate (`ingest:p`) & object (`ingest:o`) combination
* `ingest:IdentifyEntityBySingleNamedNode`: the entity's identity is given by the (single!) named-node subject

### Version Identification
After identifying the entity itself, the server needs to know which version of the entity is being ingested. This allows to sort versions of the same entity. For *version identification* you can choose one of the following:
* `ingest:IdentifyVersionBySubjectAndPredicatePath`: the entity version is found by using the entity identifier as subject and the given predicate path (`ingest:p`). The found object value should be a `xsd:timestamp` (or a string that can be interpreted as a date/time value).
* `ingest:IdentifyVersionBySubjectAndSequencePath`: the entity version is found by using the entity identifier as subject and the given sequence path (`ingest:p`). The found object value should be a `xsd:timestamp` (or a string that can be interpreted as a date/time value).
* `ingest:IdentifyVersionWithIngestTimestamp`: the entity version is assigned to the timestamp of ingestion. All entities from the message receive the same timestamp value.

### Member Identification
When both the entity identifier and entity version are found or assigned, the member can be identified. For the *member identification* there are two options: 
* `ingest:IdentifyMemberByEntityIdAndVersion`: concatenate the entity identifier and version, separating them by an optional separator (`ingest:separator`), defaults to `#`.
* `ingest:IdentifyMemberWithEntityId`: use the entity identifier as the member identifier. This is used when ingesting (existing) version objects instead of ingesting state objects. 

### Member Creation
The final step is to actually create the member of the collection, which is a structured object consisting of the entity identifier (for grouping members), the entity version (for sorting members within a group), the member identifier, the member ingest time and of course the member content. This content is the collection of triples as found by the message splitting algorithm. However, in the case of ingesting version objects, these triples need to be converted back to a state object (materialized) to allow for consistent handling when retrieving the members of the collection. Therefore, for the *member creation* the server allows for two options:
* `ingest:CreateMemberAsIs`: use the entity content as-is (do not change it). This is used for ingesting state objects.
* `ingest:CreateMemberWithEntityMaterialization`: materialize the entity version object to its original state object, using the given predicate (`ingest:p`) defining the original entity identifier, defaults to .

### Custom Prefixes
Additionally, you can *define the prefixes* to include when exposing the LDES. This will make the prefix-based RDF serializations (i.e. Turtle and TriG) smaller and more readable. You can provide the prefixes by simply adding them as regular prefixes to your collection definition:
```text
...
@prefix ldes:    <https://w3id.org/ldes#> .
@prefix example: <https://example.org/> .
@prefix foaf:    <http://xmlns.com/foaf/0.1/> .

<{collectionName}> a ldes:EventStream .
```

> [!TIP]
> To define prefixes you need to use one of the [RDF formats](#supported-rdf-formats) that support prefixes, i.e. Turtle or TriG (with only a default graph).

### Validating Collection Definition
A collection definition is a structure containing some mandatory and optional properties. As the definition is expressed as linked-data, the server also exposes SHACL shapes so you can validate the collection definition in advance if needed. Obviously, the server will also validate it when you send it a definition.

> [!NOTE]
> To validate a collection definition offline, you can download the collection SHACL from the `/admin/api/v1/shacl/collection` endpoint:
> ```bash
> curl http://localhost:8080/admin/api/v1/shacl/collection -H "accept: text/turtle"
> ```

### Dynamic Collection Definition
After creating a collection definition and optionally validating it offline with the collection SHACL, you can upload the definition to the server.

The server recognizes most popular triple-based RDF serializations, i.e. Turtle and N-Triples. It also accepts quad-formats such as TriG, N-Quads and Json-LD. However, the graph part of the quads is simply ignored, and no named graphs are allowed.

> [!NOTE]
> To upload a collection definition, you need to provide the content type and the actual content to the server `/admin/api/v1/collection` endpoint with a POST verb, e.g.:
> ```bash
> curl -X POST http://localhost:8080/admin/api/v1/collection -H "content-type: text/turtle" -d "</my-collection> a https://w3id.org/ldes#EventStream ."
> ```

> [!TIP]
> You are encouraged to use Turtle format for defining collections (and views) as this allows to define custom prefixes.

### Static Collection Definition
Additionally, you can define your collections by providing them in a directory of your choice accessible by the system (see [Core Settings](#core-settings)). At launch time, the server will scan this directory recursively and for all well-known file extensions it will read the definitions and store the new ones. Please see the standard log for success or failure messages.

> [!NOTE]
> To define one or more collection definitions statically, you need to provide a directory (with read permissions) in the configuration file:
> ```json
> {
>   "LdesServer": {
>     "BaseUri": "https://ldes.example.org/"
>     "CreateEventSource": true,
>     "DefinitionsDirectory": "/ldes-server.net/definitions",
>   }
> }
> ```

## Create Views
When creating a collection the server automatically creates a default view named `/{collection}/_`, which acts as the _event source_. This view is based on a _single-bucket fragmentation_, which results in a _paged view_ that is forwardly-linked (each page points to the next one).

> [!TIP]
> Because an event source is automatically created (unless switched off in the server configuration), you can immediately start to [ingest data](#ingest-data) and clients can [retrieve data](#retrieve-data) soon thereafter.

If you need a different page size or a different fragmentation, you can define an additional view by creating a view definition for a collection and uploading it to the server.

### Standard Paged View 
The _minimal view definition_ is similar to:
```text
@prefix tree: <https://w3id.org/tree#>.

<{collectionName}/{viewName}> a tree:Node .
```

This will define a new paged view named `viewName` for the given collection named `collectionName` that with the following defaults:
* `tree:fragmentationStrategy` set to an empty list, and
* `tree:pageSize` set to the default page size (see [server settings](#ingestion-settings))

> [!TIP]
> Again, you can use a any of the [RDF formats](#supported-rdf-formats), but no named graphs are allowed.

### Time-based View
Instead of a standard paged view, you can create a view which contains a hierarchical (TREE) structure to organize the members according to some timestamp property. Starting from the view node a number of node layers can be found with each layer defining a smaller time period. The leaf nodes are the lowest level and define the smallest time period. All the members whose timestamp property has a value that falls within a leaf time period can be found in a series of forwardly-linked nodes, similar to a pages view. This fragmentation allows to navigate the TREE structure and select only the nodes of interest.

When creating a time-based view you need to define a `lsdn:TimeFragmention` entity as an item of the `tree:fragmentationStrategy` list and as a minimum specify the timestamp property of the members to use for calculating the buckets, similar to:
```text
@prefix tree: <https://w3id.org/tree#>.
@prefix lsdn: <https://ldes-server.net/> .
@prefix dct:  <http://purl.org/dc/terms/> .

<{collectionName}/{viewName}> a tree:Node ;
  tree:fragmentationStrategy ([
    a lsdn:TimeFragmentation; 
    tree:path dct:created
  ]) .
```

This will define a new hierarchically structured time-based view named `viewName` for the given collection named `collectionName` that with the following defaults:
* `tree:pageSize` set to the default page size, which is `250` unless overriden (see [server settings](#ingest-settings))
* `lsdn:bucket` set to `"P1Y"^^xsd:duration, "P1M"^^xsd:duration, "P1D"^^xsd:duration, "PT1H"^^xsd:duration`, which results in a hierarchical structure with a number of node layers representing buckets of 1 year, 1 month, 1 day and, at the lowest level, 1 hour.

Therefore, the above is equivalent to:
```text
@prefix tree: <https://w3id.org/tree#>.
@prefix lsdn: <https://ldes-server.net/> .
@prefix dct:  <http://purl.org/dc/terms/> .
@prefix xsd:  <http://www.w3.org/2001/XMLSchema#> .

<{collectionName}/{viewName}> a tree:Node ;
  tree:pageSize 250;
  tree:fragmentationStrategy ([
    a lsdn:TimeFragmentation; 
    tree:path dct:created;
    lsdn:bucket "P1Y"^^xsd:duration, "P1M"^^xsd:duration, "P1D"^^xsd:duration, "PT1H"^^xsd:duration
  ]) .
```

The order of the bucket definitions does not matter because the system will sort the resulting buckets anyhow. The duration units are limited to year (Y), month (M), day (D), hour (H), minute (M) and second(S). Weeks (W) is not allowed, nor are complex periods (e.g. P1Y6M). You can however use any positive number for the period value (e.g. P5Y, PT15M, etc.).

When applying the fragmentation on the members, the system will create buckets as defined by the `lsdn:bucket` periods to contain the members based on the timestamp values that result from applying the given `tree:path` on a member. For year periods, the system creates buckets relative to the year 0. For all other periods, the system creates buckets relative to the larger unit. E.g if you specify `P3M` the system will create up to four equally-sized buckets per year found in the timestamp values. However, if you specify a period for which the larger unit cannot be divided by the period value (e.g. `P7M`) the system will create asymmetrically sized buckets (e.g. a 7 month and a 5 month bucket if `P7M` is given). This allows to create virtually any time-based hierarchical structure needed. E.g. in you need weeks, you can define a period as `P7D`, which results in buckets (per month): day 1 - 7, day 8 .. 14, day 15 .. 21 and day 22 .. last day of the month.

> [!TIP]
> Instead of the predicate path used in the examples above, you can also specify a sequence path to use a property of an inner object of the entity, e.g.:
> ```text
> @prefix tree: <https://w3id.org/tree#>.
> @prefix lsdn: <https://ldes-server.net/> .
> @prefix time: <http://www.w3.org/2006/time#> .
> @prefix ex:   <http://example.org/> .
> 
> <{collectionName}/{viewName}> a tree:Node ;
>   tree:fragmentationStrategy ([
>     a lsdn:TimeFragmentation; 
>     tree:path (ex:validity time:hasBeginning time:inXSDDateTimeStamp)
>   ]) .
> ```

### Dynamic View Definition
After creating a view definition and optionally validating it offline with the view SHACL, you can upload the definition to the server. The same triple-based RDF serializations are supported in a similar way as for a [collection definition](#upload-collection-definition).

> [!NOTE]
> To upload a view definition, you need to provide the content type and the actual content to the server using the `/admin/api/v1/collection/{collectionName}/view` endpoint with a POST verb, e.g.:
> ```bash
> curl -X POST http://localhost:8080/admin/api/v1/collection/my-collection/view -H "content-type: text/turtle" -d "@my-collection.by-page.ttl"
> ```

### Static View Definition
Similar to the [Static Collection Definition](#static-collection-definition), you can define (additional) views by providing a view definition as a Turtle file (.ttl) or a TriG file (.trig) - without named graphs - in the definitions directory (see [Core Settings](#core-settings)).

## Ingest Data
After creating a collection you can immediately start ingesting data by sending a message containing one or more entities or version objects to the server. How much and which data is contained in a message depends on how you defined the collection.

> [!WARNING]
> The ingest will fail in some scenarios if the input does not the match the collection definition, while in other cases it may even ingest incorrectly. Therefore, it is your responsibility to match the messages with the collection definition and to validate the ingest outcome.

An ingest message can be provided in various [RDF formats](#supported-rdf-formats), both triple and quad formats.

> [!NOTE]
> To ingest data into a collection, you can use the `/data/{collectionName}` endpoint with a POST verb, e.g.:
> ```bash
> curl -X POST http://localhost:8080/data/my-collection -H "content-type: text/turtle" -d "@my-member.ttl"
> ```

> [!WARNING]
> Although the server allows to ingest quad formats, it only uses the named graphs for splitting into entities. It does not store the quads found in the original message, but instead extracts and stores the entity triples per member.

### Duplicate Member Handling
As per definition, a collection member must have a unique identifier. If a member with the same identifier is being ingested, the server will simply ignore that duplicate member. If such a duplicate member is being ingested as part of a multi-member message, the other members will be normally processed and the message will be (partially) accepted.

### Performance Considerations
Please note that the message splitting part is typically the most compute intensive with major differences in performance for the various algorithms. Obviously, the algorithm that assumes a single entity per message (`ingest:SplitMessageAsSingleEntity`) is the fastest as it does not need to split the message. The algorithm that assumes members are in named graphs (`ingest:SplitMessageByNamedGraph`) is also fast as the triples are already grouped per graph by the specific RDF-serialization parser. The other two algorithms that assume one member per named node (both `ingest:SplitMessageByNamedNode` and `ingest:SplitMessageByPredicateAndObject`) need to collect the quads using a recursive algorithm, which collects the triples for that named node as well as any blank node triples referenced directly or indirectly (Concise Bounded Descriptions - [CBD](https://www.w3.org/submissions/CBD/)).

The other algorithms in the ingest process are less likely to have a huge impact on performance but of course the algorithms that assign a value are faster than those that need to search a value in the entity triples. 

> [!TIP]
> To ensure an optimal ingest throughput you need to tune your ingest message format and matching collection definition to use the fastest ingest algorithms possible for your use case.

## Retrieve Data
Please see the [LDES-Server.net User Guide](./user-guide.md) for information on how to retrieve a collection and its members.

## Manage Collections & Views
In order to manage collections and views, the administration API (`/admin/api/v1`) contains several endpoints.

Currently, you can retrieve all collection definitions and retrieve or delete a specific collection definition. In addition, you can retrieve all view definitions for a collection, and retrieve or delete a collection view.

### Get All Collections
To retrieve all collection definitions, you can use the `/admin/api/v1/collection` endpoint, e.g.:
```bash
curl http://localhost:8080/admin/api/v1/collection
```

### Retrieve Collection
To inspect a collection definition, you can use the `/admin/api/v1/collection/{collectionName}` endpoint, e.g.:
```bash
curl http://localhost:8080/admin/api/v1/collection/my-collection
```

### Delete Collection
To delete a collection definition, you can use the `/admin/api/v1/collection/{collectionName}` endpoint with a DELETE verb, e.g.:
```bash
curl -X DELETE http://localhost:8080/admin/api/v1/collection/my-collection
```

> [!CAUTION]
> When deleting a collection, all its views and all related ingested data will also be deleted. Use with caution!

### Get All Collection Views
To retrieve all view definitions for a collection, you can use the `/admin/api/v1/collection/{collectionName}/view` endpoint, e.g.:
```bash
curl http://localhost:8080/admin/api/v1/collection/my-collection/view
```

### Retrieve Collection View
To retrieve a specific view definition for a collection, you can use the `/admin/api/v1/collection/{collectionName}/view/{viewName}` endpoint, e.g.:
```bash
curl http://localhost:8080/admin/api/v1/collection/my-collection/view/my-view
```

### Delete Collection View
To delete a view definition for a view, you can use the `/admin/api/v1/collection/{collectionName}/view/{viewName}` endpoint with a DELETE verb, e.g.:
```bash
curl -X DELETE http://localhost:8080/admin/api/v1/collection/my-collection/view/my-view
```

> [!NOTE]
> When deleting a view, all pages which resulted from the view fragmentation are deleted as well. However, no data members are deleted.
