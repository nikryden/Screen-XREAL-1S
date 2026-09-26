---
name: windows-display-ccd
description: Windows display topology via the CCD API (QueryDisplayConfig, SetDisplayConfig, DisplayConfigGetDeviceInfo) from C# - enumerating monitors, identifying the XREAL glasses by EDID, setting modes, snapshot/restore of topology, and common pitfalls. Use when working on XrealScreen.Display, virtual monitor placement, or display mode changes.
---

# Windows display configuration (CCD)

## APIs (user32.dll)
- `GetDisplayConfigBufferSizes(flags, out numPaths, out numModes)`
- `QueryDisplayConfig(flags, ref numPaths, paths, ref numModes, modes, IntPtr.Zero)`
- `SetDisplayConfig(numPaths, paths, numModes, modes, flags)`
- `DisplayConfigGetDeviceInfo(ref header)` with typed request structs.

Use `LibraryImport` with blittable structs (`[StructLayout(LayoutKind.Sequential)]`), or CsWin32 generated bindings. Return values are Win32 error codes (0 = `ERROR_SUCCESS`); `ERROR_INSUFFICIENT_BUFFER` (122) means the topology changed between the two calls → retry loop.

## Enumerate
```csharp
const uint QDC_ONLY_ACTIVE_PATHS = 0x2;      // QDC_ALL_PATHS = 0x1
int err;
do {
    GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out var np, out var nm);
    paths = new DISPLAYCONFIG_PATH_INFO[np]; modes = new DISPLAYCONFIG_MODE_INFO[nm];
    err = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref np, paths, ref nm, modes, 0);
    Array.Resize(ref paths, (int)np); Array.Resize(ref modes, (int)nm);
} while (err == 122);
```
- Each path: `sourceInfo` (adapterId + id, GDI `\\.\DISPLAYn`) → `targetInfo` (monitor, refresh rate, rotation, scaling).
- Mode indices (`sourceInfo.modeInfoIdx`, `targetInfo.modeInfoIdx`) point into `modes`; with `QDC_VIRTUAL_MODE_AWARE` they are packed differently — pick one flag set and stick with it.
- Source mode: resolution + desktop position. Target mode: `targetVideoSignalInfo` (active size, vSync frequency as rational).

## Identify the glasses (EDID)
`DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME` (type 2) → `DISPLAYCONFIG_TARGET_DEVICE_NAME`:
- `edidManufactureId` (big-endian PNP ID packed in 3×5 bits: decode `((v>>10)&31)+'@'`, `((v>>5)&31)+'@'`, `(v&31)+'@'` after byte-swap)
- `edidProductCodeId`, `monitorFriendlyDeviceName`, `monitorDevicePath`, `outputTechnology` (DisplayPort external / USB-C alt).
- Validate flag `friendlyNameFromEdid` / `edidIdsValid` before trusting IDs.
- XREAL 1S = EDID `MRG` / `0x4102` [verified-hw] (`GlassesCatalog.Displays`, `GlassesDisplayLocator`). The glasses re-enumerate with a different mode list per OSD UltraWide setting (table in `docs/findings/xreal-1s-hardware.md`). Match by EDID IDs, fall back to friendly name; never by `\\.\DISPLAYn` (unstable).
- Source name: type 1 `GET_SOURCE_NAME` → `viewGdiDeviceName`; use to correlate with `HMONITOR` (`EnumDisplayMonitors` + `GetMonitorInfo`) for WGC.

## Change modes / topology
- Modify the queried arrays (e.g. source mode width/height, position), then:
  `SetDisplayConfig(np, paths, nm, modes, SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_ALLOW_CHANGES | SDC_SAVE_TO_DATABASE)`.
- Validate first with `SDC_VALIDATE` instead of `SDC_APPLY`.
- Extend/clone presets: `SetDisplayConfig(0, null, 0, null, SDC_APPLY | SDC_TOPOLOGY_EXTEND)`.
- Refresh rate lives on the **target** mode / path `targetInfo.refreshRate`; resolution on the **source** mode.
- Keep primary monitor at (0,0); other sources positioned relative to it without overlap.

## Snapshot / restore
- Before any change: query `QDC_ALL_PATHS`, serialize paths + modes + a list of monitor identities (EDID IDs, device path) to `%LOCALAPPDATA%\XrealScreen\topology-snapshot.json`.
- Restore: re-query, map saved paths to current adapter LUIDs/target ids (LUIDs change across reboots/driver restarts), apply with `SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_APPLY | SDC_ALLOW_CHANGES | SDC_SAVE_TO_DATABASE`; on failure fall back to `SDC_TOPOLOGY_EXTEND`.
- Restore paths: normal exit, service watchdog after app crash, panic hotkey. Delete the snapshot after a successful restore.

## Pitfalls
- `SetDisplayConfig` applies to the calling user's session; it does not work from Session 0 → call from the user-session engine (ADR-0004).
- Adding/removing VDD monitors triggers async topology changes: wait for `WM_DISPLAYCHANGE` / re-query with retry before using new paths.
- Adapter LUIDs are not stable across boots; never persist them as identity.
- Hybrid GPUs: the glasses output and VDD monitors must share an adapter for zero-copy capture; check `sourceInfo.adapterId`.
- DPI: set virtual monitors to 100% scaling (per-monitor DPI is a separate, undocumented API; prefer documenting user action first).
- HDR state (`GET_ADVANCED_COLOR_INFO`, type 9) affects capture format.
- Tests: wrap P/Invoke behind `IDisplayTopology`; unit-test mapping/restore logic with recorded snapshots, not live calls.
