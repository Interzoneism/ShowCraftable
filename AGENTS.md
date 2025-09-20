## Context
- Vintage Story API and decompiled source code live in `VS_1.21_Decompiled/`
- Vintage Story assets (json for blocktypes, recipes, shapes etc) live in `VS_1.21_assets`
- We only work with the latest stable Vintage Story version 1.21 - all the code in VS_1.21_assets and VS_1.21_Decompiled are from the 1.21 version so you can trust it completely.

## Instructions
- When changing or using functions, methods, classes, variables or other things from the Vintage Story API or source, always check the corresponding file in VS_1.21_Decompiled/
- This is a small project, always check the full code in ShowCraftable for context, even when doing small changes.

## Build & test
- Do not build or test Cake / ZZCakeBuild / Program.cs
- Ignore all warnings about NET7 compatiblity or legacy net 7 warnings
- Build: `dotnet build -nologo -clp:Summary -warnaserror`
- Test: `dotnet test --nologo --verbosity=minimal`
- Lint (optional): `dotnet format --verify-no-changes`

## Project paths
- Solution: `./ShowCraftable/ShowCraftable/ShowCraftable.sln`
