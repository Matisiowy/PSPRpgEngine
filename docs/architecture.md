# Architecture

The product has three deliberately separate parts.

## Editor

The editor owns user-facing project data, undo/redo, inspectors and previews.
It must never depend on PSP headers or PSP-specific memory layouts.

## Build pipeline

The asset compiler validates a project, converts source assets into PSP-friendly
formats, assigns stable numeric IDs and produces `game.pak`.

Planned build stages:

1. Validate project and referenced assets.
2. Convert textures and audio.
3. Compile scenes and RPG databases.
4. Pack data with a versioned table of contents.
5. combine the runtime, metadata and package into a distributable game folder.

## PSP runtime

The runtime loads only compiled data. It owns rendering, input, audio, scene
execution, gameplay systems and save data.

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

