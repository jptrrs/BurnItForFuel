using Verse;
using RimWorld;
using System.Collections.Generic;

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

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            fuelSettings = new StorageSettings(this);
            if (parent.def.building.defaultStorageSettings != null)
            {
                fuelSettings.CopyFrom(parent.def.building.defaultStorageSettings);
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