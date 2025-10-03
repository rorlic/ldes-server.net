-- create view statistics table
CREATE OR REPLACE VIEW view_stats AS
select bs.vid, 
       bs.total as bucketized,
       bs.total - (select count(distinct bm.mid) from bucket_members bm where bm.vid = bs.vid) as paginated
from bucketization_stats bs
