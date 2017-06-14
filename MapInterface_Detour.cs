using Harmony;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace HeatMap
{
    [HarmonyPatch(typeof(MapInterface), "MapInterfaceUpdate")]
    public static class MapInterface_Detour
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            if (Find.VisibleMap == null || WorldRendererUtility.WorldRenderedNow)
            {
                return;
            }

            Main.Instance.UpdateHeatMap();
        }
    }
}