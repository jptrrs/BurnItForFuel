using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BurnItForFuel;

[HarmonyPatch(typeof(RefuelWorkGiverUtility), "FindAllFuel")]
public static class RefuelWorkGiverUtility_FindAllFuel
{
    public static void Postfix(Pawn pawn, Thing refuelable, ref List<Thing> __result)
    {
        if (refuelable.TryGetComp<CompSelectFuel>() != null)
        {
            __result = HarmonyPatches.FindAllFuel(pawn, refuelable);
        }
    }
}