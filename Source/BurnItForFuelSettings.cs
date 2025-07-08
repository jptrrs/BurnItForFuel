using Verse;

namespace BurnItForFuel
{
    public class BurnItForFuelSettings : ModSettings
    {
        public bool HasEverBeenSet;
        public ThingFilter masterFuelSettings;

        public override void ExposeData()
        {
            if (Scribe.mode.HasFlag(LoadSaveMode.PostLoadInit | LoadSaveMode.Saving))
            {
                Scribe_Values.Look(ref HasEverBeenSet, "HasEverBeenSet");
                Scribe_Deep.Look(ref masterFuelSettings, "masterFuelSettings");
            }
            base.ExposeData();
        }
    }
}