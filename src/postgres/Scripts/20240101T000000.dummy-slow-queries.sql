-- check availability:
SELECT * FROM pg_available_extensions WHERE name = 'pg_stat_statements';

-- add to /var/lib/postgresql/data/postgresql.conf: 
shared_preload_libraries = 'pg_stat_statements' # (change requires restart)

-- create extension in public schema:
CREATE EXTENSION pg_stat_statements;

-- add slow query view:
create view _slowest_queries as
SELECT round((total_exec_time)::numeric, 2) AS total_time,
       calls,
       round((min_exec_time)::numeric, 2) AS min_time,
       round((max_exec_time)::numeric, 2) AS max_time,
       round((mean_exec_time)::numeric, 2) AS avg_time,
       query
FROM pg_stat_statements
ORDER BY total_exec_time DESC;

-- reset stats
select pg_stat_statements_reset();



--explain
select mid -- count(*)
from members m
where m.mid <= (select max(pm.mid) from page_members pm) and
    not exists (select pm.mid from page_members pm where pm.mid = m.mid)
order by m.mid asc
    limit 10


select count(*) from members m where m.mid <= (select max(pm.mid) from page_members pm) and not exists (select pm.mid from page_members pm where pm.mid = m.mid);
