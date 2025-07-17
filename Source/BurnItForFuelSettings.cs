using Verse;

namespace BurnItForFuel
{
    public class BurnItForFuelSettings : ModSettings
    {
        public bool InvalidSettings => masterFuelSettings == null;
        public ThingFilter masterFuelSettings;

        public override void ExposeData()
        {
            //Scribe_Values.Look(ref HasEverBeenSet, "HasEverBeenSet");
            if (Scribe.mode.HasFlag(LoadSaveMode.Saving) || Scribe.mode.HasFlag(LoadSaveMode.PostLoadInit))
            {
                Scribe_Deep.Look(ref masterFuelSettings, "masterFuelSettings");
                if (masterFuelSettings == null)
                {
                    Log.Warning($"[BurnItForFuel] masterFuelSettings was null when exposed! scribe mode is {Scribe.mode}");
                }
                else
                {
                    Log.Message($"BurnItForFuelSettings: ExposeData called, scribe mode is {Scribe.mode}, master filter has {masterFuelSettings.allowedDefs.Count} items.");
                }
            }
            base.ExposeData();
        }
    }
}