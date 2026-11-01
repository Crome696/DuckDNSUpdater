# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Branding assets under `assets/` (horizontal logo, brandmark, app icon)
- Windows application and window icon (`assets/app-icon.ico`)

### Changed

- Upgraded from .NET 8 to .NET 10 (LTS); .NET 8 reaches end of support in November 2026

### Fixed

- Stopping and immediately restarting the updater could start a second update loop while the first was still shutting down
- A `config.json` with `null` for `domain` or `token` threw an exception instead of being reported as invalid

## [1.0.0]

### Added

- Portable .NET 8 WinForms DuckDNS updater for Windows x64
- Configurable domain, token, and update interval via `config.json`
- Auto-start option, Start/Stop controls, and in-app status/log view
- Self-contained single-file publish
- Option to write logs to `duckdns-updater.log` next to the EXE (`writeLogsToFile`)
- XML documentation comments on public types and members
- English README and Changelog
- Unit tests and E2E tests (HTTP-mocked pipeline and FlaUI UI smoke) in `tests/DuckDNSUpdater.Tests`

### Changed

- UI, log messages, and validation errors are English
- Main window is slightly wider and taller; log area uses full width
- Assembly version set to 1.0.0
