namespace AquilaSolutions.LdesServer.Storage.Postgres.Queries;

public static class Sql
{
    public static class Collection
    {
        public const string Create =
            "insert into collections(name, definition) values(@Name,@Definition) returning cid";

        public const string DeleteById =
            "delete from collections where cid = @Cid";

        public const string GetByName =
            "select cid, name, definition from collections where name = @Name";

        public const string GetAll =
            "select cid, name from collections";

        public const string GetAllDefinitions =
            "select cid, name, definition from collections";

        public const string DeleteAll =
            "TRUNCATE TABLE page_relations, page_members, pages," +
            " bucket_members, buckets," +
            " bucketization_stats, views," +
            " members, collections CASCADE";

        public const string UpdateIngestedMemberCount =
            "update collection_stats set ingested = ingested + @Affected where cid = @Cid";

    }

    public static class View
    {
        public const string Create =
            "insert into views(cid, name, definition) values (@Cid, @Name, @Definition) returning vid";

        public const string GetByCollectionId =
            "select vid, cid, name, definition from views where cid = @Cid";

        public const string GetByCollectionIdForDeletion =
            "select vid, cid, name, definition from views where cid = @Cid for update";

        public const string GetByCollectionIdAndViewName =
            "select vid, cid, name, definition from views where cid = @Cid and name = @Name";

        public const string DeleteById =
            "delete from views where vid = @Vid";

        public const string DeleteBucketizationStatsByView =
            "delete from bucketization_stats where vid = @Vid";

        public const string GetReadyForBucketization =
            "select bs.vid, bs.lastTxn, bs.total from bucketization_stats bs "+
            "inner join views v on v.vid = bs.vid " +
            "where bs.lastTxn < (select max(txn) from member_txn_ranges t where t.cid = v.cid) " +
            "order by bs.lastTxn " +
            "for no key update of bs skip locked limit 1";

        public const string GetStatsForDeletion =
            "select vid, lastTxn, total from bucketization_stats where vid = @Vid for update";

        public const string GetById =
            "select vid, cid, name, definition from views v where vid = @Vid";
        
        public const string UpdateLastBucketizedAndBucketizedCount =
            "update bucketization_stats set lastTxn = @LastTxn, total = total + @BucketizedCount where vid = @Vid";

    }

    public static class Member
    {
        public const string Create =
            "insert into members (cid, createdAt, memberId, entityId, entityModel) " +
            "values (@Cid, @CreatedAt, @MemberId, @EntityId, @EntityModel) " +
            "on conflict do nothing returning mid";

        public const string DeleteByCollection =
            "delete from members where cid = @Cid; " +
            "delete from member_txn_ranges where cid = @Cid";

        public const string GetHighestBatchTransactionForBucketization =
            "select highest_batch_txn(@Cid, @lastTxn, @Count)";

        public const string InsertMemberTxnRange =
            "INSERT INTO member_txn_ranges(cid, low,high) values(@Cid, @Low, @High)";

        public const string GetPageMembers =
            "select m.mid, m.createdAt, m.memberId, m.entityId, m.entityModel from members m " +
            "where m.mid in (select pm.mid from page_members pm where pm.pid = @Pid) " +
            "order by m.createdAt, m.mid";

        public const string GetMembersByTxnRange =
            "select m.mid, m.createdAt, m.memberId, m.entityId, m.entityModel from members m " +
            "where m.txn in (@TxnIds) order by m.txn, m.mid";
    }

    public static class Bucket
    {
        public const string Create =
            "insert into buckets(vid, key) values (@Vid, @Key) returning bid";

        public const string DeleteMembersByView =
            "delete from bucket_members where vid = @Vid";

        public const string DeleteByView =
            "delete from buckets where vid = @Vid";

        public const string GetReadyForPagination =
            "select b.bid, b.vid, b.lastMid from buckets b " +
            "where b.lastMid < (select max(bm.mid) from bucket_members bm where bm.bid = b.bid) " +
            "order by b.bid "+
            "for no key update of b skip locked limit 1";
        
        public const string GetByViewForDeletion =
            "select bid, vid, key from buckets where vid = @Vid for update";

        public const string GetById =
            "select bid, vid, key from buckets where bid = @Bid";

        public const string GetByViewAndDefaultKey =
            "select bid, vid, key from buckets where vid = @Vid and key is null";

        public const string GetByViewAndKey =
            "select bid, vid, key from buckets where vid = @Vid and key = @Key";

        public const string UpdateLastPaginated =
            "update buckets set lastMid = @LastMid where bid = @Bid"; 
        
        public const string CreateByBucketizableMembers =
            "insert into bucket_members(bid, vid, mid) " +
            "select @Bid, @Vid, mid from members m where m.txn in (@TxnIds) order by m.txn, m.mid";

        public const string BucketizeMember =
            "insert into bucket_members(bid, vid, mid) values (@Bid, @Vid, @Mid)";

        public const string GetMembersToPaginate =
            "select mid from bucket_members where bid = @Bid order by mid limit @Count"; 
        
        public const string RemoveBucketMembers =
            "delete from bucket_members where bid = @Bid and mid in (@Ids)";
    }

    public static class Page
    {
        public const string DeleteRelationsByView =
            "delete from page_relations where vid = @Vid";
        
        public const string DeleteMembersByView =
            "delete from page_members where vid = @Vid";
        
        public const string DeleteByView =
            "delete from pages where vid = @Vid";
        
        public const string GetRootPageByDefaultBucket =
            "select p.pid, p.bid, p.name, p.root, p.open, p.updatedAt from pages p " +
            "where p.root = true and p.bid = (select bid from buckets where vid = @Vid and key is null)";

        public const string GetRootPage =
            "select p.pid, p.bid, p.vid, p.name, p.root, p.open, p.updatedAt, p.assigned " +
            "from pages p where p.root = true and p.bid = @Bid";

        public const string GetOpenPage =
            "select p.pid, p.bid, p.vid, p.name, p.root, p.open, p.updatedAt, p.assigned " +
            "from pages p where p.open = true and p.bid = @Bid";

        public const string CreatePage =
            "insert into pages(bid,vid,name,root) values (@Bid, @Vid, @Name, @Root) returning pid";

        public const string ClosePage =
            "update pages set open = false, updatedAt = CURRENT_TIMESTAMP where pid = @Pid";

        public const string CreatePageRelation =
            "insert into page_relations(fid,tid,vid,type,path,value) values(@Fid, @Tid, @Vid, @Type, @Path, @Value); ";

        public const string CreateGenericPageRelation =
            "insert into page_relations(fid,tid,vid) values(@Fid, @Tid, @Vid); ";

        public const string UpdateViewTimestamp =
            "update pages set updatedAt = CURRENT_TIMESTAMP where pid = @Fid";

        public const string GetPageByViewAndPageNames =
            "select p.pid, p.bid, p.vid, p.name, p.root, p.open, p.updatedAt, p.assigned from pages p " +
            "where p.name = @PageName and p.vid = (select v.vid from views v where v.name = @ViewName and v.cid = @Cid)";

        public const string GetPageLinks =
            "select p.name as link, pr.type, pr.path, pr.value " +
            "from page_relations pr inner join pages p on p.pid = pr.tid where pr.fid = @Pid";
        
        public const string AssociateMembersToPage =
            "insert into page_members(pid, mid, vid) select @Pid, v.mid, @Vid from (values @Ids) as v(mid)";
        
        public const string UpdateAssignedMemberCount =
            "update pages set assigned = assigned + @Count, updatedAt = CURRENT_TIMESTAMP where pid = @Pid";
    }
}