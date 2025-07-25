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
            settings.Draw(Rect);
            base.DoSettingsWindowContents(Rect);
        }

        public override string SettingsCategory()
        {
            //return "BurnItForFuel".Translate();
            return "Burn It For Fuel";
        }
    }
}
