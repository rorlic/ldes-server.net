-- create bucket_members table
CREATE TABLE bucket_members
(
    bid bigint   NOT NULL,
    mid bigint   NOT NULL,
    vid smallint NOT NULL
);
COMMENT
ON TABLE bucket_members is 'member to bucket association';
COMMENT
ON COLUMN bucket_members.bid is 'bucket-id of the bucket where the member is bucketized into';
COMMENT
ON COLUMN bucket_members.mid is 'member-id';
COMMENT
ON COLUMN bucket_members.vid is 'view-id, for delete performance';
ALTER TABLE "bucket_members"
    ADD FOREIGN KEY ("bid") REFERENCES "buckets" ("bid");
ALTER TABLE "bucket_members"
    ADD FOREIGN KEY ("mid") REFERENCES "members" ("mid");
ALTER TABLE "bucket_members"
    ADD FOREIGN KEY ("vid") REFERENCES "views" ("vid");
CREATE UNIQUE INDEX "idx_bucket_members_bid_mid" ON bucket_members (bid, mid);
CREATE INDEX "idx_bucket_members_vid_mid" ON bucket_members (vid, mid);
