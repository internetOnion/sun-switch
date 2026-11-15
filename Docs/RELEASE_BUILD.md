# Release build

How to make the build of Sun Switch that people play. The tooling is in `Assets/Build/Editor`.

## Making a build

In the editor: **Sun Switch > Build Game > macOS**, **Windows** or **macOS + Windows**.

From a terminal (or CI), with the editor closed:

```
Unity -batchmode -quit -projectPath . \
  -executeMethod SunSwitch.Build.ReleaseBuild.BuildFromCommandLine \
  -platforms mac,win -buildRoot Builds
```

It exits with code 1 if a build fails. Each build lands in its own folder:

```
Builds/<version>/macOS/Sun Switch.app
Builds/<version>/Windows/Sun Switch.exe (+ Sun Switch_Data, UnityPlayer.dll, …)
Builds/<version>/<platform> debug symbols/   Burst debug info, not for players
```

The platform folder holds only the game, so it can be zipped and shared as it is. `Builds/` is git-ignored.

**Windows** needs the *Windows Build Support (Mono)* module for Unity 6000.3.8f1, added in Unity Hub (Installs > ⋯ > Add modules). Without it, the Windows build is skipped with a message saying so, and the macOS build still runs.

## What a build checks and sets

Before building, it checks Build Settings: `Main` must be first, `Section1` to `Section4` must be there, and no `/Sandbox/` scene may ship. If something is wrong, it stops and says what. **Sun Switch > Build Game Scenes** sets the list.

It also applies the release player settings. **Sun Switch > Build Game > Apply Release Settings** applies them on their own.

| Setting | Value |
|---|---|
| Product name | Sun Switch |
| Company | Sun Switch Team |
| Bundle id | `com.sunswitchteam.sunswitch` |
| Version | `PlayerSettings.bundleVersion` (Project Settings > Player). Bump it before a new release, since the output folder uses it |
| Window | Borderless full screen at the desktop resolution; windowed is 1920 × 1080 and resizable; one instance at a time |
| Splash | The Unity splash is off, so the game opens on its own Start screen |
| Icon | `Assets/Art/UI/AppIcon.png`, drawn by `AppIconBaker`: day / night tile with the sun and crescent moon |

The name, company and bundle id are constants at the top of `ReleaseBuild.cs`. Changing the company or product name also moves where settings are saved (PlayerPrefs), so players' saved volumes and sensitivity reset once.

## Before sharing a build

- Play it from the Start screen to the Summit once: Play, a death and respawn, Pause, Settings, the win screen.
- Release builds log very little. If something goes wrong, the log is in `~/Library/Logs/Sun Switch Team/Sun Switch/Player.log` on macOS, or `%USERPROFILE%\AppData\LocalLow\Sun Switch Team\Sun Switch\Player.log` on Windows.
