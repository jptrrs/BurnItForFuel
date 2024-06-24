using RimWorld;
using Verse;

namespace BurnItForFuel;

public class ITab_Fuel : ITab_Storage
{
    public ITab_Fuel()
    {
        labelKey = "TabFuel";
    }

    protected override IStoreSettingsParent SelStoreSettingsParent
    {
        get
        {
            var thing = SelObject as Thing;
            var comp = thing.TryGetComp<CompSelectFuel>();
            return comp;
        }
    }

    protected override bool IsPrioritySettingVisible => false;
}