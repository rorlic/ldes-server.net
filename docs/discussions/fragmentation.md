# Fragmentation

To offer a collection as a TREE structure with pages containing a limited number of collection members, the LDES server automatically assigns members to one or more pages. This fragmentation (per view) is a two-step process consisting of calculating one or more virtual buckets based on the member's content (_bucketization_) and then putting the member in one page per bucket (_pagination_). Both processes monitor any newly ingested or bucketized members, and bucketize respectively paginate them.

Both the bucketization and the pagination processes run as interruptible processes in an infinite loop.

The bucketization and pagination processes run continuously:
1. Check for pending work
2. Process available work (up to configured limits)
3. Wait for the configured delay period
4. Repeat

Each run processes as much work as possible within the configured batch size limits.
