using Verse;

namespace HeatMap
{
    public class RoomWithLabelCell
    {
        public readonly Room Room;

        public readonly IntVec3 Cell;

        public RoomWithLabelCell(Room room, IntVec3 cell)
        {
            Room = room;
            Cell = cell;
        }
    }
}