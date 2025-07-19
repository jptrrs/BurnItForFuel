using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BurnItForFuel
{
    internal static class SelectFuelHelper
    {
        public static Dictionary<ThingDef, float> fuelValueCache = new Dictionary<ThingDef, float>();

        public static float UnitFuelValue(this ThingDef def, bool showError = true)
        {
            if (fuelValueCache.ContainsKey(def)) return fuelValueCache[def];
            string errorMsg;
            if (def.statBases == null)
            {
                errorMsg = "having no stat bases";
                goto Zero;
            }
            float flamm = def.GetStatValueAbstract(StatDefOf.Flammability);
            if (flamm <= 0f)
            {
                errorMsg = "being inflammable";
                goto Zero;
            }
            float mass = def.GetStatValueAbstract(StatDefOf.Mass);
            if (mass <= 0f)
            {
                errorMsg = "having no mass";
                goto Zero;
            }
            float fuelValue = mass * flamm;
            fuelValueCache.Add(def, fuelValue);
            return fuelValue;

            Zero:
            if (showError) Log.Error($"[BurnItForFuel] {def.defName} can't be used as fuel due to {errorMsg}.");
            return 0f;
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