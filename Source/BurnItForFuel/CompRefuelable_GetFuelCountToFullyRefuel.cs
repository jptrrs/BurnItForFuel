using HarmonyLib;
using RimWorld;

namespace BurnItForFuel;

[HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.GetFuelCountToFullyRefuel))]
public static class CompRefuelable_GetFuelCountToFullyRefuel
{
    public static void Postfix(CompRefuelable __instance, ref int __result)
    {
        __result = HarmonyPatches.GetFuelCountToFullyRefuel(__instance);
    }
}