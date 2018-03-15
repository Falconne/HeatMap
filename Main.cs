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

            _heatMap.Update();
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
            var temperature = Find.VisibleMap.mapTemperature.OutdoorTemp;
            var backColor = _heatMap.GetColorForTemperature(temperature);
            backColor.a = _outdoorThermometerOpacity / 100f;
            GUI.DrawTexture(outRect, SolidColorMaterials.NewSolidColorTexture(backColor));
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
            if (Current.ProgramState != ProgramState.Playing || Find.VisibleMap == null
                || WorldRendererUtility.WorldRenderedNow)
            {
                return;
            }

            UpdateOutdoorThermometer();

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
            _heatMap?.Reset();
        }

        public override void DefsLoaded()
        {
            _opacity = Settings.GetHandle(
                "opacity", "FALCHM.OverlayOpacity".Translate(),
                "FALCHM.OverlayOpacityDesc".Translate(), 30,
                Validators.IntRangeValidator(1, 100));

            _opacity.OnValueChanged = val => { _heatMap?.Reset(); };

            _updateDelay = Settings.GetHandle("updateDelay", "FALCHM.UpdateDelay".Translate(),
                "FALCHM.UpdateDelayDesc".Translate(),
                100, Validators.IntRangeValidator(1, 9999));

            _showOutdoorThermometer = Settings.GetHandle(
                "showOutdoorThermometer", "FALCHM.ShowOutDoorThermometer".Translate(),
                "FALCHM.ShowOutDoorThermometerDesc".Translate(), true);

            _outdoorThermometerOpacity = Settings.GetHandle(
                "outdoorThermometerOpacity", "FALCHM.ThermometerOpacity".Translate(),
                "FALCHM.ThermometerOpacityDesc".Translate(), 30,
                Validators.IntRangeValidator(1, 100));
        }

        public float GetConfiguredOpacity()
        {
            return _opacity / 100f;
        }

        public int GetUpdateDelay()
        {
            return _updateDelay;
        }

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public override string ModIdentifier => "HeatMap";

        public bool ShowHeatMap;

        private HeatMap _heatMap;

        private SettingHandle<int> _opacity;

        private SettingHandle<int> _updateDelay;

        private SettingHandle<bool> _showOutdoorThermometer;

        private SettingHandle<int> _outdoorThermometerOpacity;
    }
}
