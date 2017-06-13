using UnityEngine;
using Verse;

namespace HeatMap
{
    public class HeatMap : ICellBoolGiver
    {
        private CellBoolDrawer _drawerInt;

        private readonly Map _map;

        public HeatMap(Map map)
        {
            _map = map;
        }

        public CellBoolDrawer Drawer
        {
            get
            {
                if (_drawerInt == null)
                {
                    _drawerInt = new CellBoolDrawer(this, _map.Size.x, _map.Size.z, 0.33f);
                }
                return _drawerInt;
            }
        }

        public bool GetCellBool(int index)
        {
            return true;
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
            if (_map != null)
            {
                this.Drawer.MarkForDraw();
            }
            this.Drawer.CellBoolDrawerUpdate();
        }
    }
}