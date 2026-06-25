.PHONY: editor runtime clean

editor:
	dotnet build editor/PSPRpgEditor.sln

runtime:
	psp-cmake -S runtime -B build/psp
	cmake --build build/psp

clean:
	dotnet clean editor/PSPRpgEditor.sln
	cmake --build build/psp --target clean
