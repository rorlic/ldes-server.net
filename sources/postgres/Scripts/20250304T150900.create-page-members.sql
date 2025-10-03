-- create page_members table
CREATE TABLE page_members
(
    pid bigint   NOT NULL,
    mid bigint   NOT NULL,
    vid smallint NOT NULL
);
COMMENT
ON TABLE page_members is 'member to page association';
COMMENT
ON COLUMN page_members.pid is 'page-id of the page the member is part of';
COMMENT
ON COLUMN page_members.mid is 'member-id';
COMMENT
ON COLUMN page_members.vid is 'view-id, for delete performance';
ALTER TABLE "page_members"
    ADD FOREIGN KEY ("mid") REFERENCES "members" ("mid");
ALTER TABLE "page_members"
    ADD FOREIGN KEY ("pid") REFERENCES "pages" ("pid");
ALTER TABLE "page_members"
    ADD FOREIGN KEY ("vid") REFERENCES "views" ("vid");
CREATE UNIQUE INDEX "idx_page_members_pid_mid" ON page_members (pid, mid);
CREATE INDEX "idx_page_members_vid" ON page_members (vid);
--CREATE INDEX "idx_page_members_pid" ON page_members (pid);
