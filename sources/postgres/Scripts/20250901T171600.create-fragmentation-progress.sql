-- create fragmentation_progress view
CREATE OR REPLACE VIEW _fragmentation_progress AS
SELECT cs.cid, v.vid, cs.ingested, vs.bucketized, vs.paginated,
       to_char((((vs.bucketized)::double precision / (GREATEST(cs.ingested, (1)::bigint))::double precision) * (100)::double precision), '990.99'::text) AS "% bucketized",
       to_char((((vs.paginated)::double precision / (GREATEST(cs.ingested, (1)::bigint))::double precision) * (100)::double precision), '990.99'::text) AS "% paginated"
FROM ((collection_stats cs
    JOIN views v ON ((v.cid = cs.cid)))
    JOIN view_stats vs ON ((vs.vid = v.vid)))
ORDER BY cs.cid, v.vid
;