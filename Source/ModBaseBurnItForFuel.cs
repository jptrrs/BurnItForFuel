﻿using HugsLib;
using HugsLib.Settings;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BurnItForFuel
{
    public class ModBaseBurnItForFuel : ModBase
    {
        public ModBaseBurnItForFuel()
        {
            Settings.EntryName = "Burn It For Fuel";
        }
        
        public override string ModIdentifier
        {
            get
            {
                return "JPT_BurnItForFuel";
            }
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

        private static ThingFilter DefaultFuels
        {
            get
            {
                var filter = new ThingFilter();
                filter.CopyAllowancesFrom(ThingDef.Named("BurnItForFuel").building.fixedStorageSettings.filter);
                return filter;
            }
        }

        private SettingHandle<bool> HasEverBeenSet ;

        public SettingHandle<FuelSettingsHandle> Fuels;

        public override void DefsLoaded()
        {
            HasEverBeenSet = Settings.GetHandle<bool>("HasEverBeenSet", null, null, false);
            HasEverBeenSet.NeverVisible = true;
            Fuels = Settings.GetHandle<FuelSettingsHandle>("FuelSettings", "", null, null);
            if (Fuels.Value == null) Fuels.Value = new FuelSettingsHandle();
            if (Fuels.Value.masterFuelSettings.AllowedDefCount == 0 && !HasEverBeenSet)
            {
                Log.Message("[BurnItForFuel] Populating fuel settings for the first time. Default fuels are: "+DefaultFuels.AllowedThingDefs.ToStringSafeEnumerable()+".");
                Fuels.Value.masterFuelSettings = DefaultFuels;
                HasEverBeenSet.Value = true;
            }
            Fuels.CustomDrawerHeight = 320f;
            Fuels.CustomDrawer = rect => SettingsUI.CustomDrawer_ThingFilter(rect, ref Fuels.Value.masterFuelSettings, PossibleFuels, DefaultFuels, Fuels);
        }

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