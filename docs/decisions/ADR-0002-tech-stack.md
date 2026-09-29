# ADR-0002: WinUI 3 + .NET 10 + Windows App SDK + CommunityToolkit.Mvvm + Vortice

- **Status:** Accepted
- **Date:** 2026-09-26

## Context
Modern Windows-only desktop app with a low-latency D3D11 renderer, Windows.Graphics.Capture, CCD P/Invoke, and a small service. Machine has .NET SDK 10.0.401, VS 2022 + VS 18 Insiders, Windows SDK 26100 ([verified-local]).

## Decision
- UI: **WinUI 3** on **Windows App SDK** (latest stable, verify on NuGet), MVVM via **CommunityToolkit.Mvvm 8.x**.
- Runtime: **.NET 10**, TFM `net10.0-windows10.0.26100.0`, min 10.0.22621, x64 + ARM64.
- Graphics/interop: **Vortice.Windows 3.8.x** (Direct3D11, DXGI) + CsWinRT projections for WGC.
- Tests: **xUnit**; CLI: **System.CommandLine**.
- Central package management (`Directory.Packages.props`), SDK pinned in `global.json`.

## Consequences
- + Fluent UI (Mica, NavigationView), first-party packaging (MSIX), modern C#.
- + Vortice gives thin, maintained bindings; no C++ layer.
- − WinUI 3 does not render into our swapchain window efficiently for 3D; the renderer uses its own Win32 fullscreen window on the glasses output.
- − Windows 11 22H2+ only.

## Alternatives
- WPF — older look, no Mica/WinUI controls natively.
- Flutter/Rust (VertoXR's stack) — split-language, weaker Windows API access from UI.
- C++/WinRT renderer — more control, higher maintenance; revisit only if Vortice latency is inadequate (M3).
- SharpDX — unmaintained.
