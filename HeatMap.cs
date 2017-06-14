using RimWorld;
using UnityEngine;
using Verse;

namespace HeatMap
{
    public class HeatMap : ICellBoolGiver
    {
        public HeatMap()
        {
            var minComfortTemp = (int) ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin);
            var maxComfortTemp = (int) ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax);
            var comfortRange = maxComfortTemp - minComfortTemp;
            _mappedTemperatureRange = new IntRange(minComfortTemp - comfortRange, maxComfortTemp + comfortRange);

            var mappedColorCount = _mappedTemperatureRange.max - _mappedTemperatureRange.min;
            _mappedColors = new Color[mappedColorCount];

            var channelDelta = 2f / mappedColorCount;
            var channelR = -2f;
            var channelG = 0f;
            var channelB = 2f;
            var greenRising = true;

            for (var i = 0; i < mappedColorCount; i++)
            {
                var realR = Mathf.Min(channelR, 1f);
                realR = Mathf.Max(channelR, 0f);

                var realG = Mathf.Min(channelG, 1f);
                realG = Mathf.Max(channelG, 0f);

                var realB = Mathf.Min(channelB, 1f);
                realB = Mathf.Max(channelB, 0f);

                _mappedColors[i] = new Color(realR, realB, realG);

                if (channelG >= 1f)
                    greenRising = false;

                channelR += channelDelta;
                channelG += greenRising ? channelDelta : -channelDelta;
                channelB -= channelDelta;
            }
        }

        public CellBoolDrawer Drawer
        {
            get
            {
                if (_drawerInt == null)
                {
                    var map = Find.VisibleMap;
                    _drawerInt = new CellBoolDrawer(this, map.Size.x, map.Size.z, 0.22f);
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

            if (room != null && !room.PsychologicallyOutdoors)
            {
                if (room.Temperature <= _mappedTemperatureRange.min)
                {
                    _nextColor = _mappedColors[0];
                }
                else if (room.Temperature >= _mappedTemperatureRange.max)
                {
                    _nextColor = _mappedColors[_mappedColors.Length - 1];
                }
                else
                {
                    var colorMapIndex = (int) room.Temperature - _mappedTemperatureRange.min;
                    if (colorMapIndex <= 0)
                    {
                        colorMapIndex = 0;
                    }
                    else if (colorMapIndex >= _mappedColors.Length)
                    {
                        colorMapIndex = _mappedColors.Length;
                    }
                    _nextColor = _mappedColors[colorMapIndex];
                }
                return true;
            }

            return false;
        }

        public Color GetCellExtraColor(int index)
        {
            return _nextColor;
        }

        public Color Color
        {
            get { return Color.white; }
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

        private IntRange _mappedTemperatureRange;

        private Color[] _mappedColors;

        private Color _nextColor;
    }
}