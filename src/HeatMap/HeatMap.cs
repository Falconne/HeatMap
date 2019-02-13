using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace HeatMap
{
    public class HeatMap : ICellBoolGiver
    {
        public HeatMap()
        {
            if (Main.Instance.ShouldUseCustomRange())
                CreateCustomMap();
            else
                CreateComfortMap();
        }

        public void CreateCustomMap()
        {
            _mappedTemperatureRange = new IntRange(
                Main.Instance.GetCustomRangeMin(), Main.Instance.GetCustomRangeMax());

            var mappedColorCount = _mappedTemperatureRange.max - _mappedTemperatureRange.min;
            _mappedColors = new Color[mappedColorCount];

            var delta = 2f / (mappedColorCount - 1);
            var channelR = -1f;
            var channelG = 0f;
            var channelB = 1f;
            var greenRising = true;

            for (var i = 0; i < mappedColorCount - 1; i++)
            {
                var realR = Math.Min(channelR, 1f);
                realR = Math.Max(realR, 0f);

                var realG = Math.Min(channelG, 1f);
                realG = Math.Max(realG, 0f);

                var realB = Math.Min(channelB, 1f);
                realB = Math.Max(realB, 0f);

                _mappedColors[i] = new Color(realR, realG, realB);

                if (channelG >= 1f)
                    greenRising = false;

                channelR += delta;
                channelG += greenRising ? delta : -delta;
                channelB -= delta;
            }

            // Force high end to be red (or else if the temperature range is an even number,
            // the green channel will not go down to zero in above loop).
            _mappedColors[mappedColorCount - 1] = Color.red;
        }

        public void CreateComfortMap()
        {
            var minComfortTemp = (int)ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin) + 3;
            var maxComfortTemp = (int)ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax) - 3;

            // Narrow down the green range to a quarter scale, to make boundary temps stand out more.

            var comfortDoubleRange = (maxComfortTemp - minComfortTemp) * 2;
            _mappedTemperatureRange = new IntRange(
                minComfortTemp - comfortDoubleRange, maxComfortTemp + comfortDoubleRange);

            var mappedColorCount = _mappedTemperatureRange.max - _mappedTemperatureRange.min;
            _mappedColors = new Color[mappedColorCount];

            var channelDelta = 1f / comfortDoubleRange;
            var channelR = -2f;
            var channelG = 0f;
            var channelB = 2f;
            var greenRising = true;

            var mappingTemperature = _mappedTemperatureRange.min;
            for (var i = 0; i < mappedColorCount - 1; i++, mappingTemperature++)
            {
                var realR = Math.Min(channelR, 1f);
                realR = Math.Max(realR, 0f);

                var realG = Math.Min(channelG, 1f);
                realG = Math.Max(realG, 0f);

                var realB = Math.Min(channelB, 1f);
                realB = Math.Max(realB, 0f);

                _mappedColors[i] = new Color(realR, realG, realB);

                if (channelG >= 2f)
                    greenRising = false;

                var delta = channelDelta;
                if (mappingTemperature >= minComfortTemp - 1 &&
                    mappingTemperature <= maxComfortTemp)
                {
                    delta *= 4;
                }

                channelR += delta;
                channelG += greenRising ? delta : -delta;
                channelB -= delta;
            }

            // Force high end to be red (or else if the temperature range is an even number,
            // the green channel will not go down to zero in above loop).
            _mappedColors[mappedColorCount - 1] = Color.red;
        }

        public CellBoolDrawer Drawer
        {
            get
            {
                if (_drawerInt == null)
                {
                    var map = Find.CurrentMap;
                    _drawerInt = new CellBoolDrawer(this, map.Size.x, map.Size.z,
                        Main.Instance.GetConfiguredOpacity());
                }
                return _drawerInt;
            }
        }

        public bool GetCellBool(int index)
        {
            var map = Find.CurrentMap;
            if (map.fogGrid.IsFogged(index))
                return false;

            var room = map.cellIndices.IndexToCell(index).GetRoom(
                map, RegionType.Set_All);

            if (room != null && !room.PsychologicallyOutdoors)
            {
                _nextColor = GetColorForTemperature(room.Temperature);
                return true;
            }

            return false;
        }

        public int GetIndexForTemperature(float temperature)
        {
            // These two checks are probably not needed due to array index boundary checks
            // below, but too worried to remove them now.
            if (temperature <= _mappedTemperatureRange.min)
            {
                return 0;
            }

            if (temperature >= _mappedTemperatureRange.max)
            {
                return _mappedColors.Length - 1;
            }

            var colorMapIndex = (int)temperature - _mappedTemperatureRange.min;
            if (colorMapIndex <= 0)
            {
                return 0;
            }

            if (colorMapIndex >= _mappedColors.Length)
            {
                return _mappedColors.Length - 1;
            }

            return colorMapIndex;

        }

        public Color GetColorForTemperature(float temperature)
        {
            return _mappedColors[GetIndexForTemperature(temperature)];
        }

        public Color GetCellExtraColor(int index)
        {
            return _nextColor;
        }

        public Color Color => Color.white;

        public void Update(int updateDelay)
        {
            if (Main.Instance.ShowHeatMap)
            {
                Drawer.MarkForDraw();
                var tick = Find.TickManager.TicksGame;
                if (_nextUpdateTick == 0 || tick >= _nextUpdateTick)
                {
                    Drawer.SetDirty();
                    _nextUpdateTick = tick + updateDelay;
                }
            }
            Drawer.CellBoolDrawerUpdate();
        }

        public void Reset()
        {
            _drawerInt = null;
            _nextUpdateTick = 0;
        }

        private CellBoolDrawer _drawerInt;

        private IntRange _mappedTemperatureRange;

        private Color[] _mappedColors;

        private Color _nextColor;

        private int _nextUpdateTick;
    }
}