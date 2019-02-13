using System.Collections.Generic;
using System.Security.Policy;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace HeatMap
{
    [DefOf]
    public static class HeatMapKeyBingings
    {
        public static KeyBindingDef ToggleHeatMap;
    }

    public class Main : HugsLib.ModBase
    {
        public Main()
        {
            Instance = this;
        }

        public void UpdateHeatMap()
        {
            if (_heatMap == null)
                _heatMap = new HeatMap();

            _heatMap.Update(_updateDelay);
        }

        public void UpdateOutdoorThermometer()
        {
            if (!_showOutdoorThermometer)
                return;

            if (_heatMap == null)
                return;

            const float boxSize = 62f;
            var horizontalOffset = 8f;
            if (Prefs.AdaptiveTrainingEnabled)
                horizontalOffset += 216f;

            var outRect = new Rect(UI.screenWidth - horizontalOffset - boxSize, 8f, boxSize, boxSize);
            var temperature = Find.CurrentMap.mapTemperature.OutdoorTemp;
            var textureIndex = _heatMap.GetIndexForTemperature(temperature);
            if (!_temperatureTextureCache.ContainsKey(textureIndex))
            {
                var backColor = _heatMap.GetColorForTemperature(temperature);
                backColor.a = _outdoorThermometerOpacity / 100f;
                _temperatureTextureCache[textureIndex] = SolidColorMaterials.NewSolidColorTexture(backColor);
            }
            GUI.DrawTexture(outRect, _temperatureTextureCache[textureIndex]);
            GUI.DrawTexture(outRect, Resources.DisplayBoder);

            var temperatureForDisplay = temperature.ToStringTemperature("F0");
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(outRect, temperatureForDisplay);

            if (Widgets.ButtonInvisible(outRect))
            {
                ShowHeatMap = !ShowHeatMap;
            }
            TooltipHandler.TipRegion(outRect, "FALCHM.ThermometerTooltip".Translate());

            Text.Anchor = TextAnchor.UpperLeft;
        }

        public override void OnGUI()
        {
            if (Current.ProgramState != ProgramState.Playing ||
                Find.CurrentMap == null ||
                WorldRendererUtility.WorldRenderedNow ||
                _heatMap == null)
            {
                return;
            }

            UpdateOutdoorThermometer();
            if (ShowHeatMap && _showTemperatureOverRooms)
            {
                TemperatureDisplayer.Update(_updateDelay);
                TemperatureDisplayer.OnGUI();
            }

            if (Event.current.type != EventType.KeyDown || Event.current.keyCode == KeyCode.None)
            {
                return;
            }

            if (HeatMapKeyBingings.ToggleHeatMap.JustPressed)
            {
                if (WorldRendererUtility.WorldRenderedNow)
                {
                    return;
                }
                ShowHeatMap = !ShowHeatMap;
            }
        }

        public override void WorldLoaded()
        {
            ResetAll();
        }

        public override void DefsLoaded()
        {
            _opacity = Settings.GetHandle(
                "opacity", "FALCHM.OverlayOpacity".Translate(),
                "FALCHM.OverlayOpacityDesc".Translate(), 30,
                Validators.IntRangeValidator(1, 100));

            _opacity.OnValueChanged = val => { _heatMap?.Reset(); };

            _updateDelay = Settings.GetHandle("updateDelay",
                "FALCHM.UpdateDelay".Translate(),
                "FALCHM.UpdateDelayDesc".Translate(),
                100,
                Validators.IntRangeValidator(1, 9999));

            _showOutdoorThermometer = Settings.GetHandle(
                "showOutdoorThermometer",
                "FALCHM.ShowOutDoorThermometer".Translate(),
                "FALCHM.ShowOutDoorThermometerDesc".Translate(),
                true);

            _outdoorThermometerOpacity = Settings.GetHandle(
                "outdoorThermometerOpacity",
                "FALCHM.ThermometerOpacity".Translate(),
                "FALCHM.ThermometerOpacityDesc".Translate(),
                30,
                Validators.IntRangeValidator(1, 100));

            _outdoorThermometerOpacity.OnValueChanged = val => { _temperatureTextureCache.Clear(); };


            _showTemperatureOverRooms = Settings.GetHandle(
                "showTemperatureOverRooms",
                "FALCHM.ShowTemperatureOverRooms".Translate(),
                "FALCHM.ShowTemperatureOverRoomsDesc".Translate(),
                true);


            _useCustomRange = Settings.GetHandle(
                "useCustomeRange",
                "FALCHM.UseCustomeRange".Translate(),
                "FALCHM.UseCustomeRangeDesc".Translate(),
                false);

            _useCustomRange.OnValueChanged = val => { ResetAll(); };


            _customRangeMin = Settings.GetHandle("customRangeMin", "Unused", "Unused", 0);
            _customRangeMax = Settings.GetHandle("customRangeMax", "Unused", "Unused", 40);

            _customRangeMin.VisibilityPredicate = () => false;
            _customRangeMax.VisibilityPredicate = () => false;


            var customRangeValidator = Validators.IntRangeValidator(
                (int)GenTemperature.CelsiusTo(-100, Prefs.TemperatureMode),
                (int)GenTemperature.CelsiusTo(100, Prefs.TemperatureMode));

            var customRangeMin = Settings.GetHandle(
                "customRangeMinPlaceholder",
                "FALCHM.CustomRangeMin".Translate(),
                $"{"FALCHM.CustomRangeMinDesc".Translate()} ({Prefs.TemperatureMode.ToStringHuman()})",
                (int)GenTemperature.CelsiusTo(_customRangeMin, Prefs.TemperatureMode),
                customRangeValidator);

            customRangeMin.Unsaved = true;
            customRangeMin.VisibilityPredicate = () => _useCustomRange;

            var customRangeMax = Settings.GetHandle(
                "customRangeMaxPlaceholder",
                "FALCHM.CustomRangeMax".Translate(),
                $"{"FALCHM.CustomRangeMaxDesc".Translate()} ({Prefs.TemperatureMode.ToStringHuman()})",
                (int)GenTemperature.CelsiusTo(_customRangeMax, Prefs.TemperatureMode),
                customRangeValidator);

            customRangeMax.Unsaved = true;
            customRangeMax.VisibilityPredicate = () => _useCustomRange;


            customRangeMin.OnValueChanged = val =>
            {
                if (customRangeMax <= customRangeMin)
                    customRangeMax.Value = customRangeMin + 1;

                _customRangeMin.Value = ConvertToCelcius(customRangeMin);
                ResetAll();
            };


            customRangeMax.OnValueChanged = val =>
            {
                if (customRangeMin >= customRangeMax)
                    customRangeMin.Value = customRangeMax - 1;

                _customRangeMax.Value = ConvertToCelcius(customRangeMax);
                ResetAll();
            };
        }

        public bool ShouldUseCustomRange()
        {
            return _useCustomRange;
        }

        public int GetCustomRangeMin()
        {
            return _customRangeMin;
        }

        public int GetCustomRangeMax()
        {
            return _customRangeMax;
        }

        public float GetConfiguredOpacity()
        {
            return _opacity / 100f;
        }

        private void ResetAll()
        {
            _heatMap = null;
            TemperatureDisplayer.Reset();
            _temperatureTextureCache.Clear();
        }

        private static int ConvertToCelcius(int value)
        {
            switch (Prefs.TemperatureMode)
            {
                case TemperatureDisplayMode.Celsius:
                    return value;

                case TemperatureDisplayMode.Kelvin:
                    return value - 273;

                default:
                    return (int)((value - 32) / 1.8f);
            }
        }

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public override string ModIdentifier => "HeatMap";

        public bool ShowHeatMap;

        public RoomTemperatureDisplayer TemperatureDisplayer { get; } = new RoomTemperatureDisplayer();

        private HeatMap _heatMap;

        private readonly Dictionary<int, Texture2D> _temperatureTextureCache = new Dictionary<int, Texture2D>();

        private SettingHandle<int> _opacity;

        private SettingHandle<int> _updateDelay;

        private SettingHandle<bool> _showOutdoorThermometer;

        private SettingHandle<int> _outdoorThermometerOpacity;

        private SettingHandle<int> _customRangeMin;

        private SettingHandle<int> _customRangeMax;

        private SettingHandle<bool> _useCustomRange;

        private SettingHandle<bool> _showTemperatureOverRooms;
    }
}
