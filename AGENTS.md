# AGENTS.md

Instructions for any AI coding agent (Claude, Copilot, Codex, Cursor, …). Claude-specific details live in `CLAUDE.md`; the rules are the same.

## Resume protocol
1. Read `docs/STATUS.md` (where we are), `docs/ROADMAP.md` (exit criteria), `docs/ARCHITECTURE.md` (components).
2. Read the ADRs in `docs/decisions/` and the findings in `docs/findings/` for the area you touch.
3. Work in small steps; after each step: build, test, update `docs/STATUS.md`, commit.

## Build / test
```powershell
dotnet build XrealScreen.slnx -p:Platform=x64
dotnet test --solution XrealScreen.slnx
```
Requirements: .NET SDK 10.0.401+ (`global.json`), Windows 11, Windows SDK 26100. See `docs/findings/local-environment.md`.

## Roles
Role definitions (owned paths, required reading) are in `.claude/agents/*.md`. Non-Claude agents should read the file for the role they act in.

| Role | Owns |
|------|------|
| architect | ADRs, interfaces in Core, ARCHITECTURE.md |
| winui-dev | `src/XrealScreen.App` |
| device-protocol | `src/XrealScreen.Device.*`, `tools/XrealScreen.Cli`, protocol findings, fixtures |
| graphics-engineer | Render (M3): capture, D3D11, latency |
| display-driver-engineer | Display (M2), VDD, CCD, Service (M6) |
| qa-engineer | `tests/`, simulator scenarios, `docs/testing/` |
| tech-writer | STATUS/ROADMAP hygiene, pruning stale docs |
| legal-security-reviewer | SOURCES, clean-room compliance, privacy, ACLs (read-only) |

## Non-negotiable rules
- Clean room: no code from GPL sources, the XREAL SDK, or VertoXR. See ADR-0006.
- Findings carry a verification tag (see `docs/findings/README.md`).
- Remove or supersede wrong/outdated documentation instead of appending contradictions.
