# Project format v1

Authoring projects use the `.psprpg` extension. Their contents are versioned
JSON so projects remain diffable and easy to diagnose:

```json
{
  "formatVersion": 1,
  "name": "My RPG",
  "startupScene": "main",
  "scenes": []
}
```

Each scene has a stable string ID, display name, dimensions and a list of
entities. During compilation, string IDs will be converted to numeric IDs.

The format is versioned from the first commit. Readers must reject versions
newer than they support instead of silently interpreting them incorrectly.
