# Logging Settings

The LDES server uses ASP.NET Core's standard logging mechanism, see [configuration discussion](../discussions/configuration.md).  
Enable detailed logging by setting log levels for specific namespaces:

* `LdesServer`: Default value: `Information`. This applies to the server and to all sub-systems not explicitly defining logging.
* `LdesServer.Ingestion`: Member ingestion process details.
* `LdesServer.Bucketization`: Bucket assignment and creation.
* `LdesServer.Pagination`: Page generation and management.
* `Npgsql.Command`: Database query execution (useful for performance tuning).

Example configuration:
```json
{
  "Logging": {
    "LogLevel": {
      "LdesServer": "Information",
      "LdesServer.Ingestion": "Debug",
      "LdesServer.Bucketization": "Debug",
      "LdesServer.Pagination": "Debug",
      "Npgsql.Command": "Information"
    }
  }
}
```
