using RimWorld;
using System;
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
            float flamm, mass;
            if (!def.ValidateAsFuel(out flamm, out mass, showError)) return 0f;
            float fuelValue = mass * flamm;
            fuelValueCache.Add(def, fuelValue);
            return fuelValue;
        }

        public static bool ValidateAsFuel(this ThingDef def, out float flamm, out float mass, bool showError = true)
        {
            string errorMsg;
            if (def.statBases == null)
            {
                errorMsg = "having no stat bases";
                flamm = mass = 0f;
                goto Zero;
            }
            flamm = def.GetStatValueAbstract(StatDefOf.Flammability);
            if (flamm <= 0f)
            {
                errorMsg = "being inflammable";
                mass = 0f;
                goto Zero;
            }
            mass = def.GetStatValueAbstract(StatDefOf.Mass);
            if (mass <= 0f)
            {
                errorMsg = "having no mass";
                goto Zero;
            }
            return true;

            Zero:
            if (showError) Log.Error($"[BurnItForFuel] {def.defName} can't be used as fuel due to {errorMsg}."); //migh not be needed at all, if we gatekeep defs properly.
            return false;
        }

        public static bool ValidateAsFuel(this ThingDef def)
        {
            float flamm, mass;
            return ValidateAsFuel(def, out flamm, out mass, false);
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