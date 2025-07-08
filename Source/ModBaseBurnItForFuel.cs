using HarmonyLib;
using HugsLib;
using HugsLib.Settings;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Verse;

namespace BurnItForFuel
{
    public class ModBaseBurnItForFuel : ModBase
    {
        public SettingHandle<FuelSettingsHandle> Fuels;

        private SettingHandle<bool> HasEverBeenSet;

        public ModBaseBurnItForFuel()
        {
            BurnItForFuelSettings.EntryName = "Burn It For Fuel";
        }

        public static ThingFilter PossibleFuels
        {
            get
            {
                var filter = new ThingFilter();
                IEnumerable<ThingDef> fuels = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.IsWithinCategory(ThingCategoryDefOf.Root));
                foreach (ThingDef def in fuels)
                {
                    filter.SetAllow(def, true);
                }
                return filter;
            }
        }

        public override string ModIdentifier
        {
            get
            {
                return "JPT_BurnItForFuel";
            }
        }

        private void SetDefaultFuelsOnce()
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
            Log.Message("[BurnItForFuel] Populating fuel settings for the first time. Default fuels: " + filter.categories.ToStringSafeEnumerable() + ". Default fuel categories: " + filter.AllowedThingDefs.ToStringSafeEnumerable() + "."); 
            Fuels.Value.masterFuelSettings = filter;
            HasEverBeenSet.Value = true;
            return;

            error:
            Log.Warning(errorMsg.ToString() + " Check the file 'Things.xml' for a ThingDef called 'BurnItForFuel'. The mod will still work, but this is will require manual selection of fuels from the options panel.");
            return;                
        }

        public override void DefsLoaded()
        {
            HasEverBeenSet = BurnItForFuelSettings.GetHandle<bool>("HasEverBeenSet", null, null, false);
            HasEverBeenSet.NeverVisible = true;
            Fuels = BurnItForFuelSettings.GetHandle<FuelSettingsHandle>("FuelSettings", "", null, null);
            if (Fuels.Value == null) Fuels.Value = new FuelSettingsHandle();
            if (!HasEverBeenSet) SetDefaultFuelsOnce();
            Fuels.CustomDrawerHeight = 320f;
            Fuels.CustomDrawer = rect => SettingsUI.CustomDrawer_ThingFilter(rect, Fuels.Value.masterFuelSettings, PossibleFuels, Fuels.Value.masterFuelSettings, Fuels);
            AccessTools.Method(typeof(DefDatabase<ThingDef>), "Remove").Invoke(this, new object[] { ThingDef.Named("BurnItForFuel") });
        }

        //This still needs porting!
        public override void SettingsChanged()
        {
            base.SettingsChanged();
            if (Current.ProgramState == ProgramState.Playing)
            {
                Find.Maps.ForEach(delegate(Map map) 
                {
                    List<Thing> affected = map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Refuelable));
                    foreach (Thing t in affected)
                    {
                        Building b = t as Building;
                        if (b != null)
                        {
                            CompSelectFuel comp = b.GetComp<CompSelectFuel>();
                            if (comp != null)
                            {
                                comp.ValidateFuelSettings();
                            }
                        }
                    }
                });
            }
        }

        public class FuelSettingsHandle : SettingHandleConvertible
        {
            public ThingFilter masterFuelSettings = new ThingFilter();

            public override void FromString(string settingValue)
            {
                List<ThingDef> defList = settingValue.Replace(", ", ",").Split(',').ToList().ConvertAll(e => DefDatabase<ThingDef>.GetNamedSilentFail(e));
                foreach (ThingDef def in defList)
                { 
                    if(def != null) masterFuelSettings.SetAllow(def, true); 
                }
            }

            public override string ToString()
            {
                return masterFuelSettings.AllowedThingDefs.ToStringSafeEnumerable();
            }

        }

    }
}