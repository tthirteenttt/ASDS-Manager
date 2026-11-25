# ASDS-Manager

[![KSP Version](https://img.shields.io/badge/KSP-1.12.x-brightgreen.svg)]()
[![Compatibility](https://img.shields.io/badge/RSS%20%2F%20RO%20%2F%20Stock-Compatible-blue.svg)]()
[![License](https://img.shields.io/badge/License-MIT-green.svg)]()

**ASDS-Manager** is a KSP 1.12.x plugin for remote Autonomous Spaceport Drone Ship (ASDS) deployment and kinematic attitude locking. It is designed to pair seamlessly with [kOS-BoosterRecovery](https://github.com/tthirteenttt/kOS-BoosterRecovery), automatically reading predicted landing coordinates from `estimate.ks` and locking barges (such as ASOG, OCISLY, JRTI, or Jacklyn) flat and stationary on the ocean surface.

---

## Features

1. **Automatic kOS Prediction Sync**:
   - Reads predicted coordinates directly from `<KSP>/Ships/Script/config/asds_predicted.ks` (compatible with `kOS-BoosterRecovery` v1.0+, with automatic fallback to legacy `Ships/Script/Source/config/asds_predicted.ks`).
   - Accessible from the stock KSP Application Launcher toolbar in Flight, Space Center, and Tracking Station scenes.
2. **Background Remote Repositioning**:
   - Teleports unloaded or loaded ASDS vessels directly to target latitude/longitude without switching vessels or leaving the launchpad.
   - Rebuilds both runtime `Vessel` / `OrbitDriver` and persistent `ProtoVessel` snapshots so the barge loads cleanly when entering physics range.
3. **Kinematic Deck Stabilization & Heading Lock (`ModuleBargeAnchor`)**:
   - Freezes barge rigidbodies (`isKinematic = true`) and zeroes linear/angular drift to eliminate wave pitch/roll and ocean drift during booster touchdown.
   - Aligns deck normal to local vertical across spherical planetary curvature and locks compass heading (default `90°`).
4. **Full English & Locale-Safe Support**:
   - Pure English UI, PAW fields, and screen messages (renders cleanly on stock English KSP without CJK font issues).
   - Uses `CultureInfo.InvariantCulture` for all coordinate parsing and persistence, preventing decimal-separator bugs on non-English OS locales.
5. **Plug-and-Play ModuleManager Patch**:
   - Automatically adds `ModuleBargeAnchor` to `GravityIssue` ASOG (`LH_SPX_ASOG`), Jacklyn (`JACKLYN_LPV`), and any command part with `ASDS` in its title.

---

## Dependencies

- **Kerbal Space Program**: 1.12.x
- **ModuleManager** (required for automatic `ASOG_AnchorPatch.cfg` injection)
- **An ASDS Barge Vessel Mod** (e.g., `GravityIssue` ASOG / Jacklyn, or any custom barge named with `ASDS` / `ASOG` / `OCISLY` / `JRTI` / `Barge`)
- **Optional**: [kOS-BoosterRecovery](https://github.com/tthirteenttt/kOS-BoosterRecovery) for automatic `asds_predicted.ks` coordinate generation

---

## Installation

1. Copy `GameData/ASDSManager/` into `<KSP_ROOT>/GameData/`.
2. Launch KSP and verify the **ASDS Manager** icon appears on the stock toolbar.

---

## Quickstart Workflow

1. **One-time Barge Setup**: Launch your ASDS barge (e.g., ASOG) into the ocean once so it exists as an active vessel in your save.
2. **Generate Predicted Coordinates** (if using `kOS-BoosterRecovery`):
   - On the launchpad, run `SWITCH TO 0. RUN estimate.` in kOS to generate `Ships/Script/config/asds_predicted.ks`.
3. **Deploy & Lock ASDS**:
   - Click the **ASDS Manager** toolbar icon.
   - Verify the target barge and loaded coordinates (or enter custom coordinates / heading / sea-level offset; keep `0.5 m` for ASOG to match `kOS-BoosterRecovery`'s calibrated `5.5 m` deck elevation).
   - Click **Deploy & Lock ASDS to Coordinates**.
4. **Launch & Land**: Your barge is now remotely positioned and kinematically stabilized at the landing target.

---

## Building from Source

Requires .NET Framework 4.5 MSBuild (included with Windows) and KSP 1.12.x managed assemblies:
```powershell
.\Tools\build.ps1 -Configuration Release -TargetKSP "<PATH_TO_YOUR_KSP>"
```
The compiled `ASDSManager.dll` is automatically copied to `GameData/ASDSManager/Plugins/`.

---

## License

Released under the **MIT License** (see [LICENSE](LICENSE)).
