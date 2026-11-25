using System;
using UnityEngine;

namespace ASDSManager.Core
{
    /// <summary>
    /// PartModule that kinematically freezes an ASDS barge on the ocean surface and
    /// locks its attitude to the local horizon and target heading.
    /// </summary>
    public class ModuleBargeAnchor : PartModule
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "ASDS Anchor")]
        public bool isAnchored = false;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Deck Alt Offset (m)"), UI_FloatRange(minValue = -5f, maxValue = 10f, stepIncrement = 0.1f)]
        public float deckAltitudeOffset = 0.5f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Locked Heading (deg)"), UI_FloatRange(minValue = 0f, maxValue = 360f, stepIncrement = 1f)]
        public float lockedHeading = 90.0f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Maintain Horizon")]
        public bool maintainHorizon = true;

        [KSPField(isPersistant = true)]
        public double anchorLat = 0.0;

        [KSPField(isPersistant = true)]
        public double anchorLng = 0.0;

        [KSPField(isPersistant = true)]
        public bool hasValidAnchorGeo = false;

        private bool _lastAnchored = false;
        private CelestialBody _body;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            _body = (vessel != null) ? vessel.mainBody : null;

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (isAnchored)
                {
                    ApplyKinematicState(true);
                }
                _lastAnchored = isAnchored;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Anchor", active = true)]
        public void ToggleAnchor()
        {
            SetAnchored(!isAnchored, null, null, null);
        }

        [KSPAction("Toggle Barge Anchor")]
        public void ToggleAnchorAction(KSPActionParam param)
        {
            ToggleAnchor();
        }

        [KSPAction("Enable Barge Anchor")]
        public void EnableAnchorAction(KSPActionParam param)
        {
            SetAnchored(true, null, null, null);
        }

        [KSPAction("Disable Barge Anchor")]
        public void DisableAnchorAction(KSPActionParam param)
        {
            SetAnchored(false, null, null, null);
        }

        /// <summary>
        /// Updates anchor state, optionally recording target coordinates and heading.
        /// </summary>
        public void SetAnchored(bool state, double? targetLat, double? targetLng, float? heading)
        {
            isAnchored = state;

            if (vessel != null)
            {
                if (targetLat.HasValue && targetLng.HasValue)
                {
                    anchorLat = targetLat.Value;
                    anchorLng = targetLng.Value;
                    hasValidAnchorGeo = true;
                }
                else if (isAnchored && !hasValidAnchorGeo)
                {
                    anchorLat = vessel.latitude;
                    anchorLng = vessel.longitude;
                    hasValidAnchorGeo = true;
                }

                if (heading.HasValue)
                {
                    lockedHeading = heading.Value;
                }

                ApplyKinematicState(isAnchored);

                ScreenMessages.PostScreenMessage(
                    isAnchored ? "[ASDS] Position & Attitude Locked (Kinematic ON)" : "[ASDS] Anchor Released (Free Float)",
                    4.0f, ScreenMessageStyle.UPPER_CENTER);
            }

            _lastAnchored = isAnchored;
        }

        private void ApplyKinematicState(bool kinematic)
        {
            if (vessel == null || !vessel.loaded) return;

            foreach (Part p in vessel.parts)
            {
                if (p != null && p.rb != null)
                {
                    p.rb.isKinematic = kinematic;
                    if (kinematic)
                    {
                        p.rb.velocity = Vector3.zero;
                        p.rb.angularVelocity = Vector3.zero;
                    }
                }
            }

            if (kinematic && vessel.rootPart != null && vessel.rootPart.rb != null)
            {
                vessel.rootPart.rb.velocity = Vector3.zero;
                vessel.rootPart.rb.angularVelocity = Vector3.zero;
            }
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel == null || !vessel.loaded) return;

            if (isAnchored != _lastAnchored)
            {
                ApplyKinematicState(isAnchored);
                _lastAnchored = isAnchored;
            }

            if (!isAnchored) return;

            _body = vessel.mainBody;
            if (_body == null) return;

            // Re-enforce kinematic freeze if physics reload cleared it
            if (vessel.rootPart != null && vessel.rootPart.rb != null && !vessel.rootPart.rb.isKinematic)
            {
                ApplyKinematicState(true);
            }

            // 1. Align deck normal to local up and lock compass heading
            if (maintainHorizon)
            {
                Vector3d localUp = (vessel.transform.position - _body.transform.position).normalized;
                Vector3d north = Vector3d.Exclude(localUp, (Vector3d)_body.transform.up);
                if (north.sqrMagnitude < 1e-6)
                {
                    north = Vector3d.Exclude(localUp, Vector3d.right);
                }
                north.Normalize();

                Quaternion targetRot = Quaternion.LookRotation((Vector3)north, (Vector3)localUp) * Quaternion.Euler(0f, lockedHeading, 0f);
                vessel.SetRotation(targetRot);
            }

            // 2. Zero residual linear and angular velocity
            if (vessel.rootPart != null && vessel.rootPart.rb != null)
            {
                vessel.rootPart.rb.velocity = Vector3.zero;
                vessel.rootPart.rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
