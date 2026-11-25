using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ASDSManager.Core
{
    /// <summary>
    /// Global barge manager: discovers ASDS vessels and handles remote repositioning
    /// and attitude locking for both loaded and unloaded vessels.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class BargeManager : MonoBehaviour
    {
        public static BargeManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// Returns true if the vessel is a recognized ASDS barge.
        /// </summary>
        public static bool IsASDSVessel(Vessel v)
        {
            if (v == null) return false;

            // Rule 1: Loaded vessel with ModuleBargeAnchor
            if (v.FindPartModuleImplementing<ModuleBargeAnchor>() != null)
                return true;

            // Rule 2: Unloaded ProtoVessel with ModuleBargeAnchor
            if (v.protoVessel != null && v.protoVessel.protoPartSnapshots != null)
            {
                foreach (var part in v.protoVessel.protoPartSnapshots)
                {
                    if (part.modules != null)
                    {
                        foreach (var mod in part.modules)
                        {
                            if (mod.moduleName == "ModuleBargeAnchor") return true;
                        }
                    }
                }
            }

            // Rule 3: Vessel name keyword fallback
            string name = (v.vesselName ?? "").ToLowerInvariant();
            if (name.Contains("asds") || name.Contains("asog") || name.Contains("gravitas") ||
                name.Contains("barge") || name.Contains("ocisly") || name.Contains("jrti"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Lists all recognized ASDS vessels in the current save.
        /// </summary>
        public static List<Vessel> GetAvailableASDSVessels()
        {
            List<Vessel> list = new List<Vessel>();
            if (FlightGlobals.Vessels == null) return list;

            foreach (var v in FlightGlobals.Vessels)
            {
                if (IsASDSVessel(v))
                {
                    list.Add(v);
                }
            }
            return list;
        }

        /// <summary>
        /// Teleports and locks a barge to the specified coordinates, heading, and sea-level offset.
        /// Supports both loaded and unloaded (background) vessels.
        /// </summary>
        public static bool RepositionAndAnchorBarge(Vessel barge, double targetLat, double targetLng, float heading, float altOffset)
        {
            if (barge == null)
            {
                Debug.LogError("[ASDSManager] Cannot reposition barge: Vessel is null.");
                return false;
            }

            CelestialBody body = barge.mainBody != null ? barge.mainBody : FlightGlobals.GetHomeBody();
            if (body == null) return false;

            double targetAlt = altOffset;

            try
            {
                Vector3d worldPos = body.GetWorldSurfacePosition(targetLat, targetLng, targetAlt);
                Vector3d localUp = (worldPos - body.position).normalized;
                Vector3d north = Vector3d.Exclude(localUp, (Vector3d)body.transform.up);
                if (north.sqrMagnitude < 1e-6)
                {
                    north = Vector3d.Exclude(localUp, Vector3d.right);
                }
                north.Normalize();
                Quaternion worldRot = Quaternion.LookRotation((Vector3)north, (Vector3)localUp) * Quaternion.Euler(0f, heading, 0f);

                double ut = Planetarium.GetUniversalTime();
                Vector3d orbitPos = (worldPos - body.position).xzy;
                Vector3d orbitVel = body.getRFrmVel(worldPos).xzy;

                if (barge.loaded)
                {
                    barge.SetPosition(worldPos, true);
                    barge.SetRotation(worldRot);

                    barge.latitude = targetLat;
                    barge.longitude = targetLng;
                    barge.altitude = targetAlt;
                    barge.Splashed = true;
                    barge.Landed = false;
                    barge.situation = Vessel.Situations.SPLASHED;

                    ModuleBargeAnchor anchor = barge.FindPartModuleImplementing<ModuleBargeAnchor>();
                    if (anchor != null)
                    {
                        anchor.SetAnchored(true, targetLat, targetLng, heading);
                    }

                    if (barge.rootPart != null && barge.rootPart.rb != null)
                    {
                        barge.rootPart.rb.velocity = Vector3.zero;
                        barge.rootPart.rb.angularVelocity = Vector3.zero;
                    }
                }
                else
                {
                    // Update runtime Vessel state (map view, tracking station, kOS target, physics bubble)
                    barge.latitude = targetLat;
                    barge.longitude = targetLng;
                    barge.altitude = targetAlt;
                    barge.pqsAltitude = body.TerrainAltitude(targetLat, targetLng, true);
                    barge.Splashed = true;
                    barge.Landed = false;
                    barge.landedAt = "";
                    barge.situation = Vessel.Situations.SPLASHED;
                    barge.skipGroundPositioning = true;

                    barge.SetPosition(worldPos);
                    barge.SetRotation(worldRot);

                    // Synchronize OrbitDriver to the new surface-fixed state
                    if (barge.orbitDriver != null)
                    {
                        barge.orbitDriver.pos = orbitPos;
                        barge.orbitDriver.vel = orbitVel;
                        if (barge.orbitDriver.orbit != null)
                        {
                            barge.orbitDriver.orbit.UpdateFromStateVectors(orbitPos, orbitVel, body, ut);
                        }
                        barge.orbitDriver.SetOrbitMode(OrbitDriver.UpdateMode.IDLE);
                        barge.orbitDriver.UpdateOrbit(true);
                    }
                    barge.UpdatePosVel();

                    // Synchronize persistent ProtoVessel for scene transitions and physics load
                    ProtoVessel pv = barge.protoVessel;
                    if (pv != null)
                    {
                        pv.latitude = targetLat;
                        pv.longitude = targetLng;
                        pv.altitude = targetAlt;
                        pv.height = altOffset;
                        pv.splashed = true;
                        pv.landed = false;
                        pv.landedAt = "";
                        pv.situation = Vessel.Situations.SPLASHED;
                        pv.skipGroundPositioning = true;
                        pv.position = body.GetRelSurfacePosition(targetLat, targetLng, targetAlt);

                        Quaternion invBodyRot = Quaternion.Inverse(body.bodyTransform.rotation);
                        pv.rotation = invBodyRot * worldRot;
                        pv.normal = (Vector3)(invBodyRot * (Vector3)localUp);

                        Orbit stationaryOrbit = new Orbit();
                        stationaryOrbit.UpdateFromStateVectors(orbitPos, orbitVel, body, ut);
                        pv.orbitSnapShot = new OrbitSnapshot(stationaryOrbit);

                        if (pv.protoPartSnapshots != null)
                        {
                            foreach (var part in pv.protoPartSnapshots)
                            {
                                if (part.modules != null)
                                {
                                    foreach (var mod in part.modules)
                                    {
                                        if (mod.moduleName == "ModuleBargeAnchor")
                                        {
                                            mod.moduleValues.SetValue("isAnchored", "True", true);
                                            mod.moduleValues.SetValue("anchorLat", targetLat.ToString("F6", CultureInfo.InvariantCulture), true);
                                            mod.moduleValues.SetValue("anchorLng", targetLng.ToString("F6", CultureInfo.InvariantCulture), true);
                                            mod.moduleValues.SetValue("lockedHeading", heading.ToString("F1", CultureInfo.InvariantCulture), true);
                                            mod.moduleValues.SetValue("deckAltitudeOffset", altOffset.ToString("F2", CultureInfo.InvariantCulture), true);
                                            mod.moduleValues.SetValue("hasValidAnchorGeo", "True", true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                ScreenMessages.PostScreenMessage(
                    string.Format(CultureInfo.InvariantCulture, "[ASDS Manager] {0} deployed to Lat {1:F4}, Lng {2:F4} and locked.", barge.vesselName, targetLat, targetLng),
                    5.0f, ScreenMessageStyle.UPPER_CENTER);

                Debug.Log(string.Format(CultureInfo.InvariantCulture, "[ASDSManager] Repositioned {0}: Lat={1:F6}, Lng={2:F6}, Heading={3:F1}, Offset={4:F2}", barge.vesselName, targetLat, targetLng, heading, altOffset));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("[ASDSManager] Failed to reposition barge: {0}\n{1}", ex.Message, ex.StackTrace));
                return false;
            }
        }
    }
}
