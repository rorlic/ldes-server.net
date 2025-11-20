# Ingestion
The ingest process consists of five key steps:
* split the received linked-data message into individual entities (state or version things)
* search for each entity's identifier
* find the entity's version identifier (or create one if missing)
* create the member identifier (or use one for version things)
* create the member (including transforming a version thing back to a state thing)

For each of these ingest steps the LDES server allows a number of options (algorithms). This allows you to handle virtually any situation:
* ingest one or more existing version things
* ingest one or more state things
* ingest things with or without a timestamp
* etc.

In fact, the LDES server allows you to configure each collection to use a different set of these algorithms. But, do not worry: for each step there is a default so you do not need to specify all 5 options for a newly created collection.
