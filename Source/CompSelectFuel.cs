using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BurnItForFuel
{
    [StaticConstructorOnStartup]
    public class CompSelectFuel : ThingComp, IStoreSettingsParent
    {
        public StorageSettings FuelSettings;
        public ThingDef lastEquivalentFuel;
        private float baseFuelValue;
        private bool? fuelSettingsIncludeBaseFuel;
        private CompRefuelable siblingComp;

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
        public CompRefuelable SiblingComp
        {
            get
            {
                if (siblingComp == null)
                {
                    siblingComp = parent.TryGetComp<CompRefuelable>();
                    if (siblingComp == null)
                    {
                        Log.Error($"[BurnItForFuel] {parent.LabelCap} has a CompSelectFuel but no CompRefuelable!");
                    }
                }
                return siblingComp;
            }
        }
        public bool StorageTabVisible { get; set; }

        private ThingFilter BaseFuelSettings => SiblingComp.Props.fuelFilter;
        private bool ClearedForFuelSelection => settings.enableWithNonFuel || FuelSettingsIncludeBaseFuel;
        private ThingFilter UserFuelSettings => BurnItForFuelMod.settings.masterFuelSettings;
        private bool SafeToMixFuels => parent.def.passability != Traversability.Impassable && !parent.def.building.canPlaceOverWall;
        private static BurnItForFuelSettings settings => BurnItForFuelMod.settings;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in StorageSettingsClipboard.CopyPasteGizmosFor(FuelSettings))
            {
                yield return g;
            }
            yield break;
        }

        public float EquivalentFuelRatio(ThingDef def)
        {
            if (def == null)
            {
                Log.Error($"[BurnItForFuel] Can't calculate fuel equivalence for a null def.");
                goto Zero;
            }
            lastEquivalentFuel = def;
            if (BaseFuelValue <= 0f)
            {
                Log.Error($"[BurnItForFuel] Invalid base fuel assigned to {parent.LabelCap}.");
                goto Zero;
            }
            var unitValue = def.UnitFuelValue();
            return unitValue > 0 ? unitValue / BaseFuelValue : 0f;
            Zero:
            return 0f;
        }

        public int GetFuelCountToFullyRefuel() //modified from the original to ignore atomicFueling, so it always considers the target fuel level
        {
            return Mathf.Max(Mathf.CeilToInt((SiblingComp.TargetFuelLevel - SiblingComp.fuel) / SiblingComp.Props.FuelMultiplierCurrentDifficulty), 1);
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
            SetUpFuelFeatures();
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

        public void Refuel(List<Thing> fuelThings)
        {
            if (SiblingComp.Props.atomicFueling)
            {
                if (fuelThings.Sum((Thing t) => Mathf.FloorToInt(t.stackCount * EquivalentFuelRatio(t.def))) < GetFuelCountToFullyRefuel())
                {
                    Log.ErrorOnce("Error refueling; not enough fuel available for proper atomic refuel", 19586442);
                    return;
                }
            }
            int fuelCount = GetFuelCountToFullyRefuel();
            while (fuelCount > 0 && fuelThings.Count > 0)
            {
                Thing thing = fuelThings.Pop<Thing>();
                float fuelFactor = EquivalentFuelRatio(thing.def);
                float weightedCount = fuelCount / fuelFactor; //figure the amount needed for this fuel type
                int usedAmount = Mathf.CeilToInt(Mathf.Min(weightedCount, thing.stackCount)); //figure what will be used from the stack, amount rounded up
                int satisfiedCount = Mathf.FloorToInt(usedAmount * fuelFactor); //figure how much regular fuel that corresponds to, amount rounded down 
                SiblingComp.Refuel(satisfiedCount); //refuels the corresponding amount
                thing.SplitOff(usedAmount).Destroy(DestroyMode.Vanish); //consumes the actual fuel used
                fuelCount -= satisfiedCount; //deducts the appropiate amount from the needed count. 
                Log.Message($"Refuel used {usedAmount} of {thing.def.defName} to generate {satisfiedCount} fuel units.");
            }
        }

        public void SetUpFuelFeatures()
        {
            if (SiblingComp == null) return;
            var props = SiblingComp.Props;
            var defaults = parent.def.GetCompProperties<CompProperties_Refuelable>();

            //Configurable Target Fuel Level
            if (settings.enableTargetFuelLevel)
            {
                props.targetFuelLevelConfigurable = true;
                if (props.initialConfigurableTargetFuelLevel == 0) props.initialConfigurableTargetFuelLevel = props.fuelCapacity;
            }
            else
            {
                props.targetFuelLevelConfigurable = defaults.targetFuelLevelConfigurable;
                props.initialConfigurableTargetFuelLevel = defaults.initialConfigurableTargetFuelLevel;
            }

            //Fuel Mixing
            if (!ClearedForFuelSelection)
            {
                props.atomicFueling = defaults.atomicFueling;
                StorageTabVisible = false;
                props.canEjectFuel = defaults.canEjectFuel;
                Log.Message($"[BurnItForFuel] {BaseFuelSettings.ToString()} is used by the {parent.Label}, but it isn't marked as fuel. Fuel tab disabled. Change the settings to prevent this.");
            }
            else if (SafeToMixFuels)
            {
                props.atomicFueling = true;
                StorageTabVisible = true;
                props.canEjectFuel = false; //It would take some sort of registering what kinds of fuel were loaded. 
            }
        }

        public void SetupFuelSettings()
        {
            FuelSettings = new StorageSettings(this);
            FuelSettings.filter.SetDisallowAll(UserFuelSettings.AllowedThingDefs);
            if (BaseFuelSettings != null) FuelSettings.filter.SetAllowAll(BaseFuelSettings);
        }

        public void ValidateFuelSettings()
        {
            fuelSettingsIncludeBaseFuel = null; //reset the cached value, so it can be recalculated
            if (Scribe.mode != LoadSaveMode.PostLoadInit) 
            {
                SetUpFuelFeatures();
            }
            //if (SafeToMixFuels)
            //{
                foreach (ThingDef def in FuelSettings.filter.AllowedThingDefs.Where(d => !GetParentStoreSettings().filter.Allows(d)))
                {
                    FuelSettings.filter.SetAllow(def, false);
                    Log.Warning($"[BurnItForFuel] {def.defName} is no longer fuel, so it was removed from the {parent} fuel settings.");
                }
            //}
        }

        //private bool IsVehicle()
        //{
        //    return nativeTargetFuelLevel.Contains(parent.def) && SiblingComp.Props.consumeFuelOnlyWhenUsed;
        //}
    }
}