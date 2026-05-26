# Skirmish Setup Steps

1. Create `SkirmishSnapshotCatalog` via `Create > Game > Skirmish Snapshot Catalog`, then assign it to `GameProjectInstaller` on `ProjectContext`.
2. Place friendly/enemy snapshot JSON files as `TextAsset` files under `Assets/SOs/SkirmishSnapshots/` and assign them to the catalog lists.
3. Create `AIShipEnemyShell` and `AIShipFriendlyShell` prefabs from the `Player` scene hierarchy, remove modules, keep AI/state machine components, and set team/layer (`EnemyTeam` + `Enemy` layer, `PlayerTeam` + `Friendly` layer).
4. Extract one scene asteroid into `Assets/Prefabs/Asteroid.prefab`.
5. In `MainGame`, add `SkirmishSpawnArea` and tune min/max bounds to your playable space.
6. On the `ShipSpawner` object in `Services.prefab`, add `SkirmishSpawner` and wire `asteroidPrefab`, both AI shell prefabs, spawn area, and optional spawn parent.
7. Disable or move the six hand-placed asteroid objects in `MainGame` so only runtime spawns are used.
8. Play from `MainMenu`, open `New Game`, set counts, launch, and verify random non-overlapping placement.
