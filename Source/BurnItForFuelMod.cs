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
        /// <summary>
        /// A reference to our settings.
        /// </summary>
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

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public BurnItForFuelMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<BurnItForFuelSettings>();
        }

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="Rect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect Rect)
        {
            SettingsUI.CustomDrawer_ThingFilter(Rect, ref settings.masterFuelSettings, PossibleFuels);
            //Listing_Standard listingStandard = new Listing_Standard();
            //listingStandard.Begin(Rect);
            //listingStandard.CheckboxLabeled("exampleBoolExplanation", ref settings.exampleBool, "exampleBoolToolTip");
            //listingStandard.Label("exampleFloatExplanation");
            //settings.exampleFloat = listingStandard.Slider(settings.exampleFloat, 100f, 300f);
            //listingStandard.End();
            base.DoSettingsWindowContents(Rect);
        }

        /// <summary>
        /// Override SettingsCategory to show up in the list of settings.
        /// Using .Translate() is optional, but does allow for localisation.
        /// </summary>
        /// <returns>The (translated) mod name.</returns>
        public override string SettingsCategory()
        {
            //return "BurnItForFuel".Translate();
            return "Burn It For Fuel";
        }
    }
}
