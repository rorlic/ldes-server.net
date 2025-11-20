# Message Splitting

When a message is received by the LDES server it may contain multiple entities. In order to ingest an individual entity, the LDES server needs to know how to split the message into the contained entities. For this *message splitting* you can choose one of the following options:

* **`ingest:SplitMessageAsSingleEntity`**: Assume the message only contains one entity
  
  **Use when:** Ingesting one member per request
  
  **Performance:** Fastest option

* **`ingest:SplitMessageByNamedGraph`**: Assume the message contains one entity in each named graph (the default graph should _not_ exist)
  
  **Use when:** Your data format naturally groups members in named graphs
  
  **Performance:** Very fast (graphs are already separated by the parser)
  
  **Example:** If your message contains:
  ```trig
  <urn:graph:1> {
    <person1> a foaf:Person ; foaf:name "Alice" .
  }
  <urn:graph:2> {
    <person2> a foaf:Person ; foaf:name "Bob" .
  }
  ```
  This creates two separate members (person1 and person2).

* **`ingest:SplitMessageByNamedNode`**: Assume each named node (and its referenced blank nodes) is a separate entity
  
  **Use when:** Multiple members in a single default graph or named graph
  
  **Performance:** Slower (requires recursive blank node collection using [CBD](https://www.w3.org/submissions/CBD/))
  
  **Example:** If your message contains:
  ```turtle
  <person1> a foaf:Person ; foaf:name "Alice" .
  <person2> a foaf:Person ; foaf:name "Bob" .
  ```
  This creates two separate members (person1 and person2).

* **`ingest:SplitMessageByPredicateAndObject`**: Assume each named node found by querying for a predicate (`ingest:p`) & object (`ingest:o`) combination (and its referenced blank nodes) is a separate entity
  
  **Use when:** You need to filter entities by type or other criteria
  
  **Performance:** Slower (requires query + recursive blank node collection)
  
  **Example configuration:**
  ```turtle
  ingest:splitMessage [ 
    a ingest:SplitMessageByPredicateAndObject ;
    ingest:p rdf:type ;
    ingest:o foaf:Person
  ]
  ```
