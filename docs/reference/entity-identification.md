# Entity Identification

After splitting into entities, each one is handled on its own. First, the entity needs to be identified. This allows you to group all versions of the same entity. The *entity identification* allows for the following algorithms:

* **`ingest:IdentifyEntityByEntityType`**: The entity's identity is given by the (single!) subject of the given (`ingest:o`) type (`rdf:type`)
  
  **Use when:** Entities are identified by their type
  
  **Example:** Find the subject that has `rdf:type ex:Person`

* **`ingest:IdentifyEntityByPredicateAndObject`**: The entity's identity is given by the (single!) subject found when looking for a predicate (`ingest:p`) & object (`ingest:o`) combination
  
  **Use when:** Entities have a specific marker property
  
  **Example:** Find the subject that has `ex:isEntity "true"`

* **`ingest:IdentifyEntityBySingleNamedNode`**: The entity's identity is given by the (single!) named-node subject
  
  **Use when:** Each entity has exactly one named node (most common case)
