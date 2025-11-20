# Member Identification

When both the entity identifier and entity version are found or assigned, the member can be identified. For the *member identification* there are two options:

* **`ingest:IdentifyMemberByEntityIdAndVersion`**: Concatenate the entity identifier and version, separating them by an optional separator (`ingest:separator`), defaults to `/`.
  
  **Use when:** Ingesting state objects (most common case)
  
  **Example:** Entity `<person1>` with version `2024-01-15T10:30:00Z` becomes `<person1/2024-01-15T10:30:00Z>`

* **`ingest:IdentifyMemberWithEntityId`**: Use the entity identifier as the member identifier.
  
  **Use when:** Ingesting (existing) version objects instead of state objects

