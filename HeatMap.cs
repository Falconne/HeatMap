using RimWorld;
using UnityEngine;
using Verse;

namespace HeatMap
{
    public class HeatMap : ICellBoolGiver
    {
        public HeatMap()
        {
            _comfortableTemperatureRange = new FloatRange(
                ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin, null),
                ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax, null));

            Main.Instance.Logger.Message("Comfort: " + _comfortableTemperatureRange);
        }

        public CellBoolDrawer Drawer
        {
            get
            {
                if (_drawerInt == null)
                {
                    var map = Find.VisibleMap;
                    _drawerInt = new CellBoolDrawer(this, map.Size.x, map.Size.z, 0.33f);
                }
                return _drawerInt;
            }
        }

        public bool GetCellBool(int index)
        {
            var map = Find.VisibleMap;
            if (map.fogGrid.IsFogged(index))
                return false;

            var room = map.cellIndices.IndexToCell(index).GetRoom(
                map, RegionType.Set_All);

            return room != null && !room.PsychologicallyOutdoors;
        }

        public Color GetCellExtraColor(int index)
        {
            return Color.white;
        }

        public Color Color
        {
            get
            {
                return new Color(0.3f, 1f, 0.4f);
            }
        }

        public void Update()
        {
            if (true)
            {
                this.Drawer.MarkForDraw();
            }
            this.Drawer.CellBoolDrawerUpdate();
        }

        private CellBoolDrawer _drawerInt;

        private FloatRange _comfortableTemperatureRange;

        private readonly float _minHandledTemperature = -100f;

        private readonly float _maxHandledTemperature = 100f;
    }
}