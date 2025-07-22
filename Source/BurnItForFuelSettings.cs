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
        private List<string> ExposedList = new List<string>();

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

        public bool CustomDrawer_ThingFilter(Rect rect, ref ThingFilter filter, ThingFilter parentfilter)
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
            Widgets.Label(ColA_header, fuelsTitle);
            if (filter != null) ThingFilterUI.DoThingFilterConfigWindow(ColA_body, new ThingFilterUI.UIState(), filter, parentfilter, 1, null, DefDatabase<SpecialThingFilterDef>.AllDefs);
            Widgets.Label(ColB_header, option1);

            return true;
        }
    }
}