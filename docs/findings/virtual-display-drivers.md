# Virtual display drivers (IddCx)

Virtual monitors on Windows require an Indirect Display Driver (UMDF, IddCx). **Decision: ADR-0008 — virtual-display-rs** (supersedes ADR-0001, VirtualDrivers VDD).

## VirtualDrivers/Virtual-Display-Driver (not used; see ADR-0008)
| Fact | Value | Tag |
|------|-------|-----|
| Repo | https://github.com/VirtualDrivers/Virtual-Display-Driver | |
| License | MIT (driver repo). Companion app repo `VirtualDrivers/Virtual-Driver-Control` has **no license**: facts only, never port its code | [from-source:MIT] |
| Signing | `MttVDD.dll` + `mttvdd.cat` Authenticode-signed "SignPath Foundation" (GlobalSign GCC R45 CodeSigning CA 2020); not Microsoft/WHQL-signed | [verified-local] (signature check on repo binaries, 2026-09-26) |
| Modes | 640×480 .. 8K, up to 500 Hz, float refresh rates | [from-source:MIT] |
| Settings | `C:\VirtualDisplayDriver\vdd_settings.xml` (path overridable, see below) | [from-source:MIT] |
| Control pipe | `\\.\pipe\MTTVirtualDisplayPipe` | [from-source:MIT] |

## VirtualDrivers VDD control interface (researched 2026-09-26)
Source: `Virtual Display Driver (HDR)/MttVDD/Driver.cpp` at main `d7244969` (2026-06-22); line numbers refer to that commit. The command set is **identical** in release tag `25.7.23` (diffed). Not yet run on hardware (VDD is not installed on the test PC).

### Pipe transport [from-source:MIT]
- Name `\\.\pipe\MTTVirtualDisplayPipe` (`:47`). Message mode, duplex, 512-byte buffers (`:2290-2296`).
- DACL `D:(A;;GA;;;WD)`: Everyone has GENERIC_ALL (`:2279`). **A non-admin process can connect.** `PIPE_REJECT_REMOTE_CLIENTS` is not set. Security consequence: any local process can send commands, including the crashing reload commands (see below).
- One client at a time, one command per connection. The server reads the command, handles it, then calls `DisconnectNamedPipe` (`:2062-2269`).
- The request is UTF-16LE, at most 127 wchar (`wchar_t buffer[128]`, `:2065`), and is matched by prefix with `wcsncmp`.
- Responses: while connected, `g_pipeHandle` is the client, so log lines are also written to the pipe as UTF-8 (`:1550`), but only when file logging is on. Data replies are written just before the disconnect, so the client must have a read pending before it writes, or the reply is lost.

### Commands [from-source:MIT]
| Command | Effect | Reply | Line |
|---------|--------|-------|------|
| `PING` | heartbeat | `PONG` (ASCII, no terminator) | `:2253` |
| `GETSETTINGS` | read logging flags | UTF-16 `SETTINGS DEBUG=true\|false LOG=true\|false` + NUL | `:2239` |
| `SETDISPLAYCOUNT <n>` | rewrites `<count>` in XML, then `ReloadDriver` | log lines only | `:2222` |
| `SETGPU "<name>"` | rewrites `<gpu><friendlyname>` (quotes are stripped), then `ReloadDriver` | log lines | `:2205` |
| `HDRPLUS`, `SDR10`, `CUSTOMEDID`, `PREVENTSPOOF`, `CEAOVERRIDE`, `HARDWARECURSOR` + ` true\|false` | rewrites that XML flag, then `ReloadDriver` | log lines | `:2107-2183` |
| `LOGGING`, `LOG_DEBUG` + ` true\|false` | toggle logging in XML (no reload) | log lines | `:2080-2106` |
| `GETASSIGNEDGPU`, `GETALLGPUS`, `D3DDEVICEGPU`, `IDDCXVERSION` | diagnostics | written to the log (and the pipe when logging is on) | `:2185-2204` |
| `RELOAD_DRIVER` | `ReloadDriver` | none | `:2075` |

There are **no** add-monitor, remove-monitor, add-resolution or get-state commands.

**`ReloadDriver` is broken** [from-source:MIT]. It calls `WdfObjectGet_IndirectDeviceContextWrapper(hPipe)` on the pipe HANDLE, which is not a WDF object (`:2053-2059`). Upstream issue #351 (open) reports an access violation in `FxObject::_GetObjectFromHandle`. The function also never calls `loadSettings()`. Settings are read only in `EvtDeviceAdd` (`:2844`), so every reload-type command effectively means: write XML, crash the driver host, let PnP restart the device, and have the settings re-read [hypothesis for the restart part]. Every VDD monitor then departs and re-arrives. The unlicensed Virtual-Driver-Control app blocks `RELOAD_DRIVER` but still sends `SETDISPLAYCOUNT` with a 3 s cooldown and a 45 s timeout. The code path is the same, so we cannot rely on it. The clean alternative is to write the XML and then run `pnputil /restart-device <Root\MttVDD instance>`, which needs admin, so it goes through the helper service (ADR-0004).

