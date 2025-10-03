-- create members table
CREATE TABLE members
(
    mid           bigint GENERATED ALWAYS AS IDENTITY,
    txn           bigint NULL,
    cid           smallint                 NOT NULL,
    createdAt     timestamp WITH TIME ZONE NOT NULL,
    memberId      text                     NOT NULL,
    entityId      text                     NOT NULL,
    entityModel   bytea                    NOT NULL
);
COMMENT
ON COLUMN members.txn is 'assigned postgres transaction ID (bigint because system column xmin can wrap around)';
COMMENT
ON COLUMN members.createdAt is 'assigned timestamp of member creation, aka. value of property LDES timestamp_path';
COMMENT
ON COLUMN members.memberId is 'unique URI logically identifying a member';
COMMENT
ON COLUMN members.entityId is 'URI identifying an entity (allows grouping all members, i.e. entity versions), aka. value of property LDES version_of_path';
COMMENT
ON COLUMN members.entityModel is 'RDF (triples) representing the entity (GZipped Turtle)';
ALTER TABLE "members" ADD PRIMARY KEY ("mid");
ALTER TABLE "members" ADD FOREIGN KEY ("cid") REFERENCES "collections" ("cid");
CREATE INDEX "idx_members_txn" ON members (txn);
CREATE UNIQUE INDEX "idx_members_cid_memberId" ON members (cid,memberId);
--TODO: find alternative for preventing same member ID override
CREATE INDEX "idx_members_createdAt" ON members (createdAt);

CREATE FUNCTION set_current_txn_id()
    RETURNS TRIGGER
    LANGUAGE PLPGSQL
AS $$
BEGIN
    NEW.txn := pg_current_xact_id()::text::bigint; RETURN NEW;
END;
$$;

CREATE TRIGGER bi_members
    BEFORE insert
    ON members
    FOR EACH ROW EXECUTE FUNCTION set_current_txn_id();

create table member_txn_ranges
(
    txn  bigint   NULL,
    cid  smallint NOT NULL,
    low  bigint   NOT NULL,
    high bigint   NOT NULL
);
COMMENT
ON COLUMN member_txn_ranges.txn is 'postgres transaction ID';
COMMENT
ON COLUMN member_txn_ranges.low is 'min(mid) of txn';
COMMENT
ON COLUMN member_txn_ranges.high is 'max(mid) of txn';
ALTER TABLE "member_txn_ranges" ADD PRIMARY KEY ("txn");
ALTER TABLE "member_txn_ranges" ADD FOREIGN KEY ("cid") REFERENCES "collections" ("cid");
CREATE INDEX "idx_member_txn_ranges_cid_txn" ON member_txn_ranges (cid, txn);

CREATE TRIGGER bi_member_txn_ranges
    BEFORE insert
    ON member_txn_ranges
    FOR EACH ROW EXECUTE FUNCTION set_current_txn_id();

CREATE OR REPLACE FUNCTION highest_batch_txn(IN collectionId smallint, IN previousTxn bigint, IN maxCount smallint)
    RETURNS SETOF bigint AS
$$
DECLARE
    snapshot pg_snapshot;

    lastLow bigint;
    lastTxn bigint;
    safeTxn bigint;
           
    txnCursor refcursor;
    highestTxn bigint := NULL;
    currentCount smallint = 0;
    memberCount smallint;
BEGIN
    -- find latest txn
    select t.txn, t.low
    into lastTxn, lastLow
    from member_txn_ranges t 
    where t.cid = collectionId
    order by t.txn desc
    limit 1;

    snapshot := pg_current_snapshot();
    IF pg_snapshot_xmin(snapshot) < pg_snapshot_xmax(snapshot) THEN
        -- search for highest safe txn based on its high bound being larger than latest low bound
        select t.txn
        into safeTxn
        from member_txn_ranges t
        where t.cid = collectionId and t.txn < lastTxn and t.high < lastLow
        order by t.txn desc
        limit 1;
    ELSE
        safeTxn := lastTxn;
    END IF;

    -- calculate aggregated member count from previousTxn up to maxTxn or maxCount members whichever is lower
    OPEN txnCursor NO SCROLL FOR 
        select t.txn 
        from member_txn_ranges t
        where t.cid = collectionId and t.txn > previousTxn and t.txn <= safeTxn
        order by t.txn;
    LOOP
        FETCH txnCursor INTO highestTxn;
        if highestTxn is null then exit; end if;
        return next highestTxn;
        
        select count(m.mid) into memberCount from members m where m.txn = highestTxn;
        currentCount := currentCount + memberCount;
        if currentCount >= maxCount then exit; end if;
    END LOOP;
    CLOSE txnCursor;
END;
$$ LANGUAGE PLPGSQL;

COMMENT 
ON FUNCTION highest_batch_txn is 'stored procedure to determine the highest safe transaction, which is not yet processed and ready for bucketization for the given collection and limited to the given number of members';
