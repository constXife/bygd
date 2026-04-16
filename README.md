# Bygd — Living Villages for Valheim

A Valheim mod that adds NPC-driven settlements with automated resource gathering, courier networks, and blueprint-based construction.

## Features

### Outpost System
- **Elder's Table** — place it anywhere, build a house around it, and an elder NPC will arrive
- **Resource management** — settlers consume wood and food; supply them through offering chests
- **Comfort-based progression** — upgrade your outpost (levels 0-3) by improving comfort, just like the vanilla rested bonus
- **Ward protection** — transferred outposts are protected from damage and unauthorized access

### Courier Network
- **Courier Post** — assign a courier to deliver items between outposts
- **Mail Posts** — place them with a chest and a sign (`@destination`) to send parcels
- **Boar mounts** — level 3+ outposts get boar-mounted couriers for faster delivery

### Lumberjack
- **Lumberjack Post** — a woodcutter NPC that chops trees in nearby forests (20-80m from village)
- Collects wood and seeds, replants saplings after chopping
- Delivers wood to the outpost's resource pool

### Blueprint Construction
- **Ghost preview** — see a translucent outline of the planned building (uses PlanBuild shader)
- **Rotate and confirm** — press E to rotate, Shift+E to start building
- **Gradual construction** — pieces are placed bottom-up, 3 per second, consuming wood from the outpost
- **Player choice** — cancel the plan and build your own design instead

### Caravan System
- **Sign-based routing** — place signs with `@station` or `#waypoint` to create a route network
- **Dijkstra pathfinding** — caravans find the shortest path between stations
- **Lox-pulled carts** — ride along as a passenger

## Requirements

- [BepInEx 5.4.23+](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jotunn 2.29+](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)
- [PlanBuild](https://thunderstore.io/c/valheim/p/MathiasDecrock/PlanBuild/) (optional, for blueprint ghost preview)

## Installation

1. Install BepInEx and Jotunn via your mod manager
2. Copy `bygd.dll` and the `blueprints/` folder to `BepInEx/plugins/`

## Console Commands

| Command | Description |
|---------|-------------|
| `bygd debug` | Show outpost diagnostics |
| `bygd setlevel <N>` | Set nearest outpost level |
| `bygd devmode` | Toggle dev mode (free building, no resource drain) |
| `bygd cleanup` | Remove duplicate NPCs and ghost chests |
| `bygd reset` | Emergency stop: halt all patrols, respawn couriers |
| `bygd list` | Show registered stations and waypoints |

## Architecture

```
Plugin.cs              — mod init, piece registration
Commands.cs            — console commands (Jotunn ConsoleCommand)
Framework/
  AnchorUI.cs          — shared UI panel for all anchor buildings
  ObjectFinder.cs      — generic spatial queries
  PostRuntime.cs       — shared piece lifecycle (EnsureComponent/IsLivePost)
  NPCSpawnHelper.cs    — shared NPC spawn/despawn/find
  Localizations.cs     — Russian + English UI strings
  Reflect.cs           — Harmony reflection (private Valheim APIs)
  Log.cs, PrefabNames.cs, AISuppression.cs
Outpost/               — elder's table, settlers, resources, comfort, ward
Courier/               — courier post, delivery runner, courier binding
Mail/                  — mail posts, parcel system
Lumberjack/            — woodcutter NPC, tree chopping, sapling planting
Transport/             — cart system, walker, patrol, mount config
NPC/                   — base NPC class, settler/courier dialogue
Blueprint/             — parser, ghost preview, builder, selection UI
Patches/               — all Harmony patches
```

## Building

```sh
dotnet build
task          # build + deploy to BepInEx/plugins
```

Requires Valheim managed DLLs at the path specified in `bygd.csproj`.

## License

MIT
