using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BurnItForFuel
{
    internal static class SelectFuelHelper
    {
        public static Dictionary<ThingDef, float> fuelValueCache = new Dictionary<ThingDef, float>();

        public static float UnitFuelValue(this ThingDef def)
        {
            if (fuelValueCache.ContainsKey(def)) return fuelValueCache[def];
            if (def.statBases == null)
            {
                Log.Error($"[BurnItForFuel] {def.defName} has no stat bases, so it can't be used as fuel.");
                return 0f;
            }
            float fuelValue = def.GetStatValueAbstract(StatDefOf.Mass) * def.GetStatValueAbstract(StatDefOf.Flammability);
            fuelValueCache.Add(def, fuelValue);
            return fuelValue;
        }

        public static float FuelEquivalenceRatio(ThingDef standard, ThingDef sample)
        {
            var ratio = sample.UnitFuelValue() / standard.UnitFuelValue();
            if (ratio <= 0f)
            {
                Log.Error("[BurnItForFuel] " + sample.defName + " has a fuel equivalence ratio of 0 or less, which is invalid.");
                return 0f;
            }
            return ratio;
        }

    }
}