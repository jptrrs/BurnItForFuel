using RimWorld;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace BurnItForFuel
{
    public class BurnItForFuelSettings : ModSettings
    {
        public ThingFilter
            masterFuelSettings = new ThingFilter(),
            defaultFuelSettings = new ThingFilter();

        public ThingDef standardFuel; //used for the potential fuel calculation in abstract terms, defined on the Initializer class.

        public bool
            useMass = true,
            useFlamm = true,
            enableTargetFuelLevel = true,
            enableWithNonFuel = false,
            showFuelPotential = false,
            showInvalids = false; //for dev mode, to see what fuels are available.
        
        public Dictionary<ThingDef, List<ThingDef>> UserCustomFuels = new Dictionary<ThingDef, List<ThingDef>>();
        private List<string> ExposedList = new List<string>();

        public byte FuelPotentialValuesState => BoolArrayToByte(new bool[] { useMass, useFlamm });

        public ThingFilter PossibleFuels
        {
            get
            {
                var filter = new ThingFilter();
                IEnumerable<ThingDef> fuels = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.IsWithinCategory(ThingCategoryDefOf.Root) && (showInvalids || d.ValidateAsFuel()));
                foreach (ThingDef def in fuels)
                {
                    filter.SetAllow(def, true);
                }
                filter.allowedHitPointsConfigurable = false;
                filter.allowedQualitiesConfigurable = false;
                return filter;
            }
        }

        public byte BoolArrayToByte(bool[] boolArray) //nifty little thing!
        {
            byte result = 0;
            for (int i = 0; i < boolArray.Length; i++)
            {
                if (boolArray[i])
                {
                    result |= (byte)(1 << i);
                }
            }
            return result;
        }

        public void CustomFuelsOnDemand(bool saving)
        {
            var previous = Scribe.mode;
            Scribe.mode = LoadSaveMode.Inactive;
            string label = "UserCustomFuels";
            string filename = LoadedModManager.GetSettingsFilename(Mod.Content.FolderName, $"{Mod.GetType().Name}_{label}");
            if (saving)
            {
                Scribe.saver.InitSaving(filename, label);
            }
            else
            {
                if (!File.Exists(filename)) return;
                Scribe.loader.InitLoading(filename);
            }
            try
            {
                Scribe_Collections.Look(ref UserCustomFuels, label, LookMode.Def, LookMode.Def);
            }
            finally
            {
                if (saving) Scribe.saver.FinalizeSaving();
                else Scribe.loader.FinalizeLoading();
                Scribe.mode = previous;
            }
        }

        public bool DelayedLoading() //Apparently the DefDatabase wasn't ready before and we couldn't load ThingDefs.
        {
            CustomFuelsOnDemand(false);
            if (ExposedList.Empty()) return false;
            foreach (var e in ExposedList)
            {
                var def = DefDatabase<ThingDef>.GetNamed(e);
                if (def != null && def.ValidateAsFuel())
                {
                    masterFuelSettings.SetAllow(def, true);
                }
            }
            ExposedList.Clear();
            return true;
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving && masterFuelSettings != null)
            {
                ExposedList = masterFuelSettings.AllowedThingDefs.Select(x => x.defName).ToList();
                if (Current.ProgramState == ProgramState.Playing) RedistributeFuelSettings();
            }
            Scribe_Collections.Look(ref ExposedList, "masterFuelSettings", LookMode.Value);
            Scribe_Values.Look(ref useMass, "useMass", true);
            Scribe_Values.Look(ref useFlamm, "useFlamm", true);
            Scribe_Values.Look(ref enableTargetFuelLevel, "enableTargetFuelLevel", true);
            Scribe_Values.Look(ref enableWithNonFuel, "enableWithNonFuel", false);
            Scribe_Values.Look(ref showFuelPotential, "showFuelPotential", false);
            base.ExposeData();
        }

        public void RedistributeFuelSettings()
        {
            foreach (Map map in Find.Maps)
            {
                List<Thing> affected = map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Refuelable));
                foreach (Thing t in affected)
                {
                    if (t is Building b) b.GetComp<CompSelectFuel>()?.ValidateFuelSettings(true);
                }
            }
        }

        public void ResetFuelSettings()
        {
            masterFuelSettings.CopyAllowancesFrom(defaultFuelSettings);
        }
    }
}
