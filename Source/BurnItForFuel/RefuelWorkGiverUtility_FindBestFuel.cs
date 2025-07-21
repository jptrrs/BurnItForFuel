using HarmonyLib;
using RimWorld;
using Verse;

namespace BurnItForFuel;

[HarmonyPatch(typeof(RefuelWorkGiverUtility), "FindBestFuel")]
public static class RefuelWorkGiverUtility_FindBestFuel
{
    public static void Postfix(Pawn pawn, Thing refuelable, ref Thing __result)
    {
        if (refuelable.TryGetComp<CompSelectFuel>() != null)
        {
            __result = HarmonyPatches.FindBestFuel(pawn, refuelable);
        }
    }
}