using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ASDSManager.IO
{
    /// <summary>
    /// Reads and parses predicted ASDS coordinates exported by kOS estimate.ks (asds_predicted.ks).
    /// </summary>
    public static class PredictionFileReader
    {
        public struct PredictionResult
        {
            public bool Success;
            public double Latitude;
            public double Longitude;
            public string FilePath;
            public DateTime LastModified;
            public string Message;
        }

        private static readonly Regex LatRegex = new Regex(@"GLOBAL\s+PREDICTED_ASDS_LAT\s+IS\s+([+-]?\d+(?:\.\d+)?)\.", RegexOptions.IgnoreCase);
        private static readonly Regex LngRegex = new Regex(@"GLOBAL\s+PREDICTED_ASDS_LNG\s+IS\s+([+-]?\d+(?:\.\d+)?)\.", RegexOptions.IgnoreCase);

        /// <summary>
        /// Resolves asds_predicted.ks path (Release layout first, legacy Source/ layout as fallback).
        /// </summary>
        public static string GetCandidateFilePath()
        {
            string root = KSPUtil.ApplicationRootPath;

            // Primary path: standard kOS Release layout (0:/config/asds_predicted.ks)
            string primaryPath = Path.Combine(root, "Ships", "Script", "config", "asds_predicted.ks");
            if (File.Exists(primaryPath))
            {
                return primaryPath;
            }

            // Fallback path: legacy dev layout (0:/Source/config/asds_predicted.ks)
            string legacyPath = Path.Combine(root, "Ships", "Script", "Source", "config", "asds_predicted.ks");
            if (File.Exists(legacyPath))
            {
                return legacyPath;
            }

            return primaryPath;
        }

        /// <summary>
        /// Reads and parses predicted latitude and longitude from asds_predicted.ks.
        /// </summary>
        public static PredictionResult ReadPredictedCoordinates(string customPath = null)
        {
            PredictionResult res = new PredictionResult();
            res.Success = false;
            res.Latitude = 0.0;
            res.Longitude = 0.0;
            res.FilePath = string.IsNullOrEmpty(customPath) ? GetCandidateFilePath() : customPath;
            res.LastModified = DateTime.MinValue;
            res.Message = "";

            if (!File.Exists(res.FilePath))
            {
                res.Message = "Prediction file not found: " + res.FilePath + "\nRun estimate.ks on the launchpad first.";
                return res;
            }

            try
            {
                res.LastModified = File.GetLastWriteTime(res.FilePath);
                string content = File.ReadAllText(res.FilePath);

                Match latMatch = LatRegex.Match(content);
                Match lngMatch = LngRegex.Match(content);

                if (latMatch.Success && lngMatch.Success)
                {
                    double lat;
                    double lng;
                    if (double.TryParse(latMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out lat) &&
                        double.TryParse(lngMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out lng))
                    {
                        res.Latitude = lat;
                        res.Longitude = lng;
                        res.Success = true;
                        res.Message = string.Format(
                            CultureInfo.InvariantCulture,
                            "Loaded: Lat {0:F6}, Lng {1:F6} ({2:HH:mm:ss})",
                            lat, lng, res.LastModified);
                        return res;
                    }
                }

                res.Message = "Failed to parse asds_predicted.ks (expected GLOBAL PREDICTED_ASDS_LAT / LNG).";
            }
            catch (Exception ex)
            {
                res.Message = "Error reading prediction file: " + ex.Message;
                Debug.LogError("[ASDSManager] " + res.Message);
            }

            return res;
        }
    }
}
