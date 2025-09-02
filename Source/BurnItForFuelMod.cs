using RimWorld;
using UnityEngine;
using Verse;

namespace BurnItForFuel
{
    public class BurnItForFuelMod : Mod
    {
        public static BurnItForFuelSettings settings;

        public BurnItForFuelMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<BurnItForFuelSettings>();
        }

        public override void DoSettingsWindowContents(Rect Rect)
        {
            SettingsUI.DrawSettings(Rect);
            base.DoSettingsWindowContents(Rect);
        }

        public override string SettingsCategory()
        {
            return "Burn It For Fuel 2";
        }
    }
}
