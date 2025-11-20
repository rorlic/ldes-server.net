### Collection Definition

A *minimal collection definition* is similar to:
```turtle
@prefix ldes: <https://w3id.org/ldes#> .

<{collectionName}> a ldes:EventStream .
```

**Default configuration when using minimal definition:**

| Setting | Default Value | Description |
|---------|---------------|-------------|
| `ldes:timestampPath` | `prov:generatedAtTime` | Path to member timestamp |
| `ldes:versionOfPath` | `dct:isVersionOf` | Path to version relationship |
| `ingest:splitMessage` | `SplitMessageByNamedNode` | Split strategy |
| `ingest:identifyEntity` | `IdentifyEntityBySingleNamedNode` | Entity identification |
| `ingest:identifyVersion` | `IdentifyVersionWithIngestTimestamp` | Version identification |
| `ingest:identifyMember` | `IdentifyMemberByEntityIdAndVersion` | Member ID generation (separator: `/`) |
| `ingest:createMember` | `CreateMemberAsIs` | Member creation strategy |

This is equivalent to the full explicit definition:
```turtle
@prefix ldes: <https://w3id.org/ldes#> .
@prefix dct:  <http://purl.org/dc/terms/> .
@prefix prov: <http://www.w3.org/ns/prov#> .

@prefix lsdn:   <https://ldes-server/ns/> .
@prefix ingest: <https://ldes-server/ns/ingest#> .

<{collectionName}> a ldes:EventStream ; 
  ldes:timestampPath prov:generatedAtTime ; 
  ldes:versionOfPath dct:isVersionOf ;
  lsdn:ingestion [
    ingest:splitMessage [ a ingest:SplitMessageByNamedNode ];
    ingest:identifyEntity [ a ingest:IdentifyEntityBySingleNamedNode ];
    ingest:identifyVersion [ a ingest:IdentifyVersionWithIngestTimestamp ];
    ingest:identifyMember [ a ingest:IdentifyMemberByEntityIdAndVersion ; ingest:separator "/" ] ;
    ingest:createMember [ a ingest:CreateMemberAsIs ]
  ] .
```
For configuring the steps in the ingestion process, see [Definitions](./_index.md#definitions).

> [!TIP]
> You can specify collection definitions in any of the supported RDF serialization formats (see supported [RDF Formats](./rdf-formats.md)). However, the system will always store the collection and view definitions as Turtle.
