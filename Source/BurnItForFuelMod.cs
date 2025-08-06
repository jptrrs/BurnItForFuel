using RimWorld;
using UnityEngine;
using Verse;

namespace BurnItForFuel
{
    public class BurnItForFuelMod : Mod
    {
        public static BurnItForFuelSettings settings;
        private bool settingsWindowOpened = false;

        public BurnItForFuelMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<BurnItForFuelSettings>();
        }

        public override void DoSettingsWindowContents(Rect Rect)
        {
            if (!settingsWindowOpened)
            {
                ThingFilterCentral.NotifyFuelFilterOpen(this, true);
                settingsWindowOpened = true;
            }
            SettingsUI.DrawSettings(Rect);
            base.DoSettingsWindowContents(Rect);
        }

        public override string SettingsCategory()
        {
            return "Burn It For Fuel 2";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            settingsWindowOpened = false;
            ThingFilterCentral.NotifyFuelFilterOpen(this, false);
        }
    }
}
