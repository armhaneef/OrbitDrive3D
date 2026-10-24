# Orbit Drive 3D

A small, original C# / WPF 3D arcade game for Windows. Drive an angular silver
electric rover, jump over barriers, fly into a stylized orbital zone, dodge
asteroids, collect energy cores, and return to the runway.

**Tamil setup guide:** open `START_HERE_TAMIL.txt`.

## Run

1. Extract the whole ZIP.
2. Install/update Visual Studio with **.NET desktop development** and the **.NET 8 SDK**.
3. Open `OrbitDrive3D.sln` in Visual Studio, then press **Ctrl+F5**.
4. Click **START FLIGHT** or press Enter.

Visual Studio 2022 17.8 is the minimum for targeting .NET 8; an updated Visual
Studio is recommended to match the installed SDK. This is a WPF application,
not a Windows Forms or Unity project. It creates its window in C#, so there is
no XAML designer file to open. No external NuGet gameplay packages, art assets,
database, login or API keys are required.

Alternatively, with the .NET SDK on PATH, double-click `RUN_GAME.cmd`.

## Controls

| Key | Action |
|---|---|
| A / D or Left / Right | Steer |
| F | Toggle flight to 74 game metres / automatic landing |
| E / Q or Up / Down | Raise / lower target altitude |
| Space | Jump from the ground |
| Hold Shift | Boost (uses rechargeable energy) |
| P / Esc | Pause / resume |
| Enter | Start / resume / retry |
| R | Restart |
| C | Camera distance |
| L | Shorter / longer obstacle visibility distance |
| F11 | Fullscreen / window |
| Alt+F4 | Quit |

The car moves forward automatically. Reach at least 65 m, land back at 0 m,
and travel 4.5 km to finish the mission. The route continues if flight or
landing objectives are unfinished. Every cyan core gives 250 score and 22
energy; score also increases by one per metre. Orange barriers and asteroids
end the run on contact. Losing keyboard focus pauses the simulation.

The personal best is stored in `%LOCALAPPDATA%\OrbitDrive3D\best.txt`.
Deleting that file resets the record. The game does not contact a server.

## What is included

| File | Purpose |
|---|---|
| `OrbitDrive3D.sln` | Open this in Visual Studio |
| `OrbitDrive3D/Program.cs` | STA WPF entry point |
| `OrbitDrive3D/GameWindow.cs` | 3D world, HUD, keyboard, camera, save data |
| `OrbitDrive3D/Meshes.cs` | Procedural car, wheels, boxes, rings and spheres |
| `OrbitDrive3D/GameState.cs` | Simulation, spawning, collisions, score, mission |
| `LogicChecks/` | Dependency-free console checks for gameplay |
| `RUN_GAME.cmd` | Build and run using the SDK |
| `BUILD_EXE.cmd` | Publish an offline Windows x64 executable folder |
| `VALIDATION.txt` | Exact validation performed and limitations |

## Publish an executable

Run `BUILD_EXE.cmd`. It creates `publish-win-x64/OrbitDrive3D.exe` plus its
runtime files. Keep that whole folder together. This publish is self-contained
and intended for Windows x64 computers. It requires internet the first time
to fetch the runtime pack. No prebuilt executable is included in this ZIP.

Equivalent command, from the solution folder:

```powershell
dotnet publish OrbitDrive3D/OrbitDrive3D.csproj -c Release -r win-x64 --self-contained true -o publish-win-x64
```

## Gameplay checks

```powershell
dotnet run --project LogicChecks/LogicChecks.csproj -c Release
```

This checks gameplay independently of WPF. Test fixtures clear the generated
hazards for movement and mission checks; separate tests insert explicit
collision and pickup fixtures. The test project is deliberately separate from
the game solution so the game remains the only startup project.

## Scope and validation

This is a low-poly prototype with actual perspective-projected 3D meshes,
lighting, chase camera and three-axis obstacle positions. The orbit altitude
is an arcade scale, not the real boundary of space. The car is an original
Tesla/Cybertruck-inspired design, with no Tesla logos or external models.
There is no audio, multiplayer or realistic flight physics.

C# compilation against .NET 8/WPF reference assemblies succeeded, and 14
gameplay checks passed. The Windows UI has **not** been run or visually
verified in the Linux creation environment. Full MSBuild solution validation
was blocked by the environment's process-information restrictions. See
`VALIDATION.txt`; compiler checks are not a substitute for Windows play testing.

## Documentation

- [Microsoft WPF project setup](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/get-started/create-app-visual-studio)
- [.NET 8 Visual Studio requirements](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/8.0/version-requirements)
- [WPF 3D graphics](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/3-d-graphics-overview)

All game meshes and UI are generated locally by the supplied C# source.
