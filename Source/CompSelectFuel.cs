using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BurnItForFuel
{
    [StaticConstructorOnStartup]
    public class CompSelectFuel : ThingComp, IStoreSettingsParent
    {
        public CompProperties_SelectFuel Props
        {
            get
            {
                return (CompProperties_SelectFuel)props;
            }
        }

        public StorageSettings fuelSettings;

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

        public bool StorageSettingsIncludeBaseFuel() //e.g: Dubs Hygiene Burning Pit doesn't.
        {
            bool flag = false;
            foreach (ThingDef thingDef in BaseFuelSettings(parent).AllowedThingDefs)
            {
                if (parent.def.building.fixedStorageSettings.AllowedToAccept(thingDef))
                {
                    if (!flag) { flag = true; }
                }
            }
            return flag;
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            fuelSettings = new StorageSettings(this);
            if (BaseFuelSettings(parent) != null)
            {
                if ((!StorageSettingsIncludeBaseFuel() || IsVehicle()) && !parent.def.GetCompProperties<CompProperties_Refuelable>().atomicFueling)
                {
                    if (!StorageSettingsIncludeBaseFuel()) Log.Message("[BurnItForFuel] " + BaseFuelSettings(parent).ToString() + " was missing from the " + parent + " storage settings, so those were overriden. Add <atomicFueling>true</atomicFueling> to its CompProperties_Refuelable to prevent this.");
                    if (IsVehicle()) Log.Message("[BurnItForFuel] " + parent + " looks like its a vehicle, so we're preventing fuel mixing to protect your engines. Add <atomicFueling>true</atomicFueling> to its CompProperties_Refuelable to prevent this.");
                    GetParentStoreSettings().filter.SetAllowAll(BaseFuelSettings(parent));
                }
                foreach (ThingDef thingDef in BaseFuelSettings(parent).AllowedThingDefs)
                {
                    fuelSettings.filter.SetAllow(thingDef, true);
                }
            }
            if (SafeToMixFuels())
            {
                if (parent.TryGetComp<CompRefuelable>() != null)
                {
                    parent.TryGetComp<CompRefuelable>().Props.atomicFueling = true;
                }
            }
        }

        private bool MultipleFuelSet()
        {
            ICollection<ThingDef> filter = GetParentStoreSettings().filter.AllowedThingDefs as ICollection<ThingDef>;
            return filter.Count() > 1;
        }

        private bool SafeToMixFuels()
        {
            bool flag = false;
            if (MultipleFuelSet() && parent.def.passability != Traversability.Impassable && !parent.def.building.canPlaceOverWall && !IsVehicle()) flag = true;
            return flag;
        }

        private bool IsVehicle()
        {
            CompProperties_Refuelable props = parent.TryGetComp<CompRefuelable>().Props;
            return props.targetFuelLevelConfigurable && props.consumeFuelOnlyWhenUsed;
        }

        public bool StorageTabVisible
        {
            get
            {
                return MultipleFuelSet();
            }
        }

        public StorageSettings GetStoreSettings()
        {
            return fuelSettings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return parent.def.building.fixedStorageSettings;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look<StorageSettings>(ref fuelSettings, "fuelSettings");
            if (fuelSettings == null)
            {
                SetUpStorageSettings();
            }
        }

        public void SetUpStorageSettings()
        {
            if (GetParentStoreSettings() != null)
            {
                fuelSettings = new StorageSettings(this);
                fuelSettings.CopyFrom(GetParentStoreSettings());
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in StorageSettingsClipboard.CopyPasteGizmosFor(fuelSettings))
            {
                yield return g;
            }
            yield break;
        }

    }
}