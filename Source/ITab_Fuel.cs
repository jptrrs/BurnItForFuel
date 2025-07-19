using Verse;
using RimWorld;

namespace BurnItForFuel
{ 
    public class ITab_Fuel : ITab_Storage
    {
        public ITab_Fuel()
        {
            labelKey = "TabFuel";
        }

        public override IStoreSettingsParent SelStoreSettingsParent
        { 
            get
            {
                return SelFuelComp is IStoreSettingsParent ? SelFuelComp as IStoreSettingsParent : null;
            }
        }

        public virtual CompSelectFuel SelFuelComp
        {
            get
            {
                Thing thing = SelObject as Thing;
                return thing.TryGetComp<CompSelectFuel>();
            }
        }

        public override bool IsPrioritySettingVisible
        {
            get
            {
                return false;
            }
        }
    }
}
