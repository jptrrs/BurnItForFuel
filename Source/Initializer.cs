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
            CompInjection();
            if (!settings.HasEverBeenSet) SetDefaultFuelsOnce();
            DefDatabase<ThingDef>.Remove(ThingDef.Named("BurnItForFuel"));
        }

        private static void SetDefaultFuelsOnce()
        {
            var filter = new ThingFilter();
            var defaultsDef = ThingDef.Named("BurnItForFuel");
            StringBuilder errorMsg = new StringBuilder();
            if (defaultsDef == null)
            {
                errorMsg.Append("[BurnItForFuel] The definition for default fuels couldn't be found!");
                goto error;
            }
            filter.CopyAllowancesFrom(ThingDef.Named("BurnItForFuel").building.fixedStorageSettings.filter);
            if (filter.AllowedDefCount < 1)
            {
                errorMsg.Append("[BurnItForFuel] No fuels have been set by default.");
                goto error;
            }
            settings.masterFuelSettings = filter;
            settings.HasEverBeenSet = true;
            return;

            error:
            Log.Warning(errorMsg.ToString() + " Check the file 'Things.xml' for a ThingDef called 'BurnItForFuel'. The mod will still work, but this is will require manual selection of fuels from the options panel.");
            return;
        }

        private static void CompInjection()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x.HasComp(typeof(CompRefuelable))))
            {
                Log.Message("[BurnItForFuel] Adding CompSelectFuel to " + def.defName);
                def.comps.Add(new CompProperties_SelectFuel());
                if (def.inspectorTabs == null) def.inspectorTabs = new List<Type>();
                def.inspectorTabs.Add(typeof(ITab_Fuel));
                //if (def.inspectorTabsResolved == null) def.inspectorTabsResolved = new List<InspectTabBase>();
                //def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Fuel)));
            }
        }
    }
}