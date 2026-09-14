# Project Structure

This repository follows the conventions of modern .NET projects.

## Layout

```
WoLock/
├── .editorconfig              # Code style rules (applied to all files)
├── global.json                # Pins the .NET SDK version
├── Directory.Build.props      # Shared MSBuild properties (nullable, warnings, etc.)
├── Directory.Packages.props   # Central package version management (CPM)
├── WoLock.slnx                # Solution file (.NET 10 native format)
├── src/
│   ├── WoLock.Core/           # Cross-platform Wake-on-LAN library
│   ├── WoLock.Gui/            # Avalonia GUI (Windows/Linux, Android-ready)
│   └── WoLock.Tui/            # Terminal.Gui TUI + CLI (Linux/Windows)
├── scripts/                   # Helper scripts (e.g. legacy PowerShell WoL script)
├── assets/img/                # Central image assets (logo, icon, scene)
├── docs/                      # Documentation
└── .github/workflows/         # CI pipelines
```

## Shared files (repository root)

- **`global.json`** — pins the .NET SDK version and controls roll-forward behavior.
- **`Directory.Build.props`** — MSBuild properties applied to every project
  (e.g. `Nullable`, `ImplicitUsings`, `LangVersion`, warning handling, doc generation)
  and NBGV configuration.

## Code conventions

- **SOLID** (where possible), **DRY**, **YAGNI**.
- **Dependency Injection** — cleanly injected dependencies, no global singletons in the core.
- **Testing** — XUnit unit tests for critical logic.
- **`Directory.Packages.props`** — Central Package Management (CPM). Package versions
  are declared once; projects only reference the package `Include` without a version.
- **`.editorconfig`** — formatting and C# code style rules.

## Source projects (`src/`)

- **`WoLock.Core`** — the reusable library (magic packet, UDP sending, config, smart-guess).
- **`WoLock.Gui`** — Avalonia UI application (Windows/Linux desktop, Android-ready).
- **`WoLock.Tui`** — Terminal.Gui application plus CLI (Linux/Windows).
- **`tests/WoLock.Core.Tests`** — XUnit tests for `WoLock.Core` (48 passing tests covering magic packet, MAC parsing, config, smart-guess, factory, sender, ping).
- **`WoLock.Gui`** uses `assets/img/WoLock_Icon.ico` as its application/window title bar icon.

Each project produces its own executable (`WoLock.Gui`, `WoLock.Tui`).

## Assets (`assets/img/`)

Central image repository (source of truth for all graphics):

- **`WoLock_Logo.png`** — logo, used in the README, docs and the GUI About/splash screen.
- **`WoLock_Scene.png`** — scene/banner, used as the README hero image.
- **`WoLock_Icon.ico`** — Windows icon resource (multiple sizes). This file is used as the
  **`WoLock.Gui` application icon and window title-bar icon** (copied into the GUI project
  as a resource).
- **`WoLock_Icon.png`** — PNG icon variant.

The GUI project references the `.ico` as its application/window icon; Android gets its own
icon densities in the Android project later.

## Documentation (`docs/`)

- `PROJECT_STRUCTURE.md` — this file (project & documentation layout).
- Additional design/decision documents can be added here as the project grows.

## CI (`.github/workflows/`)

- Build verification on push/PR (Windows + Linux runners).