### Settings file [from-source:MIT]
- The folder is `HKLM\SOFTWARE\MikeTheTech\VirtualDisplayDriver\VDDPATH` if that value is set, otherwise `C:\VirtualDisplayDriver` (`:94`, `initpath` `:2360-2388`). The folder also holds `Logs\`, `user_edid.bin`, `adapter.txt` and `option.txt` (legacy fallback).
- Schema (sample `Virtual Display Driver (HDR)/vdd_settings.xml`):
  - `<monitors><count>`: one global count; 0 is treated as 1.
  - `<gpu><friendlyname>`: `default` falls back to `adapter.txt`. Otherwise it is matched case-insensitively against the DXGI adapter Description, or `"name,pciBus"` resolves the LUID (`Common/Include/AdapterOption.h:192-222`).
  - `<resolutions><resolution><width><height><refresh_rate>`: float Hz, converted to a vsync fraction.
  - `<global><g_refresh_rate>`: each global rate is added to **every** unique resolution (`:2640-2650`).
  - Also `<colour>` (SDR10bit, HDRPlus, ColourFormat), `<cursor>` (HardwareCursor, CursorMaxX/Y, AlphaCursorSupport), `<edid>` (CustomEdid, PreventSpoof, EdidCeaOverride), `hdr_advanced`, `auto_resolutions`, and `color_advanced/bit_depth_management` (force_bit_depth 8/10).
- Our modes fit: add 1920×1080, 2560×1080, 3840×1080 and 1920×2160 as `<resolution>` entries, plus `g_refresh_rate` 60 and 120.
- Edits take effect only on device (re)start. There is no file watcher.
- Folder ACL: the driver creates no ACL. A folder created directly under `C:\` inherits `Authenticated Users:(OI)(CI)(IO)(M)` from `C:\` ([verified-local] icacls on the test PC), so non-admin writes probably work [hypothesis]. Verify after install, because the installer may create the folder differently.

### How VDD monitors appear to Windows [from-source:MIT]
- Device: class Display, hardware ID `Root\MttVDD` (`MttVDD.inf:19`), provider "MikeTheTech", device "Virtual Display Driver", IddCx 1.2+ (`UmdfExtensions = IddCx0102`).
- One adapter. `FinishInit` creates `numVirtualDisplays` monitors, and all of them get the **same EDID** (`:3775-3860`).
- Built-in EDID (`:3464`): manufacturer `0x3694` = "MTT", product `0x1337`, serial `0x1EE71EE7`, monitor name descriptor **"VDD by MTT"**. `modifyEdid` forces MTT/0x1337 onto a custom EDID unless PreventSpoof is on (`:3485`).
- For CCD detection, match `DISPLAYCONFIG_TARGET_DEVICE_NAME.monitorFriendlyDeviceName == "VDD by MTT"`, or `monitorDevicePath` containing `MTT1337`. Also check the adapter's device path for `MttVDD`.
- Modes: every monitor reports the **same** list, which is the full XML list (`:4042`, `:4175`, `:4480`). Arbitrary modes are not possible. Each monitor can still be set to a different mode from that list with `SetDisplayConfig`.

### Answers to the M2 questions
- **Different resolutions at once:** possible, but only indirectly. Put all needed modes in the XML, set `count = 3`, then give each monitor its mode through CCD [from-source:MIT]. Adding or removing **one** monitor is impossible: any count change recreates all VDD monitors.
- **Ownership:** VDD monitors are anonymous and identical, so "only manage monitors we created" cannot be enforced if another app also uses VDD. The best we can do is count deltas.
- **Version check:** no pipe command reports the driver version. Read `DEVPKEY_Device_DriverVersion` of `Root\MttVDD` instead. The release INF has `DriverVer = 08/14/2025,22.50.22.79` [verified-local].
- **Releases:** the latest is `25.7.23` (2025-07-23; assets `VirtualDisplayDriver-x86.Driver.Only.zip` (x64), `-ARM64.Driver.Only.zip`, `VDD.Control.25.7.23.zip`). Main has had no newer release [from-source:MIT] (GitHub releases API).
- **winget:** `VirtualDrivers.Virtual-Display-Driver` 25.7.23 is a *portable zip of the Control app*. It does **not** install the driver [verified-local] (`winget show`, 2026-09-26).
- **Driver install:** create the root device `Root\MttVDD` from `MttVDD.inf`, as admin (for example `pnputil /add-driver` + device-node creation via SetupAPI or nefcon). Because the signature is non-WHQL, a silent install needs the SignPath signer in LocalMachine `TrustedPublisher` [hypothesis].
- **Uninstall:** remove the device, then `pnputil /delete-driver oemNN.inf /uninstall`.
- **Bootstrapper rule:** use only the signed official release asset (pinned version + SHA-256), never the repo's "Community Scripts". The antivirus on the test PC flagged `Community Scripts/virtual-driver-manager.ps1` (heuristic CMD:Heur.BZC.PZQ.Pantera) [verified-local].
- **Coexistence:** Parsec VDA (`Root\Parsec\VDA`), virtual-display-rs (`Root\VirtualDisplayDriver`) and VDD (`Root\MttVDD`) are separate root adapters with different pipes. The source shows no conflict [hypothesis until tested]. On the test PC, Parsec VDA 0.45.0.0 and virtual-display-rs 0.4.0.0 are installed and VDD is not [verified-local].

## virtual-display-rs control interface (chosen, ADR-0008)

Implemented in `src/XrealScreen.Display/VirtualDisplayRs/` and `xrs vdd state|add|clear`.

**[verified-hw] 2026-09-26 (glasses PC, driver 0.4.0.0):**
- The driver uses the monitor **id as the IddCx connector index; valid ids are 0–15** (`MAX_MONITORS = 16`, `context.rs:35/165`). Ids outside that range are accepted into the driver's state but fail with `Failed to create monitor: IddCx(CallFailed(NTSTATUS 0xC000000D))` in the Application event log (source `VirtualDisplayDriver`).
- Created together: 3840×1080@60, 1920×2160@60, 2560×1080@60, 1920×1080@120. Each appears as EDID `CHY0000`, friendly name "VirtuDisplay+", output technology HDMI, on its own IddCx adapter LUID (not the AMD render adapter's). Each offers only its own mode. Windows extends them to the right of the existing monitors.
- `Remove` by id restores the previous topology; other apps' monitors are untouched.
- CCD reports product code 0 for all of them, so we identify ours by comparing the active monitors before and after `Notify`, and by the name prefix `XrealScreen` in the driver state.

### Source facts
Source: MolotovCherry/virtual-display-rs main `22fcd2e0` (2024-12-10), crate `virtual-display-driver` 0.4.0. This matches the **0.4.0.0** driver (provider "Cherry", `Root\VirtualDisplayDriver`) installed on the test PC [verified-local]. The last GitHub release, v0.3.1 (2023), has an older, incompatible protocol.

- Pipe `\\.\pipe\virtualdisplaydriver`. Security: a **NULL DACL**, so any local user has access, and remote clients are rejected (`rust/virtual-display-driver/src/ipc.rs:127-180`) [from-source:MIT]. The pipe exists on the test PC [verified-local].
- Framing: UTF-8 serde-JSON messages, each terminated by `0x04` (EOT) (`ipc.rs:50`, `:208-246`). Multiple concurrent clients are allowed [from-source:MIT].
- Commands (`rust/driver-ipc/src/core.rs`, serde externally tagged) [from-source:MIT]:

  | Send | Effect |
  |------|--------|
  | `{"Notify":[Monitor…]}` | **Replaces the full list.** Monitors missing from the list are removed; monitors whose modes changed are re-arrived. |
  | `{"Remove":[id…]}` | Removes the given IDs. |
  | `"RemoveAll"` | Removes all monitors. |
  | `"State"` | Replies `{"State":[Monitor…]}` + EOT. |

  - `Monitor = {id:u32, name:string?, enabled:bool, modes:[{width,height,refresh_rates:[u32]}]}`.
  - The driver also broadcasts `{"Changed":[…]}` to all clients after every change.
- **Per-monitor modes and add/remove by ID without touching other monitors**, provided the client first reads `State` and merges. No admin rights and no restart are needed. `Notify` rejects duplicate width+height entries within one monitor. Refresh rates are integers only.
- EDID: manufacturer `0x0D19` = "CHY", name "VirtuDisplay+", **serial = monitor id** (`edid.rs`, `context.rs:150`). CCD can therefore map target → our id.
- Persistence: an optional JSON copy at `HKCU\SOFTWARE\VirtualDisplayDriver\data`, replayed at logon by `vdd-user-session-service`. We would not persist.
- Drawbacks: the install uses a self-signed certificate (ADR-0001), and the repo was last pushed 2025-03-03 [from-source:MIT] (GitHub API).

## Alternatives
| Driver | License | Notes |
|--------|---------|-------|
| Microsoft IddSampleDriver | MIT | IddCx 1.4 sample, `SwDeviceCreate`; unsigned, so we would have to sign it |
| virtual-display-rs (MolotovCherry) | MIT | Rust; per-monitor JSON pipe (above); self-signed install (used by VertoXR) |
| parsec-vdd | Driver binary owned by Parsec | Max 3 displays |
| Amyuni usbmmidd | Commercial | Licensing cost |

## Own driver (Later)
- Needs the WDK, which is not installed ([verified-local]).
- Dev: `bcdedit /set testsigning on` + test cert.
- Release: EV code-signing certificate + Microsoft Partner Center attestation signing.

## Pitfalls
- VDD may be shared with other apps, and its monitors are indistinguishable (see above). Check the driver version through PnP.
- Never send VDD reload-type commands from the app. Restart the device through the helper service instead.
- Virtual monitors must be on the same GPU adapter as the glasses output (hybrid laptops). For VDD, use `<gpu><friendlyname>`.
- Set virtual monitors to 100% scaling to avoid blurry capture.
