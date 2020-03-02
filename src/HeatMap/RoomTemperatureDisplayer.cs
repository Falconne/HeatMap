using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HeatMap
{
    public class RoomTemperatureDisplayer
    {
        public List<IntVec3> LabelCells { get; } = new List<IntVec3>();

        private int _nextUpdateTick;

        private Map _lastSeenMap;

        public void Update(int updateDelay)
        {
            var tick = Find.TickManager.TicksGame;
            if (_nextUpdateTick != 0 && tick < _nextUpdateTick && Find.CurrentMap == _lastSeenMap)
                return;

            _lastSeenMap = Find.CurrentMap;

            _nextUpdateTick = tick + updateDelay;
            LabelCells.Clear();

            var map = Find.CurrentMap;
            foreach (var room in map.regionGrid.allRooms)
            {
                if (room.PsychologicallyOutdoors || room.Fogged || room.IsDoorway)
                    continue;

                var cell = GetBestCellForRoom(room, map);
                LabelCells.Add(cell);
            }
        }

        private static IntVec3 GetBestCellForRoom(Room room, Map map)
        {
            var topLeftCorner = room.BorderCells.First();

            var left = int.MaxValue;
            var top = int.MinValue;
            var right = int.MinValue;
            var bottom = int.MaxValue;

            foreach (var cell in room.BorderCells)
            {
                if (cell.x < topLeftCorner.x || cell.z > topLeftCorner.z)
                    topLeftCorner = cell;

                if (cell.x < left)
                    left = cell.x;
                else if (cell.x > right)
                    right = cell.x;

                if (cell.z > top)
                    top = cell.z;
                else if (cell.z < bottom)
                    bottom = cell.z;
            }

            var midX = (int)((right - left) / 2f) + left;
            var midZ = (int)((top - bottom) / 2f) + bottom;

            var midCell = new IntVec3(midX, 0, midZ);

            if (midCell.GetRoom(map, RegionType.Set_All) == room)
                return midCell;

            var possiblyBetterTopLeftCorner = topLeftCorner;
            possiblyBetterTopLeftCorner.x++;
            possiblyBetterTopLeftCorner.z--;
            if (possiblyBetterTopLeftCorner.GetRoom(map) == room)
                topLeftCorner = possiblyBetterTopLeftCorner;

            return topLeftCorner;
        }

        public void Reset()
        {
            LabelCells.Clear();
            _nextUpdateTick = 0;
        }

        public void OnGUI()
        {
            Text.Font = GameFont.Tiny;
            var map = Find.CurrentMap;
            CellRect currentViewRect = Find.CameraDriver.CurrentViewRect;
            foreach (var cell in LabelCells)
            {
                if (!currentViewRect.Contains(cell))
                    continue;

                var room = cell.GetRoom(map, RegionType.Set_All);
                if (room == null)
                    continue;

                var panelLength = 20f;
                var panelHeight = 20f;
                var panelSize = new Vector2(panelLength, panelHeight);
                var drawTopLeft = GenMapUI.LabelDrawPosFor(cell);
                var labelRect = new Rect(drawTopLeft.x, drawTopLeft.y, panelSize.x, panelSize.y);
                Widgets.Label(labelRect, room.Temperature.ToStringTemperature("F0"));
            }
        }
    }
}