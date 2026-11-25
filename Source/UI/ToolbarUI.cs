using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;
using ASDSManager.Core;
using ASDSManager.IO;

namespace ASDSManager.UI
{
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class ToolbarUI : MonoBehaviour
    {
        private ApplicationLauncherButton _appButton;
        private bool _showWindow = false;
        private Rect _windowRect = new Rect(300, 150, 420, 400);
        private int _windowId = 849204;

        private List<Vessel> _bargeList = new List<Vessel>();
        private int _selectedBargeIndex = 0;
        private PredictionFileReader.PredictionResult _prediction;

        private string _latStr = "0.0";
        private string _lngStr = "0.0";
        private string _headingStr = "90.0";
        private string _deckOffsetStr = "0.5";
        private string _statusMsg = "Ready. Reload kOS prediction or deploy barge.";

        private Texture2D _iconTexture;

        private void Start()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(OnAppLauncherDestroyed);

            LoadIcon();
            RefreshBargeList();
            AutoFetchPrediction();
        }

        private void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnAppLauncherDestroyed);
            if (_appButton != null && ApplicationLauncher.Instance != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_appButton);
                _appButton = null;
            }
        }

        private void LoadIcon()
        {
            string iconPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "ASDSManager", "Textures", "icon.png");
            if (File.Exists(iconPath))
            {
                _iconTexture = new Texture2D(38, 38, TextureFormat.RGBA32, false);
                ImageConversion.LoadImage(_iconTexture, File.ReadAllBytes(iconPath));
            }
            else
            {
                _iconTexture = new Texture2D(38, 38, TextureFormat.RGBA32, false);
                Color[] colors = new Color[38 * 38];
                for (int i = 0; i < colors.Length; i++) colors[i] = new Color(0.1f, 0.6f, 0.9f, 0.9f);
                _iconTexture.SetPixels(colors);
                _iconTexture.Apply();
            }
        }

        private void OnAppLauncherReady()
        {
            if (_appButton == null && ApplicationLauncher.Instance != null)
            {
                _appButton = ApplicationLauncher.Instance.AddModApplication(
                    OnToggleOn,
                    OnToggleOff,
                    null, null, null, null,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION,
                    _iconTexture
                );
            }
        }

        private void OnAppLauncherDestroyed()
        {
            if (_appButton != null && ApplicationLauncher.Instance != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_appButton);
                _appButton = null;
            }
        }

        private void OnToggleOn()
        {
            _showWindow = true;
            RefreshBargeList();
            AutoFetchPrediction();
        }

        private void OnToggleOff()
        {
            _showWindow = false;
        }

        private void RefreshBargeList()
        {
            _bargeList = BargeManager.GetAvailableASDSVessels();
            if (_selectedBargeIndex >= _bargeList.Count)
            {
                _selectedBargeIndex = 0;
            }
        }

        private void AutoFetchPrediction()
        {
            _prediction = PredictionFileReader.ReadPredictedCoordinates(null);
            if (_prediction.Success)
            {
                _latStr = _prediction.Latitude.ToString("F6", CultureInfo.InvariantCulture);
                _lngStr = _prediction.Longitude.ToString("F6", CultureInfo.InvariantCulture);
                _statusMsg = string.Format(CultureInfo.InvariantCulture, "Loaded kOS prediction ({0:HH:mm:ss}).", _prediction.LastModified);
            }
            else
            {
                _statusMsg = _prediction.Message;
            }
        }

        private void OnGUI()
        {
            if (_showWindow)
            {
                GUI.skin = HighLogic.Skin;
                _windowRect = GUILayout.Window(_windowId, _windowRect, DrawWindow, "ASDS Barge Deployment & Anchor Manager");
            }
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // 1. Target barge selector
            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Barge:", GUILayout.Width(90));
            if (_bargeList.Count > 0)
            {
                Vessel cur = _bargeList[_selectedBargeIndex];
                string statusText = cur.loaded ? "[Loaded]" : "[Unloaded]";
                if (GUILayout.Button(string.Format("< {0} {1} >", cur.vesselName, statusText), GUILayout.ExpandWidth(true)))
                {
                    _selectedBargeIndex = (_selectedBargeIndex + 1) % _bargeList.Count;
                }
            }
            else
            {
                GUILayout.Label("< No ASDS barge found in save >", GUILayout.ExpandWidth(true));
            }
            if (GUILayout.Button("Refresh", GUILayout.Width(65)))
            {
                RefreshBargeList();
            }
            GUILayout.EndHorizontal();

            if (_bargeList.Count > 0)
            {
                Vessel cur = _bargeList[_selectedBargeIndex];
                GUILayout.Label(string.Format(CultureInfo.InvariantCulture, "Current Pos: Lat <b>{0:F6}</b> | Lng <b>{1:F6}</b>", cur.latitude, cur.longitude));
            }

            GUILayout.Space(6);

            // 2. kOS prediction file status
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>kOS Predictor:</b>");
            if (GUILayout.Button("Reload Prediction", GUILayout.Width(135)))
            {
                AutoFetchPrediction();
            }
            GUILayout.EndHorizontal();

            if (_prediction.Success)
            {
                GUILayout.Label(string.Format("File: {0}", _prediction.FilePath));
                GUILayout.Label(string.Format(CultureInfo.InvariantCulture, "Predicted: Lat <b>{0:F6}</b> | Lng <b>{1:F6}</b>", _prediction.Latitude, _prediction.Longitude));
            }
            else
            {
                GUILayout.Label(string.Format("Status: <color=yellow>{0}</color>", _prediction.Message));
            }
            GUILayout.EndVertical();

            GUILayout.Space(6);

            // 3. Deployment parameters
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Deployment & Attitude Parameters:</b>");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Latitude:", GUILayout.Width(105));
            _latStr = GUILayout.TextField(_latStr);
            GUILayout.Label("deg", GUILayout.Width(28));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Longitude:", GUILayout.Width(105));
            _lngStr = GUILayout.TextField(_lngStr);
            GUILayout.Label("deg", GUILayout.Width(28));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Heading:", GUILayout.Width(105));
            _headingStr = GUILayout.TextField(_headingStr);
            GUILayout.Label("deg", GUILayout.Width(28));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sea Alt Offset:", GUILayout.Width(105));
            _deckOffsetStr = GUILayout.TextField(_deckOffsetStr);
            GUILayout.Label("m", GUILayout.Width(28));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // 4. Primary actions
            GUI.backgroundColor = new Color(0.2f, 0.7f, 1.0f);
            if (GUILayout.Button("Deploy & Lock ASDS to Coordinates", GUILayout.Height(36)))
            {
                ExecuteTeleportAndAnchor();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Toggle Anchor Lock", GUILayout.Height(28)))
            {
                ToggleSelectedBargeAnchor();
            }
            if (GUILayout.Button("Switch to Barge", GUILayout.Height(28)))
            {
                SwitchToSelectedBarge();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // 5. Status bar
            GUILayout.BeginVertical("box");
            GUILayout.Label("Status: " + _statusMsg);
            GUILayout.EndVertical();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void ExecuteTeleportAndAnchor()
        {
            if (_bargeList.Count == 0)
            {
                _statusMsg = "Error: No ASDS barge found. Launch and splash a barge first.";
                return;
            }

            double lat, lng;
            if (!double.TryParse(_latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out lat) ||
                !double.TryParse(_lngStr, NumberStyles.Float, CultureInfo.InvariantCulture, out lng))
            {
                _statusMsg = "Error: Invalid latitude/longitude format.";
                return;
            }

            float heading = 90.0f;
            float.TryParse(_headingStr, NumberStyles.Float, CultureInfo.InvariantCulture, out heading);

            float offset = 0.5f;
            float.TryParse(_deckOffsetStr, NumberStyles.Float, CultureInfo.InvariantCulture, out offset);

            Vessel targetBarge = _bargeList[_selectedBargeIndex];
            bool ok = BargeManager.RepositionAndAnchorBarge(targetBarge, lat, lng, heading, offset);
            if (ok)
            {
                _statusMsg = string.Format(CultureInfo.InvariantCulture, "Deployed {0} to ({1:F4}, {2:F4}) and locked.", targetBarge.vesselName, lat, lng);
            }
            else
            {
                _statusMsg = "Deployment failed. Check KSP.log for details.";
            }
        }

        private void ToggleSelectedBargeAnchor()
        {
            if (_bargeList.Count == 0) return;
            Vessel v = _bargeList[_selectedBargeIndex];
            if (v.loaded)
            {
                ModuleBargeAnchor anchor = v.FindPartModuleImplementing<ModuleBargeAnchor>();
                if (anchor != null)
                {
                    anchor.ToggleAnchor();
                    _statusMsg = string.Format("Anchor lock set to: {0}", anchor.isAnchored);
                }
            }
            else
            {
                _statusMsg = "Unloaded barge cannot toggle lock directly; use Deploy & Lock.";
            }
        }

        private void SwitchToSelectedBarge()
        {
            if (_bargeList.Count == 0) return;
            Vessel v = _bargeList[_selectedBargeIndex];
            if (FlightGlobals.ActiveVessel != v)
            {
                FlightGlobals.SetActiveVessel(v);
                _statusMsg = string.Format("Switched active vessel to {0}.", v.vesselName);
            }
        }
    }
}
