# Project format v2

A project is a directory. Its `.psprpg` file is a small JSON manifest; scenes
and future RPG databases are separate JSON documents. Source assets are never
embedded in the manifest.

```text
MyGame/
├── MyGame.psprpg
├── Assets/
│   ├── Textures/
│   ├── Audio/
│   ├── Fonts/
│   └── Tilesets/
├── Scenes/
├── Animations/
├── Events/
└── RPG/
    ├── Dialogs/
    ├── Items/
    └── Quests/
```

All references use stable UUIDs and project-relative forward-slash paths.
Absolute paths and paths escaping the project directory are rejected.

## Manifest

```json
{
  "formatVersion": 2,
  "projectId": "11111111-1111-1111-1111-111111111111",
  "name": "My RPG",
  "startupScene": "22222222-2222-2222-2222-222222222222",
  "sceneFiles": [
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "path": "Scenes/22222222222222222222222222222222.scene.json"
    }
  ]
}
```

## Migration

The editor can open the original single-file v1 format. It deterministically
assigns UUIDs and marks the project as migrated. Saving writes a v2 manifest,
creates the project directory structure and moves scene data into `Scenes/`.

## Compiled package v1

All integers and floats in `game.pak` are little-endian.

Header (48 bytes):

| Offset | Size | Value |
| --- | ---: | --- |
| 0 | 8 | `PRPGPAK\0` |
| 8 | 4 | package version |
| 12 | 4 | entry count |
| 16 | 4 | table offset |
| 20 | 4 | data offset |
| 24 | 16 | startup scene UUID |
| 40 | 8 | reserved |

Each table entry is 32 bytes: resource type, FNV-1a ID hash, absolute data
offset, size and the 16-byte UUID. Resource type `1` is a compiled scene.
