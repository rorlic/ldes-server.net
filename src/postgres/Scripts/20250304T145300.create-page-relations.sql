-- create page_relations table
CREATE TABLE page_relations
(
    fid   bigint   NOT NULL,
    tid   bigint   NOT NULL,
    vid   smallint NOT NULL,
    type  varchar(255) NULL,
    path  varchar(255) NULL,
    value varchar(255) NULL
);
COMMENT
ON COLUMN page_relations.fid is 'from-id, i.e. the relating node (contains the relation)';
COMMENT
ON COLUMN page_relations.tid is 'to-id, i.e. the related node';
COMMENT
ON COLUMN page_relations.vid is 'view-id, for delete performance';
COMMENT
ON COLUMN page_relations.type is 'type of relation, e.g. tree:GreaterThanRelation, or NULL if the default base tree:Relation';
COMMENT
ON COLUMN page_relations.path is 'relation path, i.e. the SHACL property path indicating the predicate value to compare with the reference value';
COMMENT
ON COLUMN page_relations.value is 'relation value, i.e. the reference value to compare the predicate value to';
ALTER TABLE "page_relations"
    ADD FOREIGN KEY ("fid") REFERENCES "pages" ("pid");
ALTER TABLE "page_relations"
    ADD FOREIGN KEY ("tid") REFERENCES "pages" ("pid");
ALTER TABLE "page_relations"
    ADD FOREIGN KEY ("vid") REFERENCES "views" ("vid");
CREATE INDEX "idx_page_relations_fid" ON page_relations (fid);
CREATE INDEX "idx_page_relations_tid" ON page_relations (tid);
CREATE INDEX "idx_page_relations_vid" ON page_relations (vid);
