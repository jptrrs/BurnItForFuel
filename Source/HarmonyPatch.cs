using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BurnItForFuel
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);

        static HarmonyPatches()
        {
            //Harmony.DEBUG = true;
            var harmonyInstance = new Harmony("JPT_BurnItForFuel");

            //harmonyInstance.Patch(original: AccessTools.Method(type: typeof(RefuelWorkGiverUtility), name: "CanRefuel"),
            //    prefix: null, postfix: new HarmonyMethod(patchType, nameof(CanRefuel_Postfix)), transpiler: null);

            //harmonyInstance.Patch(original: AccessTools.Method(type: typeof(RefuelWorkGiverUtility), name: "FindBestFuel"),
            //    prefix: null, postfix: new HarmonyMethod(patchType, nameof(FindBestFuel_Postfix)), transpiler: null);

            //harmonyInstance.Patch(original: AccessTools.Method(type: typeof(RefuelWorkGiverUtility), name: "FindAllFuel"),
            //    prefix: null, postfix: new HarmonyMethod(patchType, nameof(FindAllFuel_Postfix)), transpiler: null);

            //harmonyInstance.Patch(original: AccessTools.Method(typeof(CompRefuelable), name: "GetFuelCountToFullyRefuel"),
            //    null, new HarmonyMethod(patchType, nameof(GetFuelCountToFullyRefuel_Postfix)));

            //test
            harmonyInstance.Patch(AccessTools.Method(typeof(RefuelWorkGiverUtility), "FindAllFuel"),
                null, null, new HarmonyMethod(patchType, nameof(FuelFilter_Transpiler)));
            harmonyInstance.Patch(AccessTools.Method(typeof(RefuelWorkGiverUtility), "FindBestFuel"),
                null, null, new HarmonyMethod(patchType, nameof(FuelFilter_Transpiler)));


        }

        //public static bool CompRefuelable_Props_Prefix(CompRefuelable __instance, ref CompProperties __result)
        //{
        //    var compSelectFuel = __instance.parent.TryGetComp<CompSelectFuel>();
        //    if (compSelectFuel != null)
        //    {
        //        __result = compSelectFuel.Props;
        //        return false;
        //    }
        //    return true;
        //}

        //public static void CanRefuel_Postfix(object __instance, Pawn pawn, Thing t, bool forced, ref bool __result)
        //{
        //    if (t.TryGetComp<CompSelectFuel>() != null)
        //    {
        //        __result = CanRefuel(pawn, t, forced);
        //    }
        //}

        //public static bool CanRefuel(Pawn pawn, Thing t, bool forced = false)
        //{
        //    CompRefuelable compRefuelable = t.TryGetComp<CompRefuelable>();
        //    if (compRefuelable == null || compRefuelable.IsFull || (!forced && !compRefuelable.allowAutoRefuel))
        //    {
        //        return false;
        //    }
        //    if (!forced && !compRefuelable.ShouldAutoRefuelNow)
        //    {
        //        return false;
        //    }
        //    if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        //    {
        //        return false;
        //    }
        //    if (t.Faction != pawn.Faction)
        //    {
        //        return false;
        //    }
        //    ThingFilter fuelFilter = new ThingFilter();
        //    CompSelectFuel comp = t.TryGetComp<CompSelectFuel>();
        //    comp.PurgeFuelSettings(); //Can we improve this so it's called only when necessary?
        //    fuelFilter = comp.FuelSettings.filter;
        //    if (RefuelWorkGiverUtility.FindBestFuel(pawn, t) == null) //diverted to our own.
        //    {
        //        JobFailReason.Is("NoFuelToRefuel".Translate(fuelFilter.Summary), null);
        //        return false;
        //    }
        //    if (t.TryGetComp<CompRefuelable>().Props.atomicFueling && FindAllFuel(pawn, t) == null) //diverted to our own.
        //    {
        //        JobFailReason.Is("NoFuelToRefuel".Translate(fuelFilter.Summary), null);
        //        return false;
        //    }
        //    return true;
        //}

        //public static void FindBestFuel_Postfix(Pawn pawn, Thing refuelable, ref Thing __result)
        //{
        //    if (refuelable.TryGetComp<CompSelectFuel>() != null)
        //    {
        //        __result = FindBestFuel(pawn, refuelable);
        //    }
        //}

        //private static Thing FindBestFuel(Pawn pawn, Thing refuelable)
        //{
        //    ThingFilter filter = new ThingFilter();
        //    filter = refuelable.TryGetComp<CompSelectFuel>().FuelSettings.filter; //filter diverted to our own.
        //    Predicate<Thing> predicate = (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, false) && filter.Allows(x);
        //    IntVec3 position = pawn.Position;
        //    Map map = pawn.Map;
        //    ThingRequest bestThingRequest = filter.BestThingRequest;
        //    PathEndMode peMode = PathEndMode.ClosestTouch;
        //    TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
        //    Predicate<Thing> validator = predicate;
        //    return GenClosest.ClosestThingReachable(position, map, bestThingRequest, peMode, traverseParams, 9999f, validator, null, 0, -1, false, RegionType.Set_Passable, false);
        //}

        //public static void FindAllFuel_Postfix(Pawn pawn, Thing refuelable, ref List<Thing> __result, MethodInfo __originalMethod)
        //{
        //    if (refuelable.TryGetComp<CompSelectFuel>() != null)
        //    {
        //        __result = FindAllFuel(pawn, refuelable);
        //    }
        //}

        //private static List<Thing> FindAllFuel(Pawn pawn, Thing refuelable)
        //{
        //    CompRefuelable comp = refuelable.TryGetComp<CompRefuelable>();
        //    int quantity = GetFuelCountToFullyRefuel(comp); //diverted to our own.
        //    ThingFilter filter = new ThingFilter();
        //    filter = refuelable.TryGetComp<CompSelectFuel>().FuelSettings.filter; //filter diverted to our own.

        //    from here, doing the work of FindEnoughReservableThings:
        //    Predicate<Thing> validator = (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, false) && filter.Allows(x);
        //    IntVec3 position = refuelable.Position;
        //    Region region = position.GetRegion(pawn.Map, RegionType.Set_Passable); // NOTE: comes out null if refuelable is inside a wall, even with matching RegionType. Why?
        //    TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
        //    RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParams, false);
        //    List<Thing> chosenThings = new List<Thing>();
        //    int accumulatedQuantity = 0;
        //    RegionProcessor regionProcessor = delegate (Region r)
        //    {
        //        List<Thing> list = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
        //        for (int i = 0; i < list.Count; i++)
        //        {
        //            Thing thing = list[i];
        //            if (validator(thing) && !chosenThings.Contains(thing) && ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch, pawn))
        //            {
        //                chosenThings.Add(thing);
        //                accumulatedQuantity += thing.stackCount;
        //                if (accumulatedQuantity >= quantity) return true;
        //            }
        //        }
        //        return false;
        //    };
        //    RegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, 99999, RegionType.Set_Passable);
        //    if (accumulatedQuantity >= quantity) return chosenThings;
        //    return null;
        //}

        //public static void GetFuelCountToFullyRefuel_Postfix(CompRefuelable __instance, ref int __result)
        //{
        //    __result = GetFuelCountToFullyRefuel(__instance);
        //}

        //public static int GetFuelCountToFullyRefuel(CompRefuelable __instance) //skips measures for "atomicFueling"
        //{
        //    float f = (__instance.TargetFuelLevel - __instance.Fuel) / __instance.Props.FuelMultiplierCurrentDifficulty;
        //    return Mathf.Max(Mathf.CeilToInt(f), 1);
        //}

        static IEnumerable<CodeInstruction> FuelFilter_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> code = instructions.ToList();
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Callvirt && (MethodInfo)code[i].operand == AccessTools.PropertyGetter(typeof(CompRefuelable), "Props"))
                {
                    code.RemoveAt(i - 1); // TryGetComp<CompRefuelable>()
                    code.RemoveAt(i - 1); // .Props
                    code.RemoveAt(i - 1); // .fuelFilter
                    code.Insert(i - 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThingCompUtility), "TryGetComp", new Type[] { typeof(Thing) }, new Type[] { typeof(CompSelectFuel) }))); // TryGetComp<CompSelectFuel>()
                    code.Insert(i, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompSelectFuel), nameof(CompSelectFuel.FuelSettings)))); // .FuelSettings
                    code.Insert(i + 1, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StorageSettings), nameof(StorageSettings.filter)))); // .filter
                    break;
                }
            }
            foreach (var c in code) yield return c;
        }

        //static IEnumerable<CodeInstruction> FindBestFuel_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        //{
        //    List<CodeInstruction> code = instructions.ToList();
        //    MethodInfo target = AccessTools.Method(typeof(ThingCompUtility), nameof(ThingCompUtility.TryGetComp), new Type[] { typeof(Thing) }, new Type[] { typeof(CompRefuelable) });
        //    MethodInfo changeling = AccessTools.Method(typeof(ThingCompUtility), nameof(ThingCompUtility.TryGetComp), new Type[] { typeof(Thing) }, new Type[] { typeof(CompSelectFuel) });
        //    FieldInfo fuelSettings = AccessTools.Field(typeof(CompSelectFuel), nameof(CompSelectFuel.FuelSettings));
        //    FieldInfo filter = AccessTools.Field(typeof(StorageSettings), nameof(StorageSettings.filter));

        //    for (int i = 0; i < code.Count; i++)
        //    {
        //        if (code[i].opcode == OpCodes.Call && (MethodInfo)code[i].operand == target)
        //        {
        //            code.RemoveAt(i); // TryGetComp<CompRefuelable>()
        //            code.RemoveAt(i); // .Props
        //            code.RemoveAt(i); // .fuelFilter
        //            code.Insert(i , new CodeInstruction(OpCodes.Call, changeling));
        //            code.Insert(i + 1, new CodeInstruction(OpCodes.Ldfld, fuelSettings));
        //            code.Insert(i + 2, new CodeInstruction(OpCodes.Ldfld, filter));
        //            break;
        //        }
        //    }
        //    foreach (var c in code) yield return c;
        //}

    }
}