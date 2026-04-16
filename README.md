# Bygd

Bygd is a Valheim mod focused on living villages: outposts with NPC settlers, courier logistics, blueprint-driven building, and resource loops that make player-made settlements feel inhabited.

## Status

Bygd is in active development.

- Public release line: `0.0.x`
- Current public release: `0.0.1`
- Current focus: gameplay iteration, AI stability, and mod packaging

## What It Adds

### Outposts

- Place an **Elder's Table** and build a proper house around it
- Transfer the outpost to an NPC elder
- Supply the settlement with wood and food through an offering chest
- Progress the outpost by improving comfort and village infrastructure
- Protect transferred outposts from damage and unauthorized access

### Courier Network

- Place a **Courier Post** near a developed outpost
- Build **Mail Posts** with a chest and a sign using `@destination`
- Dispatch couriers between stations
- Use sign-based routing for stations and waypoints

### Lumberjack Role

- Assign a lumberjack to harvest nearby trees
- Collect wood and seeds automatically
- Replant saplings after chopping
- Feed gathered wood back into the outpost resource pool

### Blueprint Construction

- Preview a build with a ghost outline
- Rotate and place the blueprint before construction starts
- Build structures gradually from the bottom up
- Spend outpost wood on automated construction

### Caravan Systems

- Build route networks with signs
- Use shortest-path routing between stations
- Travel with caravan transport instead of moving everything by hand

## Requirements

- [BepInEx 5.4.23+](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jotunn 2.29+](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)
- [PlanBuild](https://thunderstore.io/c/valheim/p/MathiasDecrock/PlanBuild/) optional, for the blueprint ghost preview material

## Installation

### Mod Manager

1. Install BepInEx.
2. Install Jotunn.
3. Install Bygd when a packaged release is available.

### Manual

1. Install BepInEx and Jotunn.
2. Copy `bygd.dll` into `BepInEx/plugins/`.
3. Copy the `blueprints/` folder into `BepInEx/plugins/`.

## Compatibility

- Target game: Valheim
- Framework: `net462`
- Language support in-game: English and Russian
- Build requires local Valheim managed DLLs
- Multiplayer compatibility is not declared stable yet

## Console Commands

| Command | Description |
| --- | --- |
| `bygd debug` | Show outpost diagnostics |
| `bygd setlevel <N>` | Set nearest outpost level |
| `bygd devmode` | Toggle dev mode |
| `bygd cleanup` | Remove duplicate NPCs and ghost chests |
| `bygd reset` | Stop patrols and respawn couriers |
| `bygd list` | Show registered stations and waypoints |

## Known Limitations

- The mod is still balancing NPC behavior and settlement progression.
- Multiplayer support should be treated as experimental until explicitly documented otherwise.
- The build process depends on locally installed Valheim assemblies.
- Public packaging and release automation are still being set up.

## Development

```sh
dotnet build
task
```

`task` builds the mod and deploys the DLL plus blueprint files into the local BepInEx plugins directory.

If your Valheim install is not in the default location, override the managed DLL path:

```sh
dotnet build -p:ValheimManagedPath="/path/to/Valheim/Managed"
```

## Project Structure

```text
Plugin.cs              - mod initialization and piece registration
Commands.cs            - console commands
Framework/             - shared helpers, localization, reflection, logging
Outpost/               - elder table, settlers, resources, comfort, ward logic
Courier/               - courier posts, delivery runner, courier binding
Mail/                  - mail posts and parcel handling
Lumberjack/            - lumberjack role logic
Transport/             - carts, patrol movement, routing
NPC/                   - base NPC behavior and contextual dialogue
Blueprint/             - parser, ghost preview, builder, selection UI
Patches/               - Harmony patches
```

## Roadmap

- Add screenshots and gameplay clips
- Improve balancing and AI edge-case handling
- Harden multiplayer behavior

## License

MIT
