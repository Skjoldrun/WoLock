# Implementation Plan

Detailed, step-by-step plan for building **WoLock**. This document is the working
reference for incremental development and can be used as context in new sessions.

> See [../README.md](../README.md) for the authoritative project definition.
> See [PROJECT_STRUCTURE.md](./PROJECT_STRUCTURE.md) for the repository layout.

---

## Progress (last updated: 2026-09-14)

| Milestone | Status | Notes |
|-----------|--------|-------|
| 1 — Scaffold & Core | ✅ implemented | `WoLock.Core` builds on `net10.0`. Solution exists as `WoLock.slnx` (.NET 10 native format). |
| 2 — Config & smart-guess | ✅ implemented | `DeviceConfig` / `ProfileConfig` / `WoLockConfig`, `ConfigLoader`, `BroadcastResolver` (smart-guess), Serilog via `WoLockLogger`. |
| 3 — TUI + CLI | ✅ implemented + tested | CLI (`list`, `wake`, `--ping`, `--timeout`, `--config`, `--help`) and the interactive Terminal.Gui UI render and run. |
| 4 — GUI (Avalonia) | ⬜ not started | |
| 4b — Unit tests | ✅ implemented | `tests/WoLock.Core.Tests` covers `MacAddress`, `MagicPacket`, `WakeRequest`, `WakeRequestFactory`, `ConfigLoader`, `BroadcastResolver`, `WakeSender`, `PingChecker` (48 tests). CI runs `dotnet test`. |
| 5 — Packaging | ⬜ not started | |

Implemented and verified this session: correct hex MAC parsing, graceful `--ping` when ICMP is unavailable, `--help` no longer launches the TUI, and the test project's CPM fixed (whole solution restores/builds, placeholder test passes).

---

## 0. Prerequisites & Conventions

- **.NET 10** (`net10.0`) — see `global.json`.
- **Language:** all code, comments, names, and documentation in **English**.
- **Versioning:** `NergetGitVersioning` (git-based, tag prefix `v`).
- **Packages:** Central Package Management (CPM) — versions only in
  `Directory.Packages.props`; projects reference packages without a version.
- **Build:** `Directory.Build.props` applies `Nullable`, `ImplicitUsings`,
  `TreatWarningsAsErrors`, and documentation file generation.
- **CI:** `.github/workflows/ci.yml` builds on Windows + Linux.
- **Code principles:** SOLID (where possible), DRY, YAGNI.
- **Dependency Injection:** projects use cleanly injected dependencies (no global singletons in the core).
- **Testing:** unit tests with **XUnit**; critical logic (magic packet, MAC parsing, config, smart-guess) is tested.

---

## Milestone 1 — Solution scaffold & `WoLock.Core` foundation

Goal: working cross-platform Wake-on-LAN library with magic packet + UDP sending.

### Steps
- [x] Create the solution at the repository root (`WoLock.slnx`, .NET 10 native format).
- [x] Create project `src/WoLock.Core/WoLock.Core.csproj` (class library, `net10.0`).
- [x] Add projects to the solution (`WoLock.Core`, `WoLock.Tui`, `WoLock.Core.Tests`).
- [x] Add `.editorconfig` rules (already present at repo root).

#### Core domain
- [x] `MagicPacket` — build the 102-byte magic packet (6× `0xFF` + MAC ×16).
- [x] `MacAddress` — parse/validate MAC strings (e.g. `84:47:09:88:78:56`). Parses hex octets with `NumberStyles.HexNumber`.
- [x] `WakeRequest` — model: MAC, target IP(s), port(s), NIC, broadcast flag.
- [x] `WakeSender` — async UDP sending with `EnableBroadcast`, configurable port/IP(s).
- [x] Support sending to **multiple IPs and ports** (as in the legacy PowerShell script).

#### Cross-platform
- [x] Use `System.Net.Sockets.UdpClient` (available on all target platforms).
- [x] Clear feedback when `SO_BROADCAST` cannot be set or sending fails (log + throw).
- [x] Windows + Linux (Ethernet) supported; document limitations.

### Context (from planning)
- Legacy script: `src/scripts/Send-WakeOnLan.ps1`
  - MAC `84:47:09:88:78:56`; IPs `255.255.255.255`, `172.19.1.255`, `172.19.1.87`, `172.19.1.86`; ports `9` and `7`.
- Magic packet = 6× `0xFF` + MAC ×16 = 102 bytes; `EnableBroadcast = true`.
- API is **async-first** (`SendWakePacketAsync`); sync wrapper optional.

---

## Milestone 2 — Configuration & smart-guess

Goal: load devices/profiles from `appsettings.json` with intelligent defaults.

