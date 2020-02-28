using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using HugsLib;
using HugsLib.Settings;
using static BurnItForFuel.ModBaseBurnItForFuel;

namespace BurnItForFuel
{
    [StaticConstructorOnStartup]
    public class CompSelectFuel : ThingComp, IStoreSettingsParent
    {
        public StorageSettings FuelSettings;

        public CompProperties_SelectFuel Props
        {
            get
            {
                return (CompProperties_SelectFuel)props;
            }
        }

        public bool StorageTabVisible { get; set; }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in StorageSettingsClipboard.CopyPasteGizmosFor(FuelSettings))
            {
                yield return g;
            }
            yield break;
        }

        public StorageSettings GetParentStoreSettings()
        {
            StorageSettings settings = new StorageSettings();
            if (StorageTabVisible) settings.filter = UserFuelSettings();
            else settings.filter = BaseFuelSettings(parent);
            return settings;
        }

        public StorageSettings GetStoreSettings()
        {
            return FuelSettings;
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            if (Scribe.mode != LoadSaveMode.PostLoadInit) SetupFuelSettings();
            SetUpFuelMixing();
        }

        public void SetupFuelSettings()
        {
            FuelSettings = new StorageSettings(this);
            if (BaseFuelSettings(parent) != null)
            {
                if ((!FuelSettingsIncludeBaseFuel() || IsVehicle()) && !parent.def.GetCompProperties<CompProperties_Refuelable>().atomicFueling)
                {
                    if (!FuelSettingsIncludeBaseFuel()) Log.Message("[BurnItForFuel] " + BaseFuelSettings(parent).ToString() + " is used by the " + parent.Label + ", but it isn't marked as fuel. Fuel tab disabled. Change the settings or add <atomicFueling>true</atomicFueling> to its CompProperties_Refuelable to prevent this.");
                    if (IsVehicle()) Log.Message("[BurnItForFuel] " + parent.LabelCap + " looks like its a vehicle, so we're preventing fuel mixing to protect your engines. Fuel tab disabled. Add <atomicFueling>true</atomicFueling> to its CompProperties_Refuelable to prevent this.");
                    FuelSettings.filter.SetAllowAll(BaseFuelSettings(parent));
                }
                else foreach (ThingDef thingDef in UserFuelSettings().AllowedThingDefs)
                {
                    FuelSettings.filter.SetAllow(thingDef, true);
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look<StorageSettings>(ref FuelSettings, "fuelSettings");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (FuelSettings == null) SetupFuelSettings();
                ValidateFuelSettings();
            }
        }

        public void PurgeFuelSettings()
        {
            IEnumerable<ThingDef> excess = FuelSettings.filter.AllowedThingDefs.Except(GetParentStoreSettings().filter.AllowedThingDefs);
            if (excess.Count() > 0)
            {
                foreach (ThingDef t in excess.ToList())
                {
                    FuelSettings.filter.SetAllow(t, false);
                }
            }
        }

        public void SetUpFuelMixing()
        {
            if (parent.TryGetComp<CompRefuelable>() != null && SafeToMixFuels())
            {
                parent.TryGetComp<CompRefuelable>().Props.atomicFueling = true;
                StorageTabVisible = true;
            }
            else StorageTabVisible = false;
        }

        public bool FuelSettingsIncludeBaseFuel() //e.g: Dubs Hygiene Burning Pit doesn't. 
        {
            bool flag = false;
            foreach (ThingDef thingDef in BaseFuelSettings(parent).AllowedThingDefs)
            {
                if (UserFuelSettings().Allows(thingDef))
                {
                    flag |= true;
                }
            }
            return flag;
        }

        public void ValidateFuelSettings()
        {
            if (Scribe.mode == LoadSaveMode.Inactive)
            {
                SetUpFuelMixing();
            }
            if (SafeToMixFuels())
            {
                foreach (ThingDef def in (from d in FuelSettings.filter.AllowedThingDefs
                                          where !GetParentStoreSettings().filter.Allows(d)
                                          select d).ToList())
                {
                    FuelSettings.filter.SetAllow(def, false);
                    Log.Warning("[BurnItForFuel] " + def.defName + " is no longer fuel, so it was removed from the " + parent + " fuel settings.");
                }
            }
        }

        private static ThingFilter BaseFuelSettings(ThingWithComps T)
        {
            if (T.def.comps != null)
            {
                for (int i = 0; i < T.def.comps.Count; i++)
                {
                    if (T.def.comps[i].compClass == typeof(CompRefuelable))
                    {
                        CompProperties_Refuelable comp = (CompProperties_Refuelable)T.def.comps[i];
                        return comp.fuelFilter;
                    }
                }
            }
            return null;
        }

        private static ThingFilter UserFuelSettings()
        {
            ModSettingsPack pack = HugsLibController.SettingsManager.GetModSettings("JPT_BurnItForFuel");
            return pack.GetHandle<FuelSettingsHandle>("FuelSettings").Value.masterFuelSettings;
        }

        private bool IsVehicle()
        {
            CompProperties_Refuelable props = parent.TryGetComp<CompRefuelable>().Props;
            return props.targetFuelLevelConfigurable && props.consumeFuelOnlyWhenUsed;
        }

        private bool SafeToMixFuels()
        {
            return FuelSettingsIncludeBaseFuel() && parent.def.passability != Traversability.Impassable && !parent.def.building.canPlaceOverWall && !IsVehicle();
        }
    }
}