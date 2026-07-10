# Space Pixel Unity project

We are making a game about spaceships shooting at each other in a game. They are made of destroyable pixels, so the physics are fun!
This game aims to be a bit similar to Highfleet, but expands on it by making individual pixels destroyable.

## Contributing

Code quality is important. You can write comments, but 99% of comments should be deleted and code extracted to methods before presenting your work as done. Long method names are totally okay, as long as they are descriptive
If a component/anything is necessary for the script, don't silently fail, fail hard with an error!
Periodically check for errors by calling tools - it sometimes takes a bit of time to auto compile .cs files after you write them
This project uses assemblies, so if you want to use code from other assembly, double check if you should. If you should, write the code as if you used the assembly and after compiling and seeing errors, just please let me know in the chat. I will link them manually in my IDE
Don't manually create or edit .meta files (including by automated agents) — let Unity generate them, and only commit the .meta files Unity creates.
Do not maintain both "legacy" and new systems. If something must be changed, remove the legacy code, the game is very early in development
Do not use "Bootstrappers" - scripts that generate scenes or load resources dynamically. Rather, present me with a guide how can I manually integrate your changes, thanks!

## Gotchas

- Ship position: use `ship.GetPosition()` (which uses `CommandModule.Transform.position`), NEVER `ship.transform.position`
- Resources naming conflict: `Core.Ship.Resources` (crew/power) collides with `UnityEngine.Resources`. Always alias it: `using Resources = Core.Ship.Resources;`
- You won't be able to run tests. I have the Unity Editor open.

## Third-Party Libraries

Prefer these over their standard-library equivalents:

- **ZLinq** (`using ZLinq;`): use `.AsValueEnumerable()` instead of `System.Linq` for collection queries. It does not allocate memory
- **UniTask** (`using Cysharp.Threading.Tasks;`): use `async UniTask` / `async UniTaskVoid` instead of `async Task` / `async void`
- **Zenject** (`using Zenject;`): dependency injection — use `[Inject]` on private fields, register new services in `GameSceneInstaller`

## Architecture

All of the physically destructible objects are/inherit from PixelatedRigidbody.

Ship is the base class for ship, with PlayerShip and AIShip inheriting from it. AIShip has state machines: behaviour and navigation.

UI = UIToolkit with custom DesignSystem - for all UI work, refer to Assets/DesignSystem/Docs/COMPONENTS.md. Always create dedicated UXML assets, instead of runtime-built VisualElements. DesignSystem (DS) should handle 90% of styling already

### Assemblies

- AI: just the basic state machine, don't touch
- Background: just the background, don't touch
- Context: when creating new services etc., make sure to add them to GameSceneInstaller (Zenject) and use its dependency injection
- ContourTracer: calculating physical outlines of PixelatedRigidbodies, don't touch
- Core: mostly basic interfaces/enums
- EasyPool: just the pool, don't touch
- Editor: inspector extensions, tools, standalone debugger scripts
- Events: ScriptableObjects (Collision, etc.). When objects need to communicate with each other, they should use events. Don't forget to unsubscribe from events when the object is disabled and subscribe on enabled
- External: third party code, don't touch
- Gameplay: the actual gameplay code
- Grid: PixelGrid mostly (is inside PixelatedRigidbody), probably don't touch
- Instantiation: used for ZenjectInstantiator when you need to instantiate something with Zenject, don't touch
- LM: my personal utility scripts, feel free to use these utils (GameInput, SimpleTimer, Timer, VecExt, MathExt, DefaultDictionary)
- Pixelation: PixelatedRigidbody! And CollisionResolver and PixelCollisionHandler
- Services: many different services, like spawners, or to access ships, sectors etc. Don't forget to add new services to GameSceneInstaller (Zenject) and use its dependency injection
- Ships: all ship related code, like Ship, PlayerShip, AIShip, and very importantly - Module (ships are made of modules, which have PixelatedRigidbody, and they are what make up the ship, they can be destroyed)
  - Current modules inheritors: CommandModule (its position is the ship's position - don't use ship transform.position!). Cannon, Laser, Engine
  - Modules use efficiency based on how many pixels they have left
  - Modules have crew and power needs, and they can produce both
- UI: use UIToolkit

Check `ASSEMBLY_GUIDS.md` for exact GUIDS for these and external assemblies

## Testing

I will run tests after you are done. Make sure to test critical functionality in PlayMode tests
Tests are in AssemblyFolder/Tests in its own assembly
To allow tests to see private methods/fields, add [assembly: InternalsVisibleTo("AssemblyName.Tests")]. Then create new properties `internal InternalXYZ => XYZ` or methods like XYZForTesting
A similar pattern can be done to get access to private fields in Editor inspector extensions
Use NSubstitute for mocking dependencies

