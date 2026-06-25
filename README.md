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

The M1 pipeline is functional: the editor stores v2 projects with external scene
files, migrates v1 projects, validates and compiles scenes into `game.pak`, and
can launch a built `EBOOT.PBP` in PPSSPP. The PSP runtime reads the package and
renders the startup scene's `Transform2D` and `SpriteRenderer` components.

## Build the PC editor

Requires the .NET 8 SDK on Windows.

```powershell
dotnet build editor/PSPRpgEditor.sln
dotnet run --project editor/src/PSPRpgEditor.App
```

Open `samples/demo_game/demo.psprpg` to test the v2 project. Use **Build >
Build game.pak** to create:

```text
samples/demo_game/Build/PSP/GAME/PSP RPG Demo/game.pak
```

Configure PSPSDK and PPSSPP paths under **Build > Toolchain settings**.

## Test and compile assets

```powershell
dotnet run --project tests/PSPRpgEditor.Tests
dotnet run --project tools/PSPRpgAssetCompiler -- samples/demo_game/demo.psprpg
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

Editor JSON is an authoring format. PSP builds use the compact, versioned
`game.pak`; the PSP never parses the editor project.
