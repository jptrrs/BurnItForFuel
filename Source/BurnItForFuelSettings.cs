using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BurnItForFuel
{
    public class BurnItForFuelSettings : ModSettings
    {
        public ThingFilter masterFuelSettings;
        public List<string> ExposedList = new List<string>();

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving && masterFuelSettings != null)
            {
                ExposedList = masterFuelSettings.AllowedThingDefs.Select(x => x.defName).ToList();
            }
            Scribe_Collections.Look(ref ExposedList, "masterFuelSettings", LookMode.Value);
            base.ExposeData();
        }

        public void DelayedLoading() //Apparently the DefDatabase wasn't ready before.
        {
            masterFuelSettings = new ThingFilter();
            foreach (var e in ExposedList)
            {
                var def = DefDatabase<ThingDef>.GetNamed(e);
                if (def != null)
                {
                    masterFuelSettings.SetAllow(def, true);
                }
            }
            ExposedList.Clear();
        }
    }
}