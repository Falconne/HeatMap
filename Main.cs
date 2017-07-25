using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HeatMap
{
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

        public override void DefsLoaded()
        {
            _opacity = Settings.GetHandle(
                "opacity", "Opacity of overlay",
                "Reduce this value to make the overlay more transparent", 30,
                Validators.IntRangeValidator(1, 100));

            _opacity.OnValueChanged = val => { _heatMap?.Reset(); };
        }

        public float GetConfiguredOpacity()
        {
            return _opacity / 100f;
        }

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public override string ModIdentifier => "HeatMap";

        public bool ShowHeatMap = false;

        private HeatMap _heatMap;

        private SettingHandle<int> _opacity;
    }
}
