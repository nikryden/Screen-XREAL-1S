# Hardware test M1 — XREAL 1S protocol check

Goal: confirm the 1S exposes the One-series network protocol (see `docs/findings/xreal-one-protocol.md`) and capture real IMU fixtures.
Time: ~20 min. Run commands in PowerShell from the repo root.

## Before you start
- [ ] New PC setup: Windows 11, `git clone git@github.com:nikryden/Screen-XREAL-1S.git`, .NET SDK 10.0.401+ (`winget install Microsoft.DotNet.SDK.10`), Windows SDK 10.0.26100 (comes with VS 2022/2026 "Windows application development" workload; only needed for the WinUI app, not the CLI)
- [ ] Record the PC's GPU / USB-C port (DP-alt capable?) in the report
- [ ] Build works: `dotnet build XrealScreen.slnx -c Debug -p:Platform=x64`
- [ ] Glasses charged/powered; firmware not updated during the test

## Steps
1. **Plug in** the glasses via USB-C directly to a DP-alt capable port (no hub). Put them on; confirm a picture appears.
2. **Firmware:** open the OSD menu on the glasses and note the firmware version.
3. **Disable anchor/stabilizer** in the OSD (screen mode: off / 0DoF). Note which option you chose.
4. **Network adapter:**
   ```powershell
   Get-NetAdapter | Format-Table Name, InterfaceDescription, Status, MacAddress
   ipconfig
   ```
   Expect a new adapter (NCM / "Remote NDIS"-like) with IPv4 `169.254.2.10`. Copy the adapter line and its IPv4.
5. **Port reachability:**
   ```powershell
   Test-NetConnection 169.254.2.1 -Port 52998
   Test-NetConnection 169.254.2.1 -Port 52999
   ```
   Note `TcpTestSucceeded` for each.
6. **USB IDs:**
   ```powershell
   Get-PnpDevice -PresentOnly | Where-Object InstanceId -match 'VID_3318' | Format-Table FriendlyName, InstanceId
   ```
7. **Probe** (adapter, all four ports, 2 s of IMU decoding):
   ```powershell
   dotnet run --project tools/XrealScreen.Cli -- probe
   ```
8. **IMU still recording** - place the glasses on a table, do not touch for 30 s:
   ```powershell
   dotnet run --project tools/XrealScreen.Cli -- imu record --seconds 30 --out captures/1s-still.xrcap --note "still fw=<version>"
   ```
   Writes raw bytes (`.xrcap`) and decoded samples (`.xrimu`) side by side. `captures/` is git-ignored; curated files are copied to `tests/fixtures/` later.
9. **IMU rotation recording** - wear the glasses; slowly turn head left 90 deg, back, right 90 deg, back, look up, down, then tilt head left/right:
   ```powershell
   dotnet run --project tools/XrealScreen.Cli -- imu record --seconds 30 --out captures/1s-rotate.xrcap --note "rotate fw=<version>"
   ```
10. **Live pose** - wear the glasses and watch yaw/pitch/roll follow your head; type `r` + Enter to recenter:
    ```powershell
    dotnet run --project tools/XrealScreen.Cli -- imu live --seconds 60
    ```
    Note whether yaw follows left/right turns with the right sign, or pitch/roll move instead (tells us the axis mapping).
11. **App** - start XrealScreen, Tracking page, Source "XREAL One-series glasses", Start.

## What to report back
Paste into the chat (or a new section in `docs/findings/xreal-one-protocol.md`):
- Date, device (XREAL 1S), firmware version, OSD mode used
- Output of steps 4, 5, 6, 7 and your observations from step 10
- The `captures/` files (or at least the `framer:` summary lines)
- Anything unexpected (disconnects, adapter missing, errors)

## If the adapter does not appear
- Try another USB-C port / cable; check Device Manager for an unknown device under VID_3318.
- Report the `Get-PnpDevice` output — fallback paths (HID) will be evaluated.