## Cursor Cloud specific instructions

This section is durable guidance for cloud agents running in the headless VM (the update script already ran).

### Running Unity tests / builds headlessly (Docker + license secrets)

Headless EditMode/PlayMode tests and a Linux player build ARE possible in the cloud VM via the GameCI editor image + a Unity license. This is a **heavy one-time setup that is intentionally NOT in the update script** (10.8 GB image + Docker daemon + .NET SDK). If a fresh VM lacks it, reinstall: Docker (dind, `storage-driver: fuse-overlayfs` with `features.containerd-snapshotter: false` on Docker 29+, iptables-legacy), `dotnet-sdk-8.0`, then `docker pull unityci/editor:ubuntu-6000.5.1f1-linux-il2cpp-3` (editor `6000.5.1f1`, includes `LinuxStandaloneSupport`). Start the daemon before use: `sudo dockerd &`.

- **Requires the Unity license secrets** `UNITY_LICENSE` + `UNITY_EMAIL` + `UNITY_PASSWORD` (same as CI). `UNITY_LICENSE` holds the Personal `.ulf` file contents; activation = writing it into the editor's license dir. Without them the editor launches but stops at `No valid Unity Editor license found`.
- **Run EditMode tests** (swap `EditMode`→`PlayMode` for play tests; CI runs both via `testMode: all`):
  ```bash
  sudo docker run --rm --user "$(id -u):$(id -g)" -e HOME=/tmp -e UNITY_LICENSE \
    -v "$PWD":/project -w /project unityci/editor:ubuntu-6000.5.1f1-linux-il2cpp-3 \
    bash -lc 'mkdir -p /tmp/.local/share/unity3d/Unity && printf "%s" "$UNITY_LICENSE" > /tmp/.local/share/unity3d/Unity/Unity_lic.ulf && \
      unity-editor -projectPath /project -runTests -testPlatform EditMode -testResults /project/editmode-results.xml -logFile /dev/stdout'
  ```
- **Build a Linux player** (default active scenes; no custom build method exists in-repo):
  ```bash
  sudo docker run --rm --user "$(id -u):$(id -g)" -e HOME=/tmp -e UNITY_LICENSE \
    -v "$PWD":/project -w /project unityci/editor:ubuntu-6000.5.1f1-linux-il2cpp-3 \
    bash -lc 'mkdir -p /tmp/.local/share/unity3d/Unity && printf "%s" "$UNITY_LICENSE" > /tmp/.local/share/unity3d/Unity/Unity_lic.ulf && \
      unity-editor -projectPath /project -quit -buildLinux64Player /project/Builds/SpacePixels.x86_64 -logFile /dev/stdout'
  ```
- The canonical path is still **CI** (`.github/workflows/tests.yml`, `game-ci/unity-test-runner`, image `...-base-3`). Running tests compiles all game + test assemblies, so it also validates that C# changes compile. The interactive game GUI cannot be meaningfully *played* headlessly (no display) — use batchmode tests/builds instead.
- Host `.NET SDK` (`dotnet`) is installed but the game's C# is compiled **only by Unity** (assemblies depend on `UnityEngine`); do not expect `dotnet build` on the generated `.csproj`/`.sln` to reflect a real build.

### What IS runnable in the cloud VM: the Python asset tooling under `python/`

`pillow` and `requests` are installed system-wide by the update script. Both tools use paths relative to the current working directory, so run them from the directories noted below.

- **`python/sprite_generator`** (procedural pixel-ship sprite PNGs): run from `python/` with
  `python3 -c "from sprite_generator.pipeline import run_pipeline; run_pipeline()"`.
  It reads `sprite_generator/raw_sprites.txt` (gitignored — you must create it: a line ending in `.txt` names a sprite, followed by rows of digits `0`–`7`), writes parsed per-sprite files to `sprite_generator/inputs/`, and outputs PNGs to `Assets/Sprites/Generated/New/`. Path quirk: the output dir is relative to CWD, so running from `python/` writes to `python/Assets/...`, not the repo-root `Assets/`; `cd` to repo root (or adjust) if you want the real assets folder. Faction/armor palette is chosen by filename (`enemy` / `_armor` substrings).
- **`python/icon_downloader`** (Design System Material Symbols icons): run from `python/icon_downloader/` with `python3 main.py`. Needs network access to `https://api.iconify.design`; writes SVGs into `Assets/DesignSystem/Resources/Textures/Icons/` and appends rules to `Icons.uss` (edits tracked files — review before committing).
