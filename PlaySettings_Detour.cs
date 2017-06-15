using Harmony;
using RimWorld;
using Verse;

namespace HeatMap
{
    [HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
    public static class PlaySettings_Detour
    {
        [HarmonyPostfix]
        static void PostFix(WidgetRow row, bool worldView)
        {
            if (worldView)
                return;

            row.ToggleableIcon(ref Main.Instance.ShowHeatMap, , "Show Heat Map", SoundDefOf.MouseoverToggle, null);

        }
    }
}