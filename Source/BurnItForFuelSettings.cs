using System.Collections.Generic;
using Verse;

namespace BurnItForFuel
{
    public class BurnItForFuelSettings : ModSettings
    {
        public ThingFilter masterFuelSettings;

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                //Scribe_Deep.Look<ThingFilter>(ref masterFuelSettings, true, "masterFuelSettings", new object[] { this });
                var fuels = masterFuelSettings?.AllowedThingDefs ?? new HashSet<ThingDef>();
                //Scribe_Collections.Look(ref fuels, "masterFuelSettings");
                if (masterFuelSettings == null)
                {
                    Log.Warning($"[BurnItForFuel] masterFuelSettings was null when exposed! scribe mode is {Scribe.mode}");
                }
                else
                {
                    Log.Message($"BurnItForFuelSettings: ExposeData called, scribe mode is {Scribe.mode}, master filter has {masterFuelSettings.AllowedDefCount} items.");
                }
            }
            base.ExposeData();
        }
    }
}