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

        protected override IStoreSettingsParent SelStoreSettingsParent
        {
            get
            {
                Thing thing = base.SelObject as Thing;
                CompSelectFuel comp = thing.TryGetComp<CompSelectFuel>();
                if (comp as IStoreSettingsParent != null)
                {
                    return comp as IStoreSettingsParent;
                }
                return null;
            }
        }

        protected override bool IsPrioritySettingVisible
        {
            get
            {
                return false;
            }
        }

    }
}
