using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace BurnItForFuel
{
    [StaticConstructorOnStartup]
    public static class Initializer
    {
        static BurnItForFuelSettings settings => BurnItForFuelMod.settings;

        static Initializer()
        {
            bool savedSettings = settings.DelayedLoading();
            settings.standardFuel = ThingDefOf.WoodLog;
            CompInjection();
            SetDefaultFuels();
            if (!savedSettings)
            {
                Log.Message($"[BurnItForFuel] Setting default fuels for the first time.");
                settings.masterFuelSettings.CopyAllowancesFrom(settings.defaultFuelSettings);
            }
        }

        private static void SetDefaultFuels()
        {
            var defaultsDef = ThingDef.Named("BurnItForFuel");
            if (defaultsDef == null)
            {
                Log.Warning("[BurnItForFuel] The definition for default fuels couldn't be found! Check the file 'Things.xml' for a ThingDef called 'BurnItForFuel'. The mod will still work, but this is will require manual selection of fuels from the options panel.");
                return;
            }
            foreach (var def in defaultsDef.building.fixedStorageSettings.filter.AllowedThingDefs)
            {
                if (def.ValidateAsFuel()) settings.defaultFuelSettings.SetAllow(def, true);
            }
            DefDatabase<ThingDef>.Remove(ThingDef.Named("BurnItForFuel"));
        }

        private static void CompInjection()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x.HasComp(typeof(CompRefuelable))))
            {
                def.comps.Add(new CompProperties_SelectFuel());
                if (def.inspectorTabs == null) def.inspectorTabs = new List<Type>();
                def.inspectorTabs.Add(typeof(ITab_Fuel));
            }
        }
    }
}