# Changelog

All notable changes to this project will be documented in this file.

## [1.0.1] - 2026-11-28
### Changed
- **CKAN Compliance & Single-Directory `GameData` Layout**:
  - Moved the bundled Realism Overhaul / RealAntennas ASOG patch from `GameData/GravityIssue/Patches/RO_config.cfg` into `GameData/ASDSManager/Patches/ASOG_RO_config.cfg` to prevent cross-mod directory writes and CKAN file conflicts.
  - Added duplicate-module guards (`!MODULE[ModuleFuelTanks],* {}`, `!MODULE[ModuleRealAntenna],* {}`) to `ASOG_RO_config.cfg` for safe upgrades from v1.0.0.
  - Standardized release ZIP packaging so `ASDS-Manager-v1.0.1.zip` contains strictly `GameData/ASDSManager/` (plus bundled `LICENSE`, `README.md`, and `ASDSManager.version`), excluding `Source/` and `Tools/`.
### Added
- Added KSP-AVC version file (`GameData/ASDSManager/ASDSManager.version`) for automated CKAN version & KSP compatibility detection (`1.12.0` – `1.12.99`).
- Added CKAN NetKAN specification (`NetKAN/ASDSManager.netkan`) referencing `ModuleManager` and `GravityIssue`.
- Added `-PackageRelease` flag to `Tools/build.ps1` to generate clean CKAN-compliant release archives.

## [1.0.0] - 2026-11-25
### Added
- Initial public release of **ASDS-Manager** for KSP 1.12.x.
- Kinematic ocean surface stabilization and horizon/heading lock (`ModuleBargeAnchor`).
- Remote background repositioning synchronizing runtime `Vessel`, `OrbitDriver`, and persistent `ProtoVessel` (`BargeManager`).
- Automatic kOS prediction coordinate sync from `asds_predicted.ks` (`PredictionFileReader`).
- Stock ApplicationLauncher toolbar UI (`ToolbarUI`) with pure English UI and `CultureInfo.InvariantCulture` parsing.
- ModuleManager patches for `GravityIssue` ASOG/Jacklyn and Realism Overhaul 1:1 ASOG scaling.
