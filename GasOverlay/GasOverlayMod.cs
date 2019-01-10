﻿using System;
using System.IO;
using GasOverlay.HSV;
using Harmony;
using Newtonsoft.Json;
using UnityEngine;

namespace GasOverlay
{
    public static class GasOverlayMod
    {
        public static Color?[] LastColors;
        private static readonly Color NotGasColor = new Color(0.6f, 0.6f, 0.6f);
        private static Config Config = new Config();

        [HarmonyPatch(typeof(SplashMessageScreen), "OnSpawn")]
        public static class SplashMessageScreen_OnSpawn
        {
            public static readonly string directoryPath = "Mods" + Path.DirectorySeparatorChar + "GasOverlay";
            public static readonly string filePath = directoryPath + Path.DirectorySeparatorChar + "Config.json";

            public static void Postfix()
            {
                try
                {
                    SetWatcher();
                }
                catch (Exception e)
                {
                    Debug.Log("GasOverlay: Error while starting file watcher: " + e);
                }

                try
                {
                    ReloadConfig();
                }
                catch (Exception e)
                {
                    Debug.Log("GasOverlay: Error while loading config: " + e);
                }
            }

            private static void ReloadConfig()
            {
                if (File.Exists(filePath))
                {
                    Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(filePath));
                    Debug.Log("GasOverlay: config reloaded");
                }
            }

            private static void SetWatcher()
            {
                var watcher = new FileSystemWatcher(directoryPath, "*.json");
                watcher.Changed += (o, e) => ReloadConfig();
                watcher.EnableRaisingEvents = true;
            }
        }

        [HarmonyPatch(typeof(SimDebugView), "GetOxygenMapColour")]
        public static class SimDebugView_GetOxygenMapColour
        {
            public static bool Prefix(int cell, ref Color __result)
            {
                Element element = Grid.Element[cell];

                Color newGasColor = !element.IsGas
                    ? newGasColor = NotGasColor
                    : newGasColor = GetGasColor(cell, element);

                if (LastColors == null)
                {
                    ResetLastColors();
                }

                try
                {
                    __result = LastColors[cell].HasValue
                        ? Color.Lerp(LastColors[cell].Value, newGasColor, Config.InterpFactor)
                        : newGasColor;

                    LastColors[cell] = __result;
                }
                catch (ArgumentOutOfRangeException)
                {
                    ResetLastColors();
                }

                return false;
            }

            private static void ResetLastColors()
            {
                LastColors = new Color?[Grid.CellCount];
            }

            private static Color GetGasColor(int cell, Element element)
            {
                SimHashes elementID = element.id;
                Color primaryColor = GetCellOverlayColor(cell);
                float mass = Grid.Mass[cell];
                float maxMass = Config.GasPressureEnd;
                float pressureFraction = GetPressureFraction(mass, maxMass);

                ColorHSV colorHSV = primaryColor.ToHSV();

                colorHSV = ScaleColorToPressure(colorHSV, pressureFraction, elementID);
				
                if (Config.ShowEarDrumPopMarker)
                {
                    colorHSV = MarkEarDrumPopPressure(colorHSV, mass, elementID);
                }

                colorHSV = colorHSV.Clamp();

                return colorHSV.ToRgb();
            }

            private static ColorHSV ScaleColorToPressure(ColorHSV color, float fraction, SimHashes elementID)
            {
                if (elementID == SimHashes.CarbonDioxide)
                {
					color.V *= (1 - fraction) * Config.FactorValueHSVCarbonDioxide;
				}
                else
                {
                    color.S *= fraction * 1.25f;
					color.V -= (1 - fraction) * Config.FactorValueHSVGases;
				}

                return color;
            }

            public static Color GetCellOverlayColor(int cellIndex)
            {
                Element element = Grid.Element[cellIndex];
                Substance substance = element.substance;

                Color32 overlayColor = substance.conduitColour;

                overlayColor.a = byte.MaxValue;

                return overlayColor;
            }

            public static float GetPressureFraction(float mass, float maxMass)
            {
                float minFraction = Config.MinimumGasColorIntensity;
                float fraction = mass / maxMass;

                fraction = Mathf.Lerp(minFraction, 1, fraction);

                return fraction;
            }

            private static ColorHSV MarkEarDrumPopPressure(ColorHSV color, float mass, SimHashes elementID)
            {
                if (mass > Config.EarPopFloat)
                {
                    if (elementID == SimHashes.CarbonDioxide)
                    {
                        color.V += 0.3f;
                        color.S += 0.4f;
                    }
                    else
                    {
                        color.H += 0.1f;
                    }
                }

                return color;
            }
        }
    }
}