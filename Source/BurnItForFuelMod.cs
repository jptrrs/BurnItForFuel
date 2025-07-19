using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace BurnItForFuel
{
    public class BurnItForFuelMod : Mod
    {
        public static BurnItForFuelSettings settings;

        public static ThingFilter PossibleFuels
        {
            get
            {
                var filter = new ThingFilter();
                IEnumerable<ThingDef> fuels = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.IsWithinCategory(ThingCategoryDefOf.Root));
                foreach (ThingDef def in fuels)
                {
                    filter.SetAllow(def, true);
                }
                return filter;
            }
        }
 
        public BurnItForFuelMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<BurnItForFuelSettings>();
        }

        public override void DoSettingsWindowContents(Rect Rect)
        {
            settings.CustomDrawer_ThingFilter(Rect, ref settings.masterFuelSettings, PossibleFuels);
            //Listing_Standard listingStandard = new Listing_Standard();
            //listingStandard.Begin(Rect);
            //listingStandard.CheckboxLabeled("exampleBoolExplanation", ref settings.exampleBool, "exampleBoolToolTip");
            //listingStandard.Label("exampleFloatExplanation");
            //settings.exampleFloat = listingStandard.Slider(settings.exampleFloat, 100f, 300f);
            //listingStandard.End();
            base.DoSettingsWindowContents(Rect);
        }

        public override string SettingsCategory()
        {
            //return "BurnItForFuel".Translate();
            return "Burn It For Fuel";
        }
    }
}
