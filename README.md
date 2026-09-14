![WarLock Logo](/assets/img/WoLock_Logo.png)

# WoLock

[![CI](https://github.com/Skjoldrun/WoLock/actions/workflows/ci.yml/badge.svg)](https://github.com/Skjoldrun/WoLock/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/Skjoldrun/WoLock)](LICENSE)

A small, cross-platform **Wake-on-LAN (WoL)** tool for .NET.

## 1. Objective

WoLock wakes devices using a WoL "magic packet". It consists of:

- **`WoLock.Core`** — a cross-platform library with the WoL functions.
- **`WoLock.Gui`** — a graphical interface (Avalonia UI), primarily Windows, Android-ready.
- **`WoLock.Tui`** — a terminal interface (Terminal.Gui) plus CLI for Linux/Windows.

Both the GUI and the TUI use the same `WoLock.Core` library.

> **Source of truth:** This README is the authoritative definition of the project.
> Any changes to architecture/behavior should be documented here.

---

## 2. Conventions

### 2.1 Language
- **Code, comments, names, and all documentation are written in English.**
- This applies to `WoLock.Core`, `WoLock.Gui`, `WoLock.Tui`, as well as all configuration and documentation files.

### 2.2 Project/documentation structure
- The project and documentation structure follows modern .NET project conventions
  (`global.json`, `Directory.Build.props`, central package management
  `Directory.Packages.props`, `.editorconfig`, `src/` layout, `.github/` CI).
- See [docs/PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md) for details.

### 2.3 Code principles
- **SOLID** — followed where possible.
- **DRY** (Don't Repeat Yourself) — no redundancies.
- **YAGNI** (You Aren't Gonna Need It) — no speculative implementation.
- **Dependency Injection** — projects work cleanly via injected dependencies; no global singletons in the core.
- **Testing** — unit tests with **XUnit**; critical logic (magic packet, MAC parsing, config, smart-guess) is tested.

## 3. Context / Starting point

Currently, a server (**MUNIN**) is woken via a PowerShell script:

- Script: `src/scripts/Send-WakeOnLan.ps1`
- **MAC:** `84:47:09:88:78:56` (Ethernet NIC)
- **Target IPs:** `255.255.255.255`, `172.19.1.255`, `172.19.1.87`, `172.19.1.86`
- **Ports:** 9 and 7
- **Magic packet:** 6× `0xFF` + MAC ×16 = 102 bytes, `EnableBroadcast = true`

This behavior (multiple IPs + ports, broadcast + unicast) is carried over into `WoLock`.

---

## 4. Tech stack (decided)

| Area              | Decision                                                      |
|-------------------|---------------------------------------------------------------|
| .NET version      | **.NET 10** (`net10.0`)                                       |
| GUI               | **Avalonia UI** (Dark theme primary, Light theme toggle)      |
| TUI               | **Terminal.Gui** (v2), interactive + CLI                      |
| Logging           | **Serilog** (level DEBUG/INFO, non-persistent, file optional) |
| Packaging         | **Single-File Self-Contained** (Exes); Android APK separate   |
| Project structure | **Multi-project solution** in one repo                        |

### Tested target OSes
Windows 10, Windows 11, Windows Server 2019, CachyOS (Linux), DietPi (Linux).

---

## 5. Project structure

```
WoLock/
├── WoLock.Core/        # Cross-platform WoL library (magic packet, config, smart-guess)
├── WoLock.Gui/         # Avalonia GUI (Windows/Linux desktop, Android-ready)
└── WoLock.Tui/         # Terminal.Gui TUI + CLI (Linux/Windows)
```

- **Separate executables:** `WoLock.Gui` and `WoLock.Tui` (each buildable independently).
- Namespace: **`WoLock`** (e.g. `WoLock.Core`, `WoLock.Gui`, `WoLock.Tui`).

---

## 6. WoLock.Core — library

### 6.1 Responsibilities
- Magic packet generation (6× `0xFF` + MAC ×16 = 102 bytes).
- UDP sending with `EnableBroadcast`, configurable port/target IP(s).
- **Smart-guess:** automatic derivation of local NIC IP → subnet → broadcast address.
- Config loading from `appsettings.json`.
- Optional ping-based success check (see 6.3).

### 6.2 API style
- **Async-first:** `SendWakePacketAsync(...)`.
- Sync wrapper optional.

### 6.3 Success check (optional)
- After waking, the target can be **pinged**; waits for a successful ping (accounting for boot time).
- **Limitation:** ICMP may be blocked (firewall/BIOS) → ping is realistic but not a 100% guarantee.
- Status is shown in GUI/TUI ("packet sent" / "device responds" / "no response").

### 6.4 Cross-platform note
- Windows + Linux (Ethernet) are supported.
- On problems (e.g. `SO_BROADCAST` cannot be set, VPN interfaces) there is **clear feedback** (error message/log).

---

## 7. Configuration (`appsettings.json`)

### 7.1 Smart-guess principle
- Non-configured values are **derived intelligently** (e.g. broadcast address from local NIC IP).
- Config values **override** only where needed.

### 7.2 Devices (generic, unlimited)
Per device configurable (each with smart-guess default, overrideable):

| Field               | Smart-guess default             | Overrideable |
|---------------------|---------------------------------|--------------|
| MAC address         | — (required)                    | yes          |
| Target/broadcast IP | local NIC IP → subnet broadcast | yes          |
| UDP port            | 9 (fallback 7)                  | yes          |
| NIC / interface     | active Ethernet NIC             | yes          |
| Multiple IPs        | broadcast + local unicast IPs   | yes          |

### 7.3 Profiles
- If **profiles** are defined → selectable (menu/flag).
- If **none** are defined → automatically the detected network profile.

### 7.4 Security
- Values stored **plain** in `appsettings.json` (not encrypted).

---

## 8. WoLock.Gui — Avalonia UI

### 8.1 Platforms
- **Focus:** Windows + Linux desktop.
- **Android-ready** (as a later stage; APK separate, see 8.5).

### 8.2 Theme
- **Dark theme** primary, **Light theme** toggle.

### 8.3 Status display
- **Progress bar** + textual status display + icon per status.
- Optional **second window** with detailed logs (on request only).

### 8.4 Auto-start / tray
- **Windows:** optional **tray icon** + **auto-start** (registry auto-run).
- **Linux/Android:** no special tray logic.

### 8.5 Android
- Distributable as **APK** (separate step, once Android SDK/emulator is available).

---

## 9. WoLock.Tui — Terminal.Gui + CLI

### 9.1 Mode
- **Interactive** (menu-based, keyboard-driven) as the primary interface.
- **CLI flags** for automation (e.g. `wol wake MUNIN`).

### 9.2 Display
- Primary user output with recognizable status.
- Detailed output **on request** (extended view).

### 9.3 Platform
- Linux (CachyOS/Wayland) + Windows.

---

## 10. Logging (Serilog)

- Level **DEBUG/INFO**.
- **Non-persistent** (primary focus: visible at runtime).
- **File optional.**
- Wake attempts are logged in a traceable way.

---

## 11. Packaging

- **Single-File Self-Contained** Exes (Windows/Linux).
- **Android:** APK (separate, once SDK is available).

---

## 12. Milestones / plan

The detailed implementation plan with phases, individual steps, and additional context
can be found in [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md).
It is the working basis for incremental development and can be used as
context in new sessions.

### Milestone 1 — Scaffold & Core
- [x] Create `WoLock.Core` class library (`net10.0`).
- [x] Magic packet generation in `WoLock.Core`
- [x] UDP sending (Async) with broadcast + configurable port/IP
- [x] Create solution (`WoLock.slnx`, .NET 10 native format)

### Milestone 2 — Configuration & smart-guess
- [x] `appsettings.json` model (devices + profiles)
- [x] Smart-guess: local NIC IP → subnet → broadcast derivation
- [x] Override via config
- [x] Set up Serilog (non-persistent)

### Milestone 3 — TUI (Terminal.Gui + CLI)
- [x] Interactive menu (select device, wake)
- [x] CLI flags (`wol wake <device>`, `wol list`, `--ping`, `--timeout`, `--config`, `--help`)
- [x] Status display (packet sent / device responds / no response)

> **Current status:** Milestones 1–3 are implemented, build cleanly, and the CLI + interactive TUI have been verified. Milestone 4b (unit tests) is now implemented: `tests/WoLock.Core.Tests` contains 48 passing tests covering the core logic, and CI runs `dotnet test`. Milestones 4 (GUI) and 5 (packaging) are still pending.

### Milestone 4 — GUI (Avalonia)
- [ ] Base layout + Dark/Light theme
- [ ] Device list, wake action, progress bar + status display + icon
- [ ] Optional log window
- [ ] Windows tray + auto-start (optional)

### Milestone 5 — Refinement & packaging
- [ ] Single-File Self-Contained builds
- [ ] Refine README/docs
- [ ] (Optional) Android APK, once SDK is available

---

## 13. Open items (to clarify)

- [ ] Android SDK/emulator available? (for later APK)
- [ ] Should the TUI CLI also run standalone without the TUI library?

---

*Status: Milestones 1–3 implemented and verified (see [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md) for the detailed progress table). This document remains the source of truth for the implementation.*

*Important Rules:* 
- git commits are only done by the User himself