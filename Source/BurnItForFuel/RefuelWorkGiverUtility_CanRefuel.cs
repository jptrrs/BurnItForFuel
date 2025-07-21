using HarmonyLib;
using RimWorld;
using Verse;

namespace BurnItForFuel;

[HarmonyPatch(typeof(RefuelWorkGiverUtility), nameof(RefuelWorkGiverUtility.CanRefuel))]
public static class RefuelWorkGiverUtility_CanRefuel
{
    public static void Postfix(Pawn pawn, Thing t, bool forced, ref bool __result)
    {
        if (t.TryGetComp<CompSelectFuel>() != null)
        {
            __result = HarmonyPatches.CanRefuel(pawn, t, forced);
        }
    }
}