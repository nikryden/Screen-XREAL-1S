# Screen-XREAL-1S

Windows control helper for XREAL One S glasses.

## Windows usage

Run the PowerShell helper to quickly switch display modes when the glasses are connected:

```powershell
.\windows-control-xreal-one-s.ps1 -Mode second-screen-only
```

Supported modes:

- `second-screen-only` (default): route display output to the glasses
- `extend`: extend desktop to glasses
- `duplicate`: mirror primary desktop to glasses

To return to laptop/internal display:

```powershell
.\windows-control-xreal-one-s.ps1 -RevertToInternal
```