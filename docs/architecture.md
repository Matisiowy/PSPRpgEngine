# Architecture

The product has three deliberately separate parts.

## Editor

The editor owns user-facing project data, undo/redo, inspectors and previews.
It must never depend on PSP headers or PSP-specific memory layouts.

## Build pipeline

The asset compiler validates a project, converts source assets into PSP-friendly
formats and produces a deterministic, versioned `game.pak`. UUIDs remain the
authoring identity; compact hashes are included for fast future lookup.

Planned build stages:

1. Validate project IDs, scene references and component values.
2. Convert textures and audio.
3. Compile scenes and RPG databases.
4. Pack data with a versioned table of contents.
5. Combine the runtime, metadata and package into a distributable game folder.

The first implemented package version contains compiled scenes with
`Transform2D` and `SpriteRenderer` records. Its binary contract is documented
in `shared/docs/project-format.md`.

## PSP runtime

The runtime loads only compiled data. It currently locates the startup scene in
`game.pak` and renders its visible sprite records. It will own input, audio,
scene execution, gameplay systems and save data as later milestones land.

Initial runtime modules:

- platform and lifecycle;
- renderer;
- input;
- asset package reader;
- scenes and entities;
- tile maps;
- collision;
- UI and events.

Gameplay modules such as quests, inventory and dialogue will be built on top of
the scene/event layer rather than embedded into the renderer.
