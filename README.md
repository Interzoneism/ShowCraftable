# ShowCraftable

> ⚠️ **A note from the author:** I'm sorry in advance — almost all of this code was written with AI assistance. The codebase is functional but can be dense and a bit painful to navigate. Browse at your own risk and feel free to ask questions!

ShowCraftable is a client-side [Vintage Story](https://www.vintagestory.at/) mod (v1.20+) that scans your inventory and nearby storage to show you exactly what you can craft right now, directly inside the in-game handbook.

---

## Features

- **Craftable tab** — Added to the handbook, lists every grid recipe you can craft using items currently in your hotbar, crafting grid, backpack, and storage containers within your configured search radius.
- **Craftable (Mods) tab** — Same as the Craftable tab but shows only recipes added by mods. Kept separate so the base Craftable tab stays fast.
- **Base Items / Wood Types / Stone Types tabs** — Additional handbook tabs for quickly browsing vanilla materials.
- **Fetch button** — Appears on individual recipe pages in the handbook. Click it to automatically pull the required ingredients from nearby storage directly into your inventory.
- **Custom tab categories** — Map recipe group page codes to custom handbook tab names via config.

---

## Installation

1. Download the latest `.zip` from the [Releases](../../releases) page.
2. Place it in your Vintage Story `Mods/` folder.
3. Launch the game — the mod loads automatically.

Alternatively, find it on the [Vintage Story ModDB](https://mods.vintagestory.at/).

---

## Configuration

The config file is created at `VintagestoryData/ModConfig/ShowCraftable.json` on first run.

| Key | Default | Description |
|-----|---------|-------------|
| `EnableFetchButton` | `true` | Show the fetch-ingredients button on recipe pages. |
| `DisableFetchButtonOnServer` | `false` | Disable the fetch button when playing on a multiplayer server. |
| `UseDefaultFont` | `false` | Use the game's default font for handbook tabs instead of the custom one. |
| `SearchDistanceItems` | `20` | Block radius to scan for storage containers. Set to `0` to search inventory only. |
| `AllStacksPartitions` | `-1` | Number of partitions used when building the full-stacks index. `-1` = automatic. |
| `GroupPageCategoryNames` | `{}` | Map a recipe-group page code to a custom handbook tab name, e.g. `{ "mymod:mygroup": "My Tab" }`. |

---

## Building from source

Requirements: .NET 7 SDK and a Vintage Story installation.

```bash
dotnet build ShowCraftable/ShowCraftable/ShowCraftable.sln
```

---

## Compatibility

| Vintage Story | Mod version |
|---------------|-------------|
| 1.21          | 1.2.x       |
| 1.20          | 1.0.x       |

---

## License

See [LICENSE](LICENSE) if present, or contact the author.
