# Version Identification

After identifying the entity itself, the LDES server needs to know which version of the entity is being ingested. This allows you to sort versions of the same entity. For *version identification* you can choose one of the following:

* **`ingest:IdentifyVersionBySubjectAndPredicatePath`**: The entity version is found by using the entity identifier as subject and the given predicate path (`ingest:p`). The found object value should be an `xsd:dateTime` (or a string that can be interpreted as a date/time value).
  
  **Use when:** Your entities have a timestamp property
  
  **Example:** Extract timestamp from `dct:created` property

* **`ingest:IdentifyVersionBySubjectAndSequencePath`**: The entity version is found by using the entity identifier as subject and the given sequence path (`ingest:p`). The found object value should be an `xsd:dateTime` (or a string that can be interpreted as a date/time value).
  
  **Use when:** The timestamp is nested in your entity structure
  
  **Example:** Follow path `(ex:metadata ex:timestamp)` to extract timestamp

* **`ingest:IdentifyVersionWithIngestTimestamp`**: The entity version is assigned to the timestamp of ingestion. All entities from the message receive the same timestamp value.
  
  **Use when:** Your entities don't have timestamps, or you want to use server-side timestamps
