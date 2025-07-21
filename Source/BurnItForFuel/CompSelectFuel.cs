using System.Collections.Generic;
using System.Linq;
using HugsLib;
using RimWorld;
using Verse;

namespace BurnItForFuel;

[StaticConstructorOnStartup]
public class CompSelectFuel : ThingComp, IStoreSettingsParent
{
    public StorageSettings FuelSettings;

    public CompProperties_SelectFuel Props => (CompProperties_SelectFuel)props;

    public void Notify_SettingsChanged()
    {
    }

    public bool StorageTabVisible { get; private set; }

    public StorageSettings GetParentStoreSettings()
    {
        var settings = new StorageSettings
        {
            filter = StorageTabVisible ? userFuelSettings() : baseFuelSettings(parent)
        };

        return settings;
    }

    public StorageSettings GetStoreSettings()
    {
        return FuelSettings;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var g in StorageSettingsClipboard.CopyPasteGizmosFor(FuelSettings))
        {
            yield return g;
        }
    }

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            setupFuelSettings();
        }

        setUpFuelMixing();
        //Log.Message("select fuel comp initialized by " + parent);
    }

    private void setupFuelSettings()
    {
        FuelSettings = new StorageSettings(this);
        if (baseFuelSettings(parent) == null)
        {
            return;
        }

        if ((!fuelSettingsIncludeBaseFuel() || isVehicle()) &&
            !parent.def.GetCompProperties<CompProperties_Refuelable>().atomicFueling)
        {
            if (!fuelSettingsIncludeBaseFuel())
            {
                Log.Message(
                    $"[BurnItForFuel] {baseFuelSettings(parent)} is used by the {parent.Label}, but it isn't marked as fuel. " +
                    $"Fuel tab disabled. Change the settings or add <atomicFueling>true</atomicFueling> to its CompProperties_Refuelable to prevent this.");
            }

            if (isVehicle())
            {
                Log.Message(
                    $"[BurnItForFuel] {parent.LabelCap} looks like its a vehicle, so we're preventing fuel mixing to protect your engines. " +
                    $"Fuel tab disabled. Add <atomicFueling>true</atomicFueling> to its CompProperties_Refuelable to prevent this.");
            }

            FuelSettings.filter.SetAllowAll(baseFuelSettings(parent));
        }
        else
        {
            foreach (var thingDef in userFuelSettings().AllowedThingDefs)
            {
                FuelSettings.filter.SetAllow(thingDef, true);
            }
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref FuelSettings, "fuelSettings");
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }

        if (FuelSettings == null)
        {
            setupFuelSettings();
        }

        ValidateFuelSettings();
    }

    public void PurgeFuelSettings()
    {
        var excess = FuelSettings.filter.AllowedThingDefs.Except(GetParentStoreSettings().filter.AllowedThingDefs);
        if (!excess.Any())
        {
            return;
        }

        foreach (var t in excess.ToList())
        {
            FuelSettings.filter.SetAllow(t, false);
        }
    }

    private void setUpFuelMixing()
    {
        if (parent.TryGetComp<CompRefuelable>() != null && safeToMixFuels())
        {
            parent.TryGetComp<CompRefuelable>().Props.atomicFueling = true;
            StorageTabVisible = true;
        }
        else
        {
            StorageTabVisible = false;
        }
    }

    private bool fuelSettingsIncludeBaseFuel() //e.g: Dubs Hygiene Burning Pit doesn't. 
    {
        if (baseFuelSettings(parent) == null)
        {
            return false;
        }

        foreach (var thingDef in baseFuelSettings(parent).AllowedThingDefs)
        {
            if (userFuelSettings().Allows(thingDef))
            {
                return true;
            }
        }

        return false;
    }

    public void ValidateFuelSettings()
    {
        if (Scribe.mode == LoadSaveMode.Inactive)
        {
            setUpFuelMixing();
        }

        if (!safeToMixFuels())
        {
            return;
        }

        foreach (var def in (from d in FuelSettings.filter.AllowedThingDefs
                     where !GetParentStoreSettings().filter.Allows(d)
                     select d).ToList())
        {
            FuelSettings.filter.SetAllow(def, false);
            Log.Warning(
                $"[BurnItForFuel] {def.defName} is no longer fuel, so it was removed from the {parent} fuel settings.");
        }
    }

    private static ThingFilter baseFuelSettings(ThingWithComps T)
    {
        if (T.def.comps == null)
        {
            return null;
        }

        foreach (var compProperties in T.def.comps)
        {
            if (compProperties.compClass != typeof(CompRefuelable))
            {
                continue;
            }

            var comp = (CompProperties_Refuelable)compProperties;
            return comp.fuelFilter;
        }

        return null;
    }

    private static ThingFilter userFuelSettings()
    {
        var pack = HugsLibController.SettingsManager.GetModSettings("JPT_BurnItForFuel");
        return pack.GetHandle<ModBaseBurnItForFuel.FuelSettingsHandle>("FuelSettings").Value.masterFuelSettings;
    }

    private bool isVehicle()
    {
        var propertiesRefuelable = parent.TryGetComp<CompRefuelable>().Props;
        return propertiesRefuelable.targetFuelLevelConfigurable && propertiesRefuelable.consumeFuelOnlyWhenUsed;
    }

    private bool safeToMixFuels()
    {
        return fuelSettingsIncludeBaseFuel() && parent.def.passability != Traversability.Impassable &&
               !parent.def.building.canPlaceOverWall && !isVehicle();
    }
}