# RDF Formats

Currently the LDES server supports the following serializations (for definitions and data):

| Format | Mime type | File Extension | Remarks |
|--------|-----------|----------------|---------|
| Turtle | `text/turtle` | `.ttl` | Most readable (human-oriented) *triple* format |
| TriG | `application/x+trig` | `.trig` | Most readable (human-oriented) *quad* format |
| JSON-LD | `application/ld+json` | `.jsonld` | JSON-like *quad* format |
| N-Triples | `application/n-triples` | `.nt` | Best machine-readable *triple* format |
| N-Quads | `application/n-quads` | `.nq` | Best machine-readable *quad* format |