### Steps
- [x] Add `Serilog` (Console + optional File sink) to all projects via CPM.
- [x] Configure Serilog in startup (DEBUG/INFO, non-persistent by default) via `WoLockLogger.Configure`.
- [x] Define config model: `DeviceConfig`, `ProfileConfig`, root `WoLockConfig`.
- [x] Load config from `appsettings.json` (per app, with `WOLOCK_CONFIG` env override).
- [x] Implement **smart-guess**: detect local NIC IP → derive subnet → broadcast address (`BroadcastResolver`).
- [x] Config values override smart-guess defaults where provided (`WakeRequestFactory`).
- [x] Support **multiple profiles** (optional); fall back to the detected profile when none defined.

### Context (from planning)
- Non-configured values are derived intelligently (e.g. broadcast from local NIC IP).
- Per device (generic, unlimited): MAC (required), target/broadcast IP, UDP port, NIC/interface, multiple IPs.
- Values stored **plain** in `appsettings.json` (no encryption).

---

## Milestone 3 — `WoLock.Tui` (Terminal.Gui + CLI)

Goal: interactive terminal UI plus CLI flags for automation.

### Steps
- [x] Create project `src/WoLock.Tui/WoLock.Tui.csproj` (executable, `net10.0`).
- [x] Reference `WoLock.Core`.
- [x] Add `Terminal.Gui` via CPM.
- [x] Interactive menu: select device → wake (keyboard-driven).
- [x] CLI flags (`wol wake <device>`, `wol list`, `--ping`, `--timeout`, `--config`, `-h/--help`).
- [x] Status output: packet sent / device responds / no response.
- [x] Detailed logs on demand (extended view).

### Context (from planning)
- Primary: Linux (CachyOS, Wayland) + Windows.
- Two modes: interactive (menu) + CLI (automation).
- Primary status display; detailed output only on request.

---

## Milestone 4 — `WoLock.Gui` (Avalonia)

Goal: graphical UI (Windows/Linux desktop, Android-ready).

### Steps
- [ ] Create project `src/WoLock.Gui/WoLock.Gui.csproj` (executable, `net10.0`).
- [ ] Reference `WoLock.Core`.
- [ ] Add `Avalonia` (Desktop; Android optional later) via CPM.
- Use dependency injection for services (wake service, config, logging).
- [ ] Base layout with **Dark theme** (primary) + **Light theme** toggle.
- [ ] Device list + wake action.
- [ ] Progress bar + textual status + status icon.
- [ ] Optional second window with detailed logs.
- [ ] Windows **tray icon** + optional **auto-start** (Windows only).

### Context (from planning)
- Focus: Windows + Linux desktop; Android-ready.
- Success detection: ping the target after wake, wait for ICMP reply (note: ICMP may be blocked).
- Windows: optional tray + auto-start; Linux/Android: no tray logic.

---

## Milestone 4b — Unit tests (XUnit)

Goal: cover the critical logic of `WoLock.Core`.

### Steps
- [x] Create test project `tests/WoLock.Core.Tests/WoLock.Core.Tests.csproj` (XUnit).
- [x] Reference `WoLock.Core`.
- [x] Tests for `MagicPacket` (102-byte layout, MAC ×16).
- [x] Tests for `MacAddress` parsing/validation.
- [x] Tests for `WakeRequest` defaults and validation.
- [x] Tests for config loading and smart-guess (local NIC → subnet → broadcast).
- [x] Tests for `WakeRequestFactory` (ports, targets, broadcast, additional IPs).
- [x] Tests for `WakeSender` (via injected `IWakeUdpClient` factory).
- [x] Tests for `BroadcastResolver` subnet math (`ComputeBroadcast`, `MaskFromPrefix`).
- [x] Tests for `PingChecker` (IPv4 validation).
- [x] Wire tests into CI (`dotnet test`).

> 48 tests, all passing. Two bugs found and fixed while writing tests:
> `BroadcastResolver.MaskFromPrefix(0)` returned `255.255.255.255` (C# shift-by-32 wrap),
> and `WakeRequestFactory` ignored `additionalIps` when a `targetIp` was set.

## Milestone 5 — Refinement & packaging

Goal: polished, distributable builds.

### Steps
- [ ] Single-file self-contained builds (Windows/Linux).
- [ ] Verify versions via `NergetGitVersioning` (tags `vX.Y.Z`).
- [ ] Finalize README + docs.
- [ ] (Optional) Android APK once Android SDK/emulator is available.

### Context (from planning)
- Packaging: single-file self-contained Exes; Android APK separately.
- Target OSes tested: Windows 10/11, Windows Server 2019, CachyOS, DietPi.

---

## Open items (to clarify)

- [x] Replace the placeholder unit test with real tests for `MagicPacket`, `MacAddress`, `WakeRequest`, config loading and smart-guess.
- [ ] Android SDK/emulator available? (for later APK)
- [ ] Android SDK/emulator available? (for later APK)
- [ ] Should the TUI CLI also run standalone without the interactive UI?

---

## How to use this document

- Work through milestones/steps in order; mark `[x]` as steps complete.
- Keep this document updated so new sessions can pick up context here.
- Any architecture/behavior change must be reflected in
  [../README.md](../README.md) (authoritative) and here.
