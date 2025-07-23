using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace BurnItForFuel
{
    public class BurnItForFuelSettings : ModSettings
    {
        public ThingFilter masterFuelSettings = new ThingFilter();
        public bool hideInvalids = true;
        public static float buttonHeight = 30f;
        private List<string> ExposedList = new List<string>();

        public ThingFilter PossibleFuels
        {
            get
            {
                var filter = new ThingFilter();
                IEnumerable<ThingDef> fuels = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.IsWithinCategory(ThingCategoryDefOf.Root) && (!hideInvalids || d.ValidateAsFuel()));
                foreach (ThingDef def in fuels)
                {
                    filter.SetAllow(def, true);
                }
                return filter;
            }
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving && masterFuelSettings != null)
            {
                ExposedList = masterFuelSettings.AllowedThingDefs.Select(x => x.defName).ToList();
                SettingsChanged();
            }
            Scribe_Collections.Look(ref ExposedList, "masterFuelSettings", LookMode.Value);
            base.ExposeData();
        }

        public bool DelayedLoading() //Apparently the DefDatabase wasn't ready before and we couldn't load ThingDefs.
        {
            if (ExposedList.Empty()) return false;
            foreach (var e in ExposedList)
            {
                var def = DefDatabase<ThingDef>.GetNamed(e);
                if (def != null && def.ValidateAsFuel())
                {
                    masterFuelSettings.SetAllow(def, true);
                }
            }
            ExposedList.Clear();
            return true;
        }

        public void SettingsChanged()
        {
            if (Current.ProgramState != ProgramState.Playing) return;
            foreach (Map map in Find.Maps)
            {
                List<Thing> affected = map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Refuelable));
                foreach (Thing t in affected)
                {
                    if (t is Building b) b.GetComp<CompSelectFuel>()?.ValidateFuelSettings();
                }
            }
        }

        public bool Draw(Rect rect)
        {
            //text
            string fuelsTitle = "Select Fuels:";
            string option1 = "Option 1";

            //layout
            Text.Anchor = TextAnchor.UpperLeft;
            Rect ColA = new Rect(rect);
            Rect ColB = new Rect(rect);
            var divider = rect.width / 1.618f;
            ColA.width = divider;
            ColB.width -= ColA.width;
            ColB.x += ColA.xMax;
            var padding = 10f;
            ColA.width -= padding;
            ColB.width -= padding;
            ColB.x += padding;

            Rect ColA_header = new Rect(ColA);
            ColA_header.height = Text.CalcHeight(fuelsTitle, ColA_header.width);
            Rect ColA_body = new Rect(ColA);
            ColA_body.y += ColA_header.height;
            ColA_body.height -= ColA_header.height;

            Rect ColB_header = new Rect(ColB);
            ColB_header.height = Text.CalcHeight(option1, ColB_header.width);
            Rect ColB_body = new Rect(ColB);
            ColB_body.y += ColB_header.height;
            ColB_body.height -= ColB_header.height;


            //Action
            float num = ColA_header.y;
            ThingFilterExtras.NotifyFuelFilterOpen(this);

            Widgets.Label(ColA_header, ref num, fuelsTitle, new TipSignal("fuelsToolTip"));
            ThingFilterUI.DoThingFilterConfigWindow(ColA_body, new ThingFilterUI.UIState(), masterFuelSettings, PossibleFuels, 1, null, DefDatabase<SpecialThingFilterDef>.AllDefs);
            Widgets.Label(ColB_header, option1);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(ColB_body);
            listingStandard.CheckboxLabeled(option1, ref hideInvalids, "exampleBoolToolTip");
            //listingStandard.Label("exampleFloatExplanation");
            //settings.exampleFloat = listingStandard.Slider(settings.exampleFloat, 100f, 300f);
            listingStandard.End();


            return true;
        }
    }
}