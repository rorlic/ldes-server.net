-- create views table
CREATE TABLE views
(
    vid        smallint GENERATED ALWAYS AS IDENTITY,
    cid        smallint NOT NULL,
    name       text     NOT NULL,
    definition text NULL
);
COMMENT
ON COLUMN views.name is 'the view name (can be the empty string, i.e. the default view, aka. event source) -- all view names are unique within a collection';
COMMENT
ON COLUMN views.definition is 'view definition';
ALTER TABLE "views"
    ADD PRIMARY KEY ("vid");
ALTER TABLE "views"
    ADD FOREIGN KEY ("cid") REFERENCES "collections" ("cid");
CREATE UNIQUE INDEX "idx_views_cid_name" ON views (cid, name);

-- create bucketization statistics table
CREATE TABLE bucketization_stats
(
    vid     smallint NOT NULL,
    lastTxn bigint   NOT NULL default 0,
    total   bigint   NOT NULL default 0
);
COMMENT
ON COLUMN bucketization_stats.lastTxn is 'the last, per-view bucketized member txn id, always increasing';
COMMENT
ON COLUMN bucketization_stats.total is 'counter for number of bucketized members, always increasing';
ALTER TABLE "bucketization_stats"
    ADD FOREIGN KEY ("vid") REFERENCES "views" ("vid");

CREATE FUNCTION create_bucketization_stats()
    RETURNS TRIGGER
    LANGUAGE PLPGSQL
AS $$
BEGIN
insert into bucketization_stats(vid)
values (NEW.vid);
RETURN NULL;
END;
$$;

CREATE
OR REPLACE TRIGGER ai_views
    AFTER insert
    ON views
    FOR EACH ROW EXECUTE FUNCTION create_bucketization_stats();

