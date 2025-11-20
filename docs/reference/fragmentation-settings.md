# Fragmentation Settings

There are a number of settings related to fragmentation that you can change if needed:

| Setting | Type | Description | Default | Remarks |
|---------|------|-------------|---------|---------|
| `LoopDelay` | integer | Number of milliseconds to wait between runs | bucketization: 5000<br>pagination: 7000 | Set to `null` for one-time processing (useful for batch jobs). Lower values increase responsiveness but use more CPU. |
| `MemberBatchSize` | integer | Maximum members processed per run | 5000 | Higher values improve throughput but increase memory usage and processing time per run. |
| `DefaultPageSize` | integer | Maximum members per page | 250 | Applies to the default view (`_`) and views without explicit page size. Smaller pages reduce response size; larger pages reduce total page count. |

You can specify these values in a custom configuration file (or use environment variables) to change the defaults:
```json
{
  "LdesServer": {
    "Bucketization": {
      "LoopDelay": 5000,
      "MemberBatchSize": 5000
    },
    "Pagination": {
      "LoopDelay": 7000,
      "MemberBatchSize": 5000,
      "DefaultPageSize": 250
    }
  }
}
```
