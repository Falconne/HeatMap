using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HeatMap
{
    [HarmonyPatch(typeof(MapTemperature), "GetColorForTemperature")]
    public static class MapTemperature_GetColorForTemperature_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(ref Color __result, float temperature)
        {
            // use vanilla overlay
            if (!HeatMap.Instance.OverrideVanillaOverlay)
                return true; // execute original

            // use HeatMap's overlay
            __result = HeatMapHelper.GetColorForTemperature(temperature);
            return false; // skip the original
        }
    }

	[HarmonyPatch(typeof(MapTemperature), "get_Drawer")]
	public static class MapTemperature_get_Drawer_Patch
	{
		[HarmonyPostfix]
		static void Postfix(ref CellBoolDrawer __result)
		{
            // check if opacity changed
            var opacity = HeatMap.Instance.OverlayOpacity;
            if (__result?.opacity != opacity)
            {
                // set drawer opacity
                __result.opacity = opacity;

                // Material must be set to null to regenerate it, which applies the opacity
                __result.material = null;
            }
		}
	}

	[HarmonyPatch(typeof(MapTemperature), "GetCellBool")]
    public static class MapTemperature_GetCellBool_Patch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var output = new List<CodeInstruction>();
            foreach (var instruction in instructions)
            {
                // override everything past GetRooms
                if (instruction.opcode == OpCodes.Call 
                    && instruction.operand is MethodInfo methodInfo 
                    && methodInfo.Name == nameof(GridsUtility.GetRoom))
                    break;

                // keep everything before GetRooms
                output.Add(instruction);
            }

            // call static System.Boolean HeatMap.MapTemperature_GetCellBool_Patch::Check(Verse.IntVec3 intVec, Verse.Map map)
            output.Add(
                new CodeInstruction(OpCodes.Call, 
                typeof(MapTemperature_GetCellBool_Patch).GetMethod(nameof(MapTemperature_GetCellBool_Patch.CheckRoom), BindingFlags.Static | BindingFlags.NonPublic)));
            // ret NULL
            output.Add(new CodeInstruction(OpCodes.Ret));

            // output the changed IL code
            return output;
        }

        /// <summary>
        /// Checks if tile is valid and if it is outdoors, check if outdoors temperature should get an overlay
        /// </summary>
        /// <param name="intVec"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        private static bool CheckRoom(IntVec3 intVec, Map map)
        {
            var room = intVec.GetRoom(map);
            return room != null && (!room.PsychologicallyOutdoors || !HeatMap.Instance.ShowIndoorsOnly);
        }
    }
}
