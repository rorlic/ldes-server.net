-- create pages table
CREATE TABLE pages
(
    pid       bigint GENERATED ALWAYS AS IDENTITY,
    bid       bigint                   NOT NULL,
    vid       smallint                 NOT NULL,
    name      text                     NOT NULL,
    root      boolean                  NOT NULL DEFAULT false,
    open      boolean                  NOT NULL DEFAULT true,
    updatedAt timestamp WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned  smallint                 NOT NULL DEFAULT 0
);
COMMENT
ON COLUMN pages.vid is 'view-id, for delete performance';
COMMENT
ON COLUMN pages.name is 'page name, i.e. a guid';
COMMENT
ON COLUMN pages.root is 'true if the page is the root page for the bucket, false otherwise';
COMMENT
ON COLUMN pages.open is 'true if the page has not yet reached its member count capacity (i.e. not immutable), false otherwise -- note that a page stays closed, even if retention removes members';
ALTER TABLE "pages"
    ADD PRIMARY KEY ("pid");
ALTER TABLE "pages"
    ADD FOREIGN KEY ("bid") REFERENCES "buckets" ("bid");
ALTER TABLE "pages"
    ADD FOREIGN KEY ("vid") REFERENCES "views" ("vid");
CREATE UNIQUE INDEX "idx_pages_vid_name" ON pages (vid, name);
CREATE INDEX "idx_pages_bid" ON pages (bid);
-- CREATE UNIQUE INDEX "idx_pages_bid_root" ON pages (bid) WHERE root is true; -- only one page can be the root page per bucket
-- CREATE UNIQUE INDEX "idx_pages_bid_open" ON pages (bid) WHERE open is true; -- only one page can be open per bucket
CREATE INDEX "idx_pages_vid" ON pages (vid);
