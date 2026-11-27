# ASDS-Manager

[![KSP Version](https://img.shields.io/badge/KSP-1.12.x-brightgreen.svg)]()
[![Compatibility](https://img.shields.io/badge/RSS%20%2F%20RO%20%2F%20Stock-Compatible-blue.svg)]()
[![License](https://img.shields.io/badge/License-MIT-green.svg)]()

**ASDS-Manager** is a KSP 1.12.x plugin for remote Autonomous Spaceport Drone Ship (ASDS) deployment and kinematic attitude locking. It is designed to pair seamlessly with [kOS-BoosterRecovery](https://github.com/tthirteenttt/kOS-BoosterRecovery), automatically reading predicted landing coordinates from `estimate.ks` and locking barges (such as ASOG) flat and stationary on the ocean surface.

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
5. **Plug-and-Play ModuleManager & RO Patches**:
   - Automatically adds `ModuleBargeAnchor` to `GravityIssue` ASOG (`LH_SPX_ASOG`), Jacklyn (`JACKLYN_LPV`), and any command part with `ASDS` in its title (`GameData/ASDSManager/Patches/ASOG_AnchorPatch.cfg`).
   - Includes a bundled 1:1 real-scale **Realism Overhaul + RealAntennas** config for **ASOG** (`GameData/ASDSManager/Patches/ASOG_RO_config.cfg`).

---

## Dependencies

### Required
- **Kerbal Space Program**: 1.12.x
- **ModuleManager** (required for `ASOG_AnchorPatch.cfg` and `ASOG_RO_config.cfg`)
- **[GravityIssue](https://spacedock.info/mod/3895/GravityIssue)** (CKAN: `GravityIssue`) — provides the ASOG (`LH_SPX_ASOG`) drone ship model and part

### Bundled Custom RO Configuration (Included in This Release)
- **`GameData/ASDSManager/Patches/ASOG_RO_config.cfg`** — custom Realism Overhaul & RealAntennas patch scaling ASOG to real-world Marmac 302 dimensions (`91.8 m × 44.4 m × 6.1 m`, `1,800 t`).
  - **Note**: RO configuration is provided for **ASOG (`LH_SPX_ASOG`) only**; Jacklyn / Javelin is **not** configured for Realism Overhaul.
  - Guarded with `:NEEDS[RealismOverhaul,GravityIssue]` and `:NEEDS[RealAntennas,GravityIssue]`, so it stays inactive on stock KSP installs.

### Optional
- **[kOS](https://github.com/KSP-KOS/KOS)** & **[kOS-BoosterRecovery](https://github.com/tthirteenttt/kOS-BoosterRecovery)** — for automatic `asds_predicted.ks` coordinate generation and autonomous Falcon 9 booster landing

---

## Installation

1. Install **ModuleManager** and **[GravityIssue](https://spacedock.info/mod/3895/GravityIssue)** into `<KSP_ROOT>/GameData/`.
2. Download `ASDS-Manager-v1.0.1.zip` from [Releases](https://github.com/tthirteenttt/ASDS-Manager/releases) and extract `GameData/ASDSManager/` into `<KSP_ROOT>/GameData/`.
3. Launch KSP and verify the **ASDS Manager** icon appears on the stock toolbar.

*(Upgrading from v1.0.0: you may delete the old `GameData/GravityIssue/Patches/RO_config.cfg` if you previously copied it manually; it now lives cleanly under `GameData/ASDSManager/Patches/ASOG_RO_config.cfg`.)*

---

## Quickstart Workflow

1. **One-time Barge Setup**: Launch the **ASOG** barge into the ocean once so it exists as an active vessel in your save.
2. **Generate Predicted Coordinates** (if using `kOS-BoosterRecovery`):
   - On the launchpad, run `SWITCH TO 0. RUN estimate.` in kOS to generate `Ships/Script/config/asds_predicted.ks`.
3. **Deploy & Lock ASDS**:
   - Click the **ASDS Manager** toolbar icon.
   - Verify the target barge and loaded coordinates (or enter custom coordinates / heading / sea-level offset; keep `0.5 m` for ASOG to match `kOS-BoosterRecovery`'s calibrated `5.5 m` deck elevation).
   - Click **Deploy & Lock ASDS to Coordinates**.
4. **Launch & Land**: Your barge is now remotely positioned and kinematically stabilized at the landing target.

---

## Building & Packaging from Source

Requires .NET Framework 4.5 MSBuild (included with Windows) and KSP 1.12.x managed assemblies:
```powershell
.\Tools\build.ps1 -Configuration Release -TargetKSP "<PATH_TO_YOUR_KSP>" -PackageRelease -Version "1.0.1"
```
The compiled `ASDSManager.dll` is automatically copied to `GameData/ASDSManager/Plugins/`. Passing `-PackageRelease` generates a clean CKAN-compliant `ASDS-Manager-v<Version>.zip` containing only `GameData/ASDSManager/`.

---

## License

Released under the **MIT License** (see [LICENSE](LICENSE)).
