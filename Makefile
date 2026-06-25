.PHONY: editor compiler test runtime clean

editor:
	dotnet build editor/PSPRpgEditor.sln

compiler:
	dotnet build tools/PSPRpgAssetCompiler/PSPRpgAssetCompiler.csproj

test:
	dotnet run --project tests/PSPRpgEditor.Tests

runtime:
	psp-cmake -S runtime -B build/psp
	cmake --build build/psp

clean:
	dotnet clean editor/PSPRpgEditor.sln
	cmake --build build/psp --target clean
