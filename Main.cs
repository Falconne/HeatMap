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

        public override void MapLoaded(Map map)
        {
            base.MapLoaded(map);
            heatMap = new HeatMap(map);
        }

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public override string ModIdentifier => "HeatMap";

        private HeatMap heatMap;
    }
}
