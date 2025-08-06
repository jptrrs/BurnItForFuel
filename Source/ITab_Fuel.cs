using Verse;
using RimWorld;

namespace BurnItForFuel
{
    public class ITab_Fuel : ITab_Storage
    {
        object lastSelObject;

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

        public override void OnOpen()
        {
            lastSelObject = SelObject;
            ThingFilterCentral.NotifyFuelFilterOpen(this, true);
            base.OnOpen();
        }

        public override void ExtraOnGUI()
        {
            if (SelObject != lastSelObject)
            {
                lastSelObject = SelObject;
                ThingFilterCentral.NotifyFuelFilterOpen(this, true);
            }
            base.ExtraOnGUI();
        }

        public override void CloseTab()
        {
            base.CloseTab();
            lastSelObject = null;
            ThingFilterCentral.NotifyFuelFilterOpen(this, false);
        }

        public override void Notify_ClickOutsideWindow()
        {
            ThingFilterCentral.NotifyFuelFilterOpen(this, false);
            base.Notify_ClickOutsideWindow();
        }
    }
}
