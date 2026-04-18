## Instructions
- This is a small project, always check the full code in ShowCraftable for context, even when doing small changes.

## Build & test
- Do not build or test Cake / ZZCakeBuild / Program.cs
- Ignore all warnings about NET7 compatiblity or legacy net 7 warnings
- Build: `dotnet build -nologo -clp:Summary -warnaserror`
- Test: `dotnet test --nologo --verbosity=minimal`
- Lint (optional): `dotnet format --verify-no-changes`

## Project paths
- Solution: `./ShowCraftable/ShowCraftable/ShowCraftable.sln`
