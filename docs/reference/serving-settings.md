# Serving Settings

When an LDES client requests a page from the LDES server, the server indicates whether the page is immutable and how long the response can be cached. These settings control the HTTP `Cache-Control` header values:

| Setting | Description | Default |
|---------|-------------|---------|
| `LdesServer:Serving:MaxAge` | Cache duration (seconds) for mutable pages | `60` seconds |
| `LdesServer:Serving:MaxAgeImmutable` | Cache duration (seconds) for immutable pages | `604800` seconds (one week) |

**Why this matters:** Proper cache settings reduce server load and improve client performance. Mutable pages need shorter cache times because they may receive new members. Immutable pages can be cached longer as their content never changes, that is, members may be removed due to retention policies, but members will *never* be added to an immutable page.
