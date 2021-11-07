using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HeatMap
{
    public class RoomTemperatureDisplayer
    {
        public List<IntVec3> LabelCells { get; } = new List<IntVec3>();

		private Map _map = null;
        private int _nextUpdateTick = 0;

        public void Update(int updateDelay)
        {
            var tick = Find.TickManager.TicksGame;
            if (tick < _nextUpdateTick)
                return;

            _nextUpdateTick = tick + updateDelay;
            LabelCells.Clear();

			if (_map == null) // Shouldn't happen, but better to catch it anyway
			{
				Log.Message($"HeatMap: RoomTemperatureDisplayer.Update: {nameof(_map)} == null!");
				_map = Find.CurrentMap;
			}

			foreach (var room in _map.regionGrid.allRooms)
            {
                if (room.PsychologicallyOutdoors || room.Fogged || room.IsDoorway || room.BorderCells.Count() == 0)
                    continue;

                var cell = GetBestCellForRoom(room, _map);
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

            if (midCell.GetRoom(map) == room)
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
			_map = Find.CurrentMap;
		}
		
		public void OnGUI()
        {
			if (_map == null) // Shouldn't happen, but better to catch it anyway
			{
				Log.Message($"HeatMap: RoomTemperatureDisplayer.OnGUI: {nameof(_map)} == null!");
				_map = Find.CurrentMap;
			}

            Text.Font = GameFont.Tiny;
            //CellRect currentViewRect = Find.CameraDriver.CurrentViewRect;
            foreach (var cell in LabelCells)
            {
                //if (!currentViewRect.Contains(cell))
                //    continue;

                var room = cell.GetRoom(_map);
                if (room == null)
                    continue;

                var drawTopLeft = GenMapUI.LabelDrawPosFor(cell);
                var labelRect = new Rect(drawTopLeft.x, drawTopLeft.y, 40f, 20f);
                Widgets.Label(labelRect, room.Temperature.ToStringTemperature("F0"));
            }
        }
    }
}