using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BurnItForFuel
{
    [StaticConstructorOnStartup]
    public class CompSelectFuel : ThingComp, IStoreSettingsParent
    {
        public StorageSettings FuelSettings;
        private ThingFilter baseFuelSettings;
        private bool? fuelSettingsIncludeBaseFuel;
        private float baseFuelValue;

        public float BaseFuelValue
        {
            get
            {
                if (baseFuelValue != 0f) return baseFuelValue;
                if (BaseFuelSettings == null || BaseFuelSettings.AllowedThingDefs.EnumerableNullOrEmpty())
                {
                    Log.Warning($"[BurnItForFuel] Base fuel for {parent.LabelCap} is undefined!");
                    return 0f;
                }
                baseFuelValue = BaseFuelSettings.AllowedThingDefs.Select(t => t.UnitFuelValue()).Min();
                return baseFuelValue;
            }
        }

        public float EquivalentFuelFactor(ThingDef def)
        {
            if (BaseFuelValue <= 0f)
            {
                Log.Error($"[BurnItForFuel] Invalid base fuel assigned to {parent.LabelCap}.");
                return 0f;
            }
            var unitValue = def.UnitFuelValue();
            return unitValue > 0 ? unitValue / BaseFuelValue : 0f;
        }

        public bool FuelSettingsIncludeBaseFuel //e.g: Dubs Hygiene Burning Pit doesn't. 
        {
            get
            {
                if (fuelSettingsIncludeBaseFuel != null || BaseFuelSettings == null) return fuelSettingsIncludeBaseFuel.Value;
                foreach (ThingDef thingDef in BaseFuelSettings.AllowedThingDefs)
                {
                    if (UserFuelSettings.Allows(thingDef))
                    {
                        fuelSettingsIncludeBaseFuel = true;
                        return true;
                    }
                }
                fuelSettingsIncludeBaseFuel = false;
                return false;
            }
        }

        public CompProperties_SelectFuel Props
        {
            get
            {
                return (CompProperties_SelectFuel)props;
            }
        }

        public bool StorageTabVisible { get; set; }

        private ThingFilter BaseFuelSettings
        {
            get
            {
                if (baseFuelSettings != null) return baseFuelSettings;
                if (parent.def.comps != null)
                {
                    List<CompProperties> comps = parent.def.comps;
                    for (int i = 0; i < comps.Count; i++)
                    {
                        if (comps[i].compClass == typeof(CompRefuelable))
                        {
                            CompProperties_Refuelable comp = (CompProperties_Refuelable)comps[i];
                            if (comp.fuelFilter == null)
                            {
                                Log.Warning("[BurnItForFuel] " + parent.LabelCap + " has a CompRefuelable, but it doesn't have a fuel filter set. This will cause issues with fuel selection.");
                                return null;
                            }
                            baseFuelSettings = comp.fuelFilter;
                            return baseFuelSettings;
                        }
                    }
                }
                return null;
            }
        }

        private ThingFilter UserFuelSettings => BurnItForFuelMod.settings.masterFuelSettings;

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
            if (StorageTabVisible) settings.filter = UserFuelSettings;
            else settings.filter = BaseFuelSettings;
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

        public void Notify_SettingsChanged()
        {
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

        public void SetupFuelSettings()
        {
            FuelSettings = new StorageSettings(this);
            if (BaseFuelSettings != null)
            {
                if ((!FuelSettingsIncludeBaseFuel || IsVehicle()) && !parent.def.GetCompProperties<CompProperties_Refuelable>().atomicFueling)
                {
                    if (!FuelSettingsIncludeBaseFuel) 
                    { 
                        Log.Message("[BurnItForFuel] " + BaseFuelSettings.ToString() + " is used by the " + parent.Label + ", but it isn't marked as fuel. Fuel tab disabled. Change the settings or add <atomicFueling>true</atomicFueling> to its CompProperties_Refuelable to prevent this.");
                    }
                    if (IsVehicle()) Log.Message("[BurnItForFuel] " + parent.LabelCap + " looks like its a vehicle, so we're preventing fuel mixing to protect your engines. Fuel tab disabled. Add <atomicFueling>true</atomicFueling> to its CompProperties_Refuelable to prevent this.");
                    FuelSettings.filter.SetAllowAll(BaseFuelSettings);
                }
                else foreach (ThingDef thingDef in UserFuelSettings.AllowedThingDefs)
                {
                    FuelSettings.filter.SetAllow(thingDef, true);
                }
            }
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

        private bool IsVehicle()
        {
            CompProperties_Refuelable props = parent.TryGetComp<CompRefuelable>().Props;
            return props.targetFuelLevelConfigurable && props.consumeFuelOnlyWhenUsed;
        }

        private bool SafeToMixFuels()
        {
            return FuelSettingsIncludeBaseFuel && parent.def.passability != Traversability.Impassable && !parent.def.building.canPlaceOverWall && !IsVehicle();
        }


    }
}