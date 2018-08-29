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

            if (row == null || Resources.Icon == null)
                return;

            row.ToggleableIcon(ref Main.Instance.ShowHeatMap, Resources.Icon,
                "Show Heat Map", SoundDefOf.Mouseover_ButtonToggle);
        }
    }
}