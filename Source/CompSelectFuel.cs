using System;
using System.Text;
using System.Collections.Generic;
using Verse;
using RimWorld;

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

        private bool StorageSettingsIncludeBaseFuel()
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
                if (StorageSettingsIncludeBaseFuel())
                {
                    foreach (ThingDef thingDef in BaseFuelSettings(parent).AllowedThingDefs)
                    {
                        fuelSettings.filter.SetAllow(thingDef, true);
                    }
                }
                else
                {
                    Log.Message("[BurnItForFuel] The storage settings defined for the " + parent.Label + " do not include the base fuel, which would prevent proper refuelling. Overriding.");
                    GetParentStoreSettings().filter.SetAllowAll(BaseFuelSettings(parent));
                    fuelSettings.filter.SetAllowAll(BaseFuelSettings(parent));
                }
            }
        }

        public bool StorageTabVisible
        {
            get
            {
                return true;
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