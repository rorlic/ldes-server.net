-- create buckets table
CREATE TABLE buckets
(
    bid     bigint GENERATED ALWAYS AS IDENTITY,
    vid     smallint NOT NULL,
    key     TEXT NULL,
    lastMid bigint   NOT NULL default 0
);
COMMENT
ON COLUMN buckets.vid is 'view-id, for delete performance';
COMMENT
ON COLUMN buckets.key is 'per-view unique bucket key (i.e. the grouping key: all members with the same bucket key are available in a forward linked list of pages)';
COMMENT
ON COLUMN buckets.lastMid is 'the last, per-bucket paginated member id, always increasing';
ALTER TABLE "buckets"
    ADD PRIMARY KEY ("bid");
ALTER TABLE "buckets"
    ADD FOREIGN KEY ("vid") REFERENCES "views" ("vid");
CREATE INDEX "idx_buckets_vid" ON buckets (vid);
CREATE UNIQUE INDEX "idx_buckets_vid_key" ON buckets (vid, key);

