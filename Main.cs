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
            if (_outdoorThermometer == null)
                _outdoorThermometer = new OutdoorThermometer();

            _outdoorThermometer.Update(_heatMap, _outdoorThermometerOpacity / 100f);
        }

        public override void OnGUI()
        {
            if (Current.ProgramState != ProgramState.Playing || Find.VisibleMap == null
                || WorldRendererUtility.WorldRenderedNow)
            {
                return;
            }

            if (_showOutdoorThermometer)
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
                "opacity", "Opacity of overlay",
                "Reduce this value to make the overlay more transparent.", 30,
                Validators.IntRangeValidator(1, 100));

            _opacity.OnValueChanged = val => { _heatMap?.Reset(); };

            _updateDelay = Settings.GetHandle("updateDelay", "Update delay",
                "Number of ticks delay between overlay updates while game is unpaused. Lower numbers provide smoother updates, but may affect performance on low end machines.",
                100, Validators.IntRangeValidator(1, 9999));

            _showOutdoorThermometer = Settings.GetHandle(
                "showOutdoorThermometer", "Show outdoor thermometer",
                "Displays outdoor temperature in a distinct box on the top right hand corner of the UI", true);

            _outdoorThermometerOpacity = Settings.GetHandle(
                "outdoorThermometerOpacity", "Opacity of thermometer",
                "Reduce this value to make the outdoor temperature thermometer background color more transparent.", 30,
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

        private OutdoorThermometer _outdoorThermometer;

        private SettingHandle<int> _opacity;

        private SettingHandle<int> _updateDelay;

        private SettingHandle<bool> _showOutdoorThermometer;

        private SettingHandle<int> _outdoorThermometerOpacity;
    }
}
