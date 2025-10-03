# LDES Server for .NET
> TODO: 
> * describe project
> * cleanup this doc
> * refer to [administration guide](./docs/admin-guide.md) and [user guide](./docs/user-guide.md)

## Ingesting Data
A data publisher can ingest multiple entities in a single message. Such a message defines a single logical transaction. Messages can possibly contain:
* a single entity or multiple entities
* a single graph or multiple graphs each containing one or more entities

When receiving a message, the mime type can be a linked data one or a non-linked data. If the mime type is JSON, a JSON-LD context should be attached to the message. If the mime type is a linked data one, the message should be parsed as linked data.

After parsing the linked data, we need to determine if the message contains multiple entities and if so, split the message in these entities.

After member splitting, we need to find the entity identifier (version of) and the entity version indicator (timestamp or number). We need this data to create the member identifier as well as for member retention and cleanup.

Once we know the basic entity properties, we need to store the member and this metadata in some (relational, document or graph) storage. The metadata can be used to sort and group related entity versions.

### Splitting a Message
There are various ways to split a message into its entities:
* consider the message as one entity
* split the message by named node and validate that all triples are used and not shared
* split the message by predicate and validate all triples are used and not shared
* split by named graph and validate no triples exist in the default graph (i.e. all triples are used)

### Identifying an Entity
We can use different ways to identify the entity (aggregate root):
* validate that entity contains one named node and use the subject as identifier
* validate that a search by predicate + object contains one named node and use the subject as identifier

### Determine Entity Version
We have the following ways to determine the entity version identifier:
* given the entity identifier and a configured predicate, use the integer or timestamp object value (or string value that can be interpreted as an ISO8601 date time) for the version identifier
* use the ingestion timestamp as a version identifier

### Creating a Member Identifier
In order to create a unique member identifier, we need to identify the entity as indicated above and create a new URI by appending a separator and the version indicator.

### Storing a Member
We store the member data as triples as well as the entity identifier and version indicator (as number).

## Bucketizing Members
Bucketizing members is the process of categorizing each member is one or more buckets (categories) by calculating one or more string values for some member property and adding the member to the virtual bucket based on each string (bucket key). This effectively partitions a member collection into subsets.

LDES views basically offer different ways of partitioning a member collection. There is however one simple view that can be applied to a member collection without configuration, i.e. a simple paged view meaning all members are contained in the same (unnamed) bucket.

## Paginating Members
The buckets that come into existence by the bucketization process basically hold a subset of the member collection. As this subset can be to big for a http response, it needs to be split into pages each holding up to a configured maximum number of members. Therefore, paginating members boils down to assigning each member to a page until that page is full (contains the configured maximum amount or members). Once a page is full, a new empty page is created and a relation (link) to this new page is added. Now the page is complete and marked as closed (immutable).

## Fetching Fragments
The ideal way to serve fragments is by wrapping an entity in a named graph with its URI set to the member identifier, ie. the member version is an object identified by this generated identifier and contains the ldes:isVersionOfPath and ldes:timestampPath predicates triples.

