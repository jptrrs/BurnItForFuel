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
                ThingFilterExtras.NotifyFuelFilterOpen(this, true);
                settingsWindowOpened = true;
            }
            settings.Draw(Rect);
            base.DoSettingsWindowContents(Rect);
        }

        public override string SettingsCategory()
        {
            return "Burn It For Fuel";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            settingsWindowOpened = false;
            ThingFilterExtras.NotifyFuelFilterOpen(this, false);
        }
    }
}
