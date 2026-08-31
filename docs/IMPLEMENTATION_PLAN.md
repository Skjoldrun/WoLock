# Implementation Plan

Detailed, step-by-step plan for building **WoLock**. This document is the working
reference for incremental development and can be used as context in new sessions.

> See [../README.md](../README.md) for the authoritative project definition.
> See [PROJECT_STRUCTURE.md](./PROJECT_STRUCTURE.md) for the repository layout.

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
- [ ] Create the solution `WoLock.sln` at the repository root.
- [ ] Create project `src/WoLock.Core/WoLock.Core.csproj` (class library, `net10.0`).
- [ ] Add projects to the solution (`WoLock.Core`).
- [ ] Add `.editorconfig` rules (already present at repo root).

#### Core domain
- [ ] `MagicPacket` — build the 102-byte magic packet (6× `0xFF` + MAC ×16).
- [ ] `MacAddress` — parse/validate MAC strings (e.g. `84:47:09:88:78:56`).
- [ ] `WakeRequest` — model: MAC, target IP(s), port(s), NIC, broadcast flag.
- [ ] `WakeSender` — async UDP sending with `EnableBroadcast`, configurable port/IP(s).
- [ ] Support sending to **multiple IPs and ports** (as in the legacy PowerShell script).

#### Cross-platform
- [ ] Use `System.Net.Sockets.UdpClient` (available on all target platforms).
- [ ] Clear feedback when `SO_BROADCAST` cannot be set or sending fails (log + throw).
- [ ] Windows + Linux (Ethernet) supported; document limitations.

### Context (from planning)
- Legacy script: `src/scripts/Send-WakeOnLan.ps1`
  - MAC `84:47:09:88:78:56`; IPs `255.255.255.255`, `172.19.1.255`, `172.19.1.87`, `172.19.1.86`; ports `9` and `7`.
- Magic packet = 6× `0xFF` + MAC ×16 = 102 bytes; `EnableBroadcast = true`.
- API is **async-first** (`SendWakePacketAsync`); sync wrapper optional.

---

## Milestone 2 — Configuration & smart-guess

Goal: load devices/profiles from `appsettings.json` with intelligent defaults.

### Steps
- [ ] Add `Serilog` (Console + optional File sink) to all projects via CPM.
- [ ] Configure Serilog in `Directory.Build.props`-driven startup (DEBUG/INFO, non-persistent by default).
- [ ] Define config model: `DeviceConfig`, `ProfileConfig`, root `WoLockConfig`.
- [ ] Load config from `appsettings.json` (per app, with environment override).
- [ ] Implement **smart-guess**: detect local NIC IP → derive subnet → broadcast address.
- [ ] Config values override smart-guess defaults where provided.
- [ ] Support **multiple profiles** (optional); fall back to the detected profile when none defined.

### Context (from planning)
- Non-configured values are derived intelligently (e.g. broadcast from local NIC IP).
- Per device (generic, unlimited): MAC (required), target/broadcast IP, UDP port, NIC/interface, multiple IPs.
- Values stored **plain** in `appsettings.json` (no encryption).

---

## Milestone 3 — `WoLock.Tui` (Terminal.Gui + CLI)

Goal: interactive terminal UI plus CLI flags for automation.

### Steps
- [ ] Create project `src/WoLock.Tui/WoLock.Tui.csproj` (executable, `net10.0`).
- [ ] Reference `WoLock.Core`.
- [ ] Add `Terminal.Gui` via CPM.
- [ ] Interactive menu: select device → wake (keyboard-driven).
- [ ] CLI flags (e.g. `wol wake <device>`, `wol list`).
- [ ] Status output: packet sent / device responds / no response.
- [ ] Detailed logs on demand (extended view).

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
- [ ] Create test project `tests/WoLock.Core.Tests/WoLock.Core.Tests.csproj` (XUnit).
- [ ] Reference `WoLock.Core`.
- [ ] Tests for `MagicPacket` (102-byte layout, MAC ×16).
- [ ] Tests for `MacAddress` parsing/validation.
- [ ] Tests for `WakeRequest` defaults and validation.
- [ ] Tests for config loading and smart-guess (local NIC → subnet → broadcast).
- [ ] Wire tests into CI (`dotnet test`).

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

- [ ] Android SDK/emulator available? (for later APK)
- [ ] Should the TUI CLI run standalone without the interactive UI?

---

## How to use this document

- Work through milestones/steps in order; mark `[x]` as steps complete.
- Keep this document updated so new sessions can pick up context here.
- Any architecture/behavior change must be reflected in
  [../README.md](../README.md) (authoritative) and here.
