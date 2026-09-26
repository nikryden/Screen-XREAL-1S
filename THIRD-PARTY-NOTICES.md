# Third-party notices

XrealScreen is MIT-licensed. This file lists third-party components it uses or distributes. **Placeholder** — full license texts are added in M7 (Packaging).

## Ported code
| Component | License | Where | What |
|-----------|---------|-------|------|
| [Skarian/one-xr](https://github.com/Skarian/one-xr) - Copyright (c) 2026 Neil Skaria | MIT | `src/XrealScreen.Device.XrealOne/OneReportFramer.cs` | Stream framing and 128-byte report field offsets (re-implemented in C#) |

one-xr MIT License: Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Planned dependencies
| Component | License | Distribution |
|-----------|---------|--------------|
| Windows App SDK | MIT | NuGet / framework package |
| CommunityToolkit.Mvvm (and other CommunityToolkit packages) | MIT | NuGet |
| Vortice.Windows (Direct3D11, DXGI, …) | MIT | NuGet |
| System.CommandLine | MIT | NuGet (tools only) |
| xUnit | Apache-2.0 | NuGet (tests only, not distributed) |
| VirtualDrivers/Virtual-Display-Driver | MIT | Installed by the bootstrapper; **not bundled in the MSIX** |

## Development-only content
| Component | License | Location |
|-----------|---------|----------|
| `csharp-xunit` Claude skill (github/awesome-copilot) | MIT | `.claude/skills/csharp-xunit/` |
| `winui-app` Claude skill | Apache-2.0 | `.claude/skills/winui-app/` |
