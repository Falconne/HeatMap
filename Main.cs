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

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public override string ModIdentifier => "HeatMap";

        public bool ShowHeatMap = false;

        private HeatMap _heatMap;

    }
}
