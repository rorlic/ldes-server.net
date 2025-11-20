# Member Creation

The final step is to actually create the member of the collection, which is a structured object consisting of the entity identifier (for grouping members), the entity version (for sorting members within a group), the member identifier, the member ingest time and of course the member content. This content is the collection of triples as found by the message splitting algorithm. However, in the case of ingesting version objects, these triples need to be converted back to a state object (materialized) to allow for consistent handling when retrieving the members of the collection. Therefore, for the *member creation* the LDES server allows for two options:

* **`ingest:CreateMemberAsIs`**: Use the entity content as-is (do not change it).
  
  **Use when:** Ingesting state objects (most common case)

* **`ingest:CreateMemberWithEntityMaterialization`**: Materialize the entity version object to its original state object, using the given predicate (`ingest:p`) defining the original entity identifier.
  
  **Use when:** Ingesting version objects that need to be converted back to state objects
