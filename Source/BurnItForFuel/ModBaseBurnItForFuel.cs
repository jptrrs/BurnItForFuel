using System.Linq;
using HugsLib;
using HugsLib.Settings;
using RimWorld;
using Verse;

namespace BurnItForFuel;

public class ModBaseBurnItForFuel : ModBase
{
    public SettingHandle<FuelSettingsHandle> Fuels;

    private SettingHandle<bool> HasEverBeenSet;

    public ModBaseBurnItForFuel()
    {
        Settings.EntryName = "Burn It For Fuel";
    }

    public override string ModIdentifier => "JPT_BurnItForFuel";

    public static ThingFilter PossibleFuels
    {
        get
        {
            var filter = new ThingFilter();
            var fuels = DefDatabase<ThingDef>.AllDefsListForReading.Where(d =>
                d.IsWithinCategory(ThingCategoryDefOf.Root));
            foreach (var def in fuels)
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

    public override void DefsLoaded()
    {
        HasEverBeenSet = Settings.GetHandle<bool>("HasEverBeenSet", null, null);
        HasEverBeenSet.NeverVisible = true;
        Fuels = Settings.GetHandle<FuelSettingsHandle>("FuelSettings", "", null);
        if (Fuels.Value == null)
        {
            Fuels.Value = new FuelSettingsHandle();
        }

        if (Fuels.Value.masterFuelSettings.AllowedDefCount == 0 && !HasEverBeenSet)
        {
            Log.Message(
                $"[BurnItForFuel] Populating fuel settings for the first time. Default fuels are: {DefaultFuels.AllowedThingDefs.ToStringSafeEnumerable()}.");
            Fuels.Value.masterFuelSettings = DefaultFuels;
            HasEverBeenSet.Value = true;
        }

        Fuels.CustomDrawerHeight = 320f;
        Fuels.CustomDrawer = rect =>
            SettingsUI.CustomDrawer_ThingFilter(rect, ref Fuels.Value.masterFuelSettings, PossibleFuels, DefaultFuels,
                Fuels);
    }

    public override void SettingsChanged()
    {
        base.SettingsChanged();
        if (Current.ProgramState == ProgramState.Playing)
        {
            Find.Maps.ForEach(delegate(Map map)
            {
                var affected = map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Refuelable));
                foreach (var t in affected)
                {
                    if (t is not Building b)
                    {
                        continue;
                    }

                    var comp = b.GetComp<CompSelectFuel>();
                    comp?.ValidateFuelSettings();
                }
            });
        }
    }

    public class FuelSettingsHandle : SettingHandleConvertible
    {
        public ThingFilter masterFuelSettings = new ThingFilter();

        public override void FromString(string settingValue)
        {
            var defList = settingValue.Replace(", ", ",").Split(',').ToList()
                .ConvertAll(DefDatabase<ThingDef>.GetNamedSilentFail);
            foreach (var def in defList)
            {
                if (def != null)
                {
                    masterFuelSettings.SetAllow(def, true);
                }
            }
        }

        public override string ToString()
        {
            return masterFuelSettings.AllowedThingDefs.ToStringSafeEnumerable();
        }
    }
}