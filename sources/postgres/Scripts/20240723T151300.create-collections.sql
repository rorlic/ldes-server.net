-- create collections table
CREATE TABLE collections
(
    cid        smallint GENERATED ALWAYS AS IDENTITY,
    name       VARCHAR(255) NOT NULL UNIQUE,
    definition TEXT NULL
);
COMMENT
ON COLUMN collections.name is 'collection name';
COMMENT
ON COLUMN collections.definition is 'collection definition';
ALTER TABLE "collections"
    ADD PRIMARY KEY ("cid");
CREATE INDEX "idx_collections_name" ON collections (name);

-- create collection statistics table
CREATE TABLE collection_stats
(
    cid      smallint NOT NULL,
    ingested bigint   NOT NULL default 0
);
COMMENT
ON COLUMN collection_stats.ingested is 'counter for number of ingested members, always increasing';
ALTER TABLE "collection_stats"
    ADD PRIMARY KEY ("cid");
ALTER TABLE "collection_stats"
    ADD FOREIGN KEY ("cid") REFERENCES "collections" ("cid") ON DELETE CASCADE;

CREATE FUNCTION create_collection_stats()
    RETURNS TRIGGER
    LANGUAGE PLPGSQL
AS $$
BEGIN
    insert into collection_stats(cid) values(NEW.cid);
    RETURN NULL;
END;
$$;

CREATE TRIGGER ai_collections
    AFTER insert
    ON collections
    FOR EACH ROW EXECUTE FUNCTION create_collection_stats();
