# XrealScreen (Screen-XREAL-1S)

A Windows 11 app that turns **XREAL 1S** (also One / One Pro) AR glasses into a head-tracked virtual multi-monitor workspace.

- Multiple virtual screens in an arc or grid around you
- Ultrawide modes: **Off (16:9), 21:9, 32:9, 16:18** (two 16:9 stacked)
- **Auto center**: manual recenter, or follow with dead-zone and smoothing
- Modern WinUI 3 UI (Fluent, Mica, light/dark), .NET 10

> **Status: early development.** Head tracking, layout and the hardware tooling work; virtual monitors (M2) and rendering to the glasses (M3) are next. See [docs/STATUS.md](docs/STATUS.md) and [docs/ROADMAP.md](docs/ROADMAP.md).

## How it works
The glasses connect over USB-C: video arrives as a normal Windows monitor, and head-tracking (IMU) data arrives over a USB network adapter. XrealScreen fuses the IMU data into a head pose, creates virtual monitors with a signed open-source Indirect Display Driver, captures them and draws them head-locked in space on the glasses. Details: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

The official XREAL SDK is Unity/Android-only, so it is not used at runtime ([docs/findings/xreal-sdk.md](docs/findings/xreal-sdk.md)).

## Build
Requirements: Windows 11, .NET SDK 10.0.401+, Windows SDK 10.0.26100.
```powershell
dotnet build XrealScreen.slnx -p:Platform=x64
dotnet test --solution XrealScreen.slnx
dotnet run --project src/XrealScreen.App -p:Platform=x64
```

## Try it with glasses
Follow [docs/testing/hardware-test-M1.md](docs/testing/hardware-test-M1.md):
```powershell
dotnet run --project tools/XrealScreen.Cli -- probe
dotnet run --project tools/XrealScreen.Cli -- imu live
```

## Contributing / AI agents
Start with [CLAUDE.md](CLAUDE.md) or [AGENTS.md](AGENTS.md).

## License
MIT — see [LICENSE](LICENSE) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Not affiliated with or endorsed by XREAL.
