using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BurnItForFuel
{
    internal static class SelectFuelHelper
    {
        public static Dictionary<ThingDef, float> fuelValueCache = new Dictionary<ThingDef, float>();
        private static BurnItForFuelSettings Settings => BurnItForFuelMod.settings;

        public static float UnitFuelValue(this ThingDef def, bool showError = false)
        {
            if (fuelValueCache.ContainsKey(def)) return fuelValueCache[def];
            float flamm, mass;
            if (!def.ValidateAsFuel(out flamm, out mass, showError)) return 0f;
            float fuelValue = mass * flamm;
            fuelValueCache.Add(def, fuelValue);
            return fuelValue;
        }

        public static void ResetFuelValueCache()
        {
            fuelValueCache.Clear();
        }

        public static bool ValidateAsFuel(this ThingDef def, out float flamm, out float mass, bool showError = false)
        {
            string errorMsg;
            if (def.statBases == null)
            {
                errorMsg = "having no stat bases";
                flamm = mass = 0f;
                goto Zero;
            }
            flamm = Settings.useFlamm ? def.GetStatValueAbstract(StatDefOf.Flammability) : 1f;
            if (flamm <= 0f)
            {
                errorMsg = "being inflammable";
                mass = 0f;
                goto Zero;
            }
            mass = Settings.useMass ? def.GetStatValueAbstract(StatDefOf.Mass): 1f;
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

        public static bool ValidateAsFuel(this ThingDef def, bool showError = false)
        {
            float flamm, mass;
            return ValidateAsFuel(def, out flamm, out mass, showError);
        }

        public static float AbsoluteFuelRatio(this ThingDef def)
        {
            var ratio = def.UnitFuelValue() / Settings.standardFuel.UnitFuelValue();
            return ratio;
        }
    }
}