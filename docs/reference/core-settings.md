# Core Settings

There are a number of settings related to ingesting and retrieving data that you can change if needed:

| Setting | Type | Description | Default |
|---------|------|-------------|---------|
| `LdesServer:BaseUri` | string | Base URI for LDES or page URLs in content | `http://localhost:8080/feed/` |
| `LdesServer:CreateEventSource` | boolean | Create default view named `_` | `true` |
| `LdesServer:DefinitionsDirectory` | string | Directory for static definitions | None |

> [!CAUTION]
> **Critical:** Set `BaseUri` to your server's external endpoint URL (e.g., `https://ldes.example.org/`).  
> If misconfigured, clients will receive incorrect URLs in LDES content, breaking navigation between pages.

**Example:** If your server is accessible at `https://ldes.example.org/` but `BaseUri` is set to `http://localhost:8080/feed/`, clients will try to follow links to localhost and fail.
