# PSP RPG Engine

PSP RPG Engine is a small, data-driven game engine and desktop authoring tool
for creating top-down RPG games for Sony PSP.

The repository contains:

- `runtime/` - the C++ runtime compiled into `EBOOT.PBP`;
- `editor/` - the PC editor used to author projects and scenes;
- `shared/` - versioned project-format definitions;
- `tools/` - future asset compiler and packager;
- `templates/` - starter projects;
- `samples/` - projects used to test the whole pipeline.

## Current milestone

The editor can create, open and save `.psprpg` projects, inspect scene objects,
add and delete entities, and position them on a snapping 2D canvas. The
`.psprpg` authoring format contains readable, versioned JSON. The PSP runtime
initializes the GU, displays a test scene, and exits cleanly through HOME.

## Build the PC editor

Requires the .NET 8 SDK on Windows.

```powershell
dotnet build editor/PSPRpgEditor.sln
dotnet run --project editor/src/PSPRpgEditor.App
```

## Build the PSP runtime

Run these commands in a PSPDEV environment:

```sh
cmake -S runtime -B build/psp -DCMAKE_TOOLCHAIN_FILE="$PSPDEV/psp/share/pspdev.cmake"
cmake --build build/psp
```

Some PSPDEV installations also provide the convenience command:

```sh
psp-cmake -S runtime -B build/psp
cmake --build build/psp
```

The resulting `EBOOT.PBP` can be launched with PPSSPP or copied to
`PSP/GAME/PSPRpgEngine/` on a PSP.

## Design rule

Editor JSON is an authoring format. The final PSP build will use a compact,
versioned `game.pak`; the PSP will not parse the complete editor project.
