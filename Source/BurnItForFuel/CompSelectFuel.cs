using System.Collections.Generic;
using System.Linq;
using HugsLib;
using RimWorld;
using Verse;
using static BurnItForFuel.ModBaseBurnItForFuel;

namespace BurnItForFuel;

[StaticConstructorOnStartup]
public class CompSelectFuel : ThingComp, IStoreSettingsParent
{
    public StorageSettings FuelSettings;

    public CompProperties_SelectFuel Props => (CompProperties_SelectFuel)props;

    public void Notify_SettingsChanged()
    {
    }

    public bool StorageTabVisible { get; set; }

    public StorageSettings GetParentStoreSettings()
    {
        var settings = new StorageSettings
        {
            filter = StorageTabVisible ? UserFuelSettings() : BaseFuelSettings(parent)
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
            SetupFuelSettings();
        }

        SetUpFuelMixing();
        //Log.Message("select fuel comp initialized by " + parent);
    }

    public void SetupFuelSettings()
    {
        FuelSettings = new StorageSettings(this);
        if (BaseFuelSettings(parent) == null)
        {
            return;
        }

        if ((!FuelSettingsIncludeBaseFuel() || IsVehicle()) &&
            !parent.def.GetCompProperties<CompProperties_Refuelable>().atomicFueling)
        {
            if (!FuelSettingsIncludeBaseFuel())
            {
                Log.Message(
                    $"[BurnItForFuel] {BaseFuelSettings(parent)} is used by the {parent.Label}, but it isn't marked as fuel. " +
                    $"Fuel tab disabled. Change the settings or add <atomicFueling>true</atomicFueling> to its CompProperties_Refuelable to prevent this.");
            }

            if (IsVehicle())
            {
                Log.Message(
                    $"[BurnItForFuel] {parent.LabelCap} looks like its a vehicle, so we're preventing fuel mixing to protect your engines. " +
                    $"Fuel tab disabled. Add <atomicFueling>true</atomicFueling> to its CompProperties_Refuelable to prevent this.");
            }

            FuelSettings.filter.SetAllowAll(BaseFuelSettings(parent));
        }
        else
        {
            foreach (var thingDef in UserFuelSettings().AllowedThingDefs)
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
            SetupFuelSettings();
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

    public void SetUpFuelMixing()
    {
        if (parent.TryGetComp<CompRefuelable>() != null && SafeToMixFuels())
        {
            parent.TryGetComp<CompRefuelable>().Props.atomicFueling = true;
            StorageTabVisible = true;
        }
        else
        {
            StorageTabVisible = false;
        }
    }

    public bool FuelSettingsIncludeBaseFuel() //e.g: Dubs Hygiene Burning Pit doesn't. 
    {
        if (BaseFuelSettings(parent) == null)
        {
            return false;
        }

        foreach (var thingDef in BaseFuelSettings(parent).AllowedThingDefs)
        {
            if (UserFuelSettings().Allows(thingDef))
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
            SetUpFuelMixing();
        }

        if (!SafeToMixFuels())
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

    private static ThingFilter BaseFuelSettings(ThingWithComps T)
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

    private static ThingFilter UserFuelSettings()
    {
        var pack = HugsLibController.SettingsManager.GetModSettings("JPT_BurnItForFuel");
        return pack.GetHandle<FuelSettingsHandle>("FuelSettings").Value.masterFuelSettings;
    }

    private bool IsVehicle()
    {
        var propertiesRefuelable = parent.TryGetComp<CompRefuelable>().Props;
        return propertiesRefuelable.targetFuelLevelConfigurable && propertiesRefuelable.consumeFuelOnlyWhenUsed;
    }

    private bool SafeToMixFuels()
    {
        return FuelSettingsIncludeBaseFuel() && parent.def.passability != Traversability.Impassable &&
               !parent.def.building.canPlaceOverWall && !IsVehicle();
    }
}