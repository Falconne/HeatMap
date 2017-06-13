using HugsLib.Utils;
using Verse;

namespace HeatMap
{
    public class Main : HugsLib.ModBase
    {
        public Main()
        {
            Instance = this;
        }

        public HeatMap GetHeatMap()
        {
            return heatMap;
        }

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public override string ModIdentifier => "HeatMap";

        private readonly HeatMap heatMap = new HeatMap();
    }
}
