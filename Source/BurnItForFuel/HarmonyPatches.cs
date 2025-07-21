using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BurnItForFuel;

[StaticConstructorOnStartup]
public static class HarmonyPatches
{
    private static readonly Type patchType = typeof(HarmonyPatches);

    static HarmonyPatches()
    {
        new Harmony("JPT_BurnItForFuel").PatchAll(Assembly.GetExecutingAssembly());
    }

    public static bool CanRefuel(Pawn pawn, Thing t, bool forced = false)
    {
        var compRefuelable = t.TryGetComp<CompRefuelable>();
        if (compRefuelable == null || compRefuelable.IsFull || !forced && !compRefuelable.allowAutoRefuel)
        {
            return false;
        }

        if (compRefuelable.FuelPercentOfMax > 0f && !compRefuelable.Props.allowRefuelIfNotEmpty)
        {
            return false;
        }

        if (!forced && !compRefuelable.ShouldAutoRefuelNow)
        {
            return false;
        }

        if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        {
            return false;
        }

        if (t.Faction != pawn.Faction)
        {
            return false;
        }

        var compActivable = t.TryGetComp<CompInteractable>();
        if (compActivable != null && compActivable.Props.cooldownPreventsRefuel && compActivable.OnCooldown)
        {
            JobFailReason.Is(compActivable.Props.onCooldownString.CapitalizeFirst());
            return false;
        }

        var comp = t.TryGetComp<CompSelectFuel>();
        comp.PurgeFuelSettings();
        var fuelFilter = comp.FuelSettings.filter;
        if (FindBestFuel(pawn, t) == null)
        {
            JobFailReason.Is("NoFuelToRefuel".Translate(fuelFilter.Summary));
            return false;
        }

        if (!t.TryGetComp<CompRefuelable>().Props.atomicFueling || FindAllFuel(pawn, t) != null)
        {
            return true;
        }

        JobFailReason.Is("NoFuelToRefuel".Translate(fuelFilter.Summary));
        return false;
    }

    public static Thing FindBestFuel(Pawn pawn, Thing refuelable)
    {
        //Log.Message("FindBestFuel_Postfix for: "+refuelable);
        var filter = refuelable.TryGetComp<CompSelectFuel>().FuelSettings.filter;

        var position = pawn.Position;
        var map = pawn.Map;
        var bestThingRequest = filter.BestThingRequest;
        const PathEndMode peMode = PathEndMode.ClosestTouch;
        var traverseParams = TraverseParms.For(pawn);
        var validator = (Predicate<Thing>)predicate;
        return GenClosest.ClosestThingReachable(position, map, bestThingRequest, peMode, traverseParams, 9999f,
            validator);

        bool predicate(Thing x)
        {
            return !x.IsForbidden(pawn) && pawn.CanReserve(x) && filter.Allows(x);
        }
    }

    public static int GetFuelCountToFullyRefuel(CompRefuelable __instance)
    {
        if (__instance.Props.atomicFueling)
        {
            return Mathf.CeilToInt(__instance.Props.fuelCapacity / __instance.Props.FuelMultiplierCurrentDifficulty);
        }

        var f = (__instance.TargetFuelLevel - __instance.Fuel) / __instance.Props.FuelMultiplierCurrentDifficulty;
        return Mathf.Max(Mathf.CeilToInt(f), 1);
    }

    public static List<Thing> FindAllFuel(Pawn pawn, Thing refuelable)
    {
        var comp = refuelable.TryGetComp<CompRefuelable>();
        var quantity = GetFuelCountToFullyRefuel(comp);
        var filter = refuelable.TryGetComp<CompSelectFuel>().FuelSettings.filter;

        var position = refuelable.Position;
        var region =
            position.GetRegion(pawn
                .Map); // NOTE: comes out null if refuelable is inside a wall, even with matching RegionType. Why?
        var traverseParams = TraverseParms.For(pawn);

        var chosenThings = new List<Thing>();
        var accumulatedQuantity = 0;

        RegionTraverser.BreadthFirstTraverse(region, EntryCondition, RegionProcessor, 99999);
        return accumulatedQuantity >= quantity ? chosenThings : null;

        bool Validator(Thing x)
        {
            return !x.IsForbidden(pawn) && pawn.CanReserve(x) && filter.Allows(x);
        }

        bool EntryCondition(Region from, Region r)
        {
            return r.Allows(traverseParams, false);
        }

        bool RegionProcessor(Region r)
        {
            var list = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
            foreach (var thing in list)
            {
                if (!Validator(thing))
                {
                    continue;
                }

                if (chosenThings.Contains(thing))
                {
                    continue;
                }

                if (!ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch, pawn))
                {
                    continue;
                }

                chosenThings.Add(thing);
                accumulatedQuantity += thing.stackCount;
                if (accumulatedQuantity >= quantity)
                {
                    return true;
                }
            }

            return false;
        }
    }
}