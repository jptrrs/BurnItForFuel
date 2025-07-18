using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BurnItForFuel
{
    public class BurnItForFuelSettings : ModSettings
    {
        public ThingFilter masterFuelSettings;
        private List<string> ExposedList = new List<string>();

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving && masterFuelSettings != null)
            {
                ExposedList = masterFuelSettings.AllowedThingDefs.Select(x => x.defName).ToList();
                SettingsChanged();
            }
            Scribe_Collections.Look(ref ExposedList, "masterFuelSettings", LookMode.Value);
            base.ExposeData();
        }

        public void DelayedLoading() //Apparently the DefDatabase wasn't ready before and we couldn't load ThingDefs.
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

        public void SettingsChanged()
        {
            if (Current.ProgramState != ProgramState.Playing) return;
            foreach (Map map in Find.Maps)
            {
                List<Thing> affected = map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Refuelable));
                foreach (Thing t in affected)
                {
                    if (t is Building b) b.GetComp<CompSelectFuel>()?.ValidateFuelSettings();
                }
            }
        }
    }
}