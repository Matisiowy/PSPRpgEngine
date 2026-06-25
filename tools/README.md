# Tools

## PSPRpgAssetCompiler

Validates a `.psprpg` project and builds its deterministic PSP package.

```powershell
dotnet run --project tools/PSPRpgAssetCompiler -- `
  samples/demo_game/demo.psprpg `
  artifacts/demo/game.pak
```

When the output argument is omitted, the package is written to
`<project>/Build/game.pak`.
