using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BurnItForFuel
{
    public class BurnItForFuelSettings : ModSettings
    {
        public bool
            useMass = true,
            useFlamm = true,
            enableTargetFuelLevel = true,
            enableWithNonFuel = false,
            showFuelPotential = false,
            showInvalids = false; //for dev mode, to see what fuels are available.

        public ThingFilter masterFuelSettings = new ThingFilter();
        public ThingDef standardFuel; //used for the potential fuel calculation in abstract terms.
        private List<string> ExposedList = new List<string>();

        public ThingFilter PossibleFuels
        {
            get
            {
                var filter = new ThingFilter();
                IEnumerable<ThingDef> fuels = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.IsWithinCategory(ThingCategoryDefOf.Root) && (showInvalids || d.ValidateAsFuel()));
                foreach (ThingDef def in fuels)
                {
                    filter.SetAllow(def, true);
                }
                filter.allowedHitPointsConfigurable = false;
                filter.allowedQualitiesConfigurable = false;
                return filter;
            }
        }

        public byte FuelPotentialValuesState => BoolArrayToByte(new bool[] { useMass, useFlamm });

        public byte BoolArrayToByte(bool[] boolArray) //nifty little thing!
        {
            byte result = 0;
            for (int i = 0; i < boolArray.Length; i++)
            {
                if (boolArray[i])
                {
                    result |= (byte)(1 << i);
                }
            }
            return result;
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving && masterFuelSettings != null)
            {
                ExposedList = masterFuelSettings.AllowedThingDefs.Select(x => x.defName).ToList();
                if (Current.ProgramState == ProgramState.Playing) RedistributeFuelSettings();
            }
            Scribe_Collections.Look(ref ExposedList, "masterFuelSettings", LookMode.Value);
            Scribe_Values.Look(ref useMass, "useMass", true);
            Scribe_Values.Look(ref useFlamm, "useFlamm", true);
            Scribe_Values.Look(ref enableTargetFuelLevel, "enableTargetFuelLevel", true);
            Scribe_Values.Look(ref enableWithNonFuel, "enableWithNonFuel", false);
            Scribe_Values.Look(ref showFuelPotential, "showFuelPotential", false);
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
            standardFuel = ThingDefOf.WoodLog;
            return true;
        }

        public void RedistributeFuelSettings()
        {
            foreach (Map map in Find.Maps)
            {
                List<Thing> affected = map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Refuelable));
                foreach (Thing t in affected)
                {
                    if (t is Building b) b.GetComp<CompSelectFuel>()?.ValidateFuelSettings();
                }
            }
        }

        public void Draw(Rect rect)
        {
            //text
            string fuels_label = "SelectFuels".Translate();
            string fuels_tt = "SelectFuelsToolTip".Translate();
            string calculation_label = "FuelPotentialCalculation".Translate();
            string useMass_label = "UseMassLabel".Translate();
            string useMass_tt = "UseMassToolTip".Translate();
            string useFlamm_label = "UseFlammLabel".Translate();
            string useFlammToolTip = "UseFlammToolTip".Translate();
            string zeroPotentialNotice = "* "+"ZeroPotentialNotice".Translate();
            string enableTargetFuelLevel_label = "EnableTargetFuelLevelLabel".Translate();
            string enableTargetFuelLevel_tt = "EnableTargetFuelLevelToolTip".Translate();
            string enableWithNonFuel_label = "EnableWithNonFuelLabel".Translate();
            string enableWithNonFuel_tt = "EnableWithNonFuelToolTip".Translate();

            //layout
            Text.Anchor = TextAnchor.UpperLeft;
            rect.height -= ThingFilterExtras.buttonHeight;
            Rect ColA = rect;
            Rect ColB = rect;
            var divider = rect.width / 1.618f;
            ColA.width = divider;
            ColB.width -= ColA.width;
            ColB.x += ColA.xMax;
            var padding = 10f;
            ColA.width -= padding;
            ColB.width -= padding;
            ColB.x += padding;

            Rect ColA_header = ColA;
            ColA_header.height = Text.LineHeightOf(Text.Font);
            float num = ColA_header.y;
            Rect ColA_body = ColA;
            ColA_body.y += ColA_header.height;
            ColA_body.height -= ColA_header.height;

            Rect ColB_header = ColB;
            ColB_header.height = Text.LineHeightOf(Text.Font);
            Rect ColB_body = ColB;
            ColB_body.y += ColB_header.height;
            ColB_body.height -= ColB_header.height;

            //action!
            Widgets.Label(ColA_header, ref num, fuels_label, new TipSignal(fuels_tt));
            ThingFilterUI.DoThingFilterConfigWindow(ColA_body, new ThingFilterUI.UIState(), masterFuelSettings, PossibleFuels, 1, null, DefDatabase<SpecialThingFilterDef>.AllDefs, true, true);
            Widgets.Label(ColB_header, calculation_label);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(ColB_body);
            float boxHeight = Text.LineHeightOf(Text.Font) * 2 + 2f;
            Listing_Standard subBox = listing.BeginSection(boxHeight);
            subBox.CheckboxLabeled(useMass_label, ref useMass, useMass_tt);
            subBox.CheckboxLabeled(useFlamm_label, ref useFlamm, useFlammToolTip);
            listing.EndSection(subBox);
            Text.Anchor = TextAnchor.UpperRight;
            listing.SubLabel(zeroPotentialNotice, 1f);
            Text.Anchor = TextAnchor.UpperLeft;
            listing.Gap();
            listing.CheckboxLabeled(enableTargetFuelLevel_label, ref enableTargetFuelLevel, enableTargetFuelLevel_tt);
            listing.CheckboxLabeled(enableWithNonFuel_label, ref enableWithNonFuel, enableWithNonFuel_tt);
            listing.End();
        }

        public void TTFilterCheckbox(Rect rect, float size)
        {
            //text
            string label = "ShowFuelPotential".Translate();
            string tooltip = "ShowFuelPotentialToolTip".Translate(standardFuel.label);

            //layout
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            float padding = 10f;
            float totalWidth = rect.width;
            size -= (size / 6);
            Rect rect2 = rect;
            rect2.width = Mathf.Min(totalWidth / 2, Text.CalcSize(label).x + size + padding * 2);
            rect2.x += totalWidth - rect2.width;
            Rect labelRect = rect2;
            labelRect.x += padding;
            labelRect.width -= padding;

            //action!
            Widgets.Label(labelRect, label);
            Widgets.ToggleInvisibleDraggable(rect2, ref showFuelPotential, true, false);
            Widgets.CheckboxDraw(rect2.x + rect2.width - size, rect.y + (rect.height - size) / 2f, showFuelPotential, false, size, null, null);
            if (Mouse.IsOver(rect2)) Widgets.DrawHighlight(rect2);
            TooltipHandler.TipRegion(rect2, tooltip);

            if (Prefs.DevMode)
            {
                string extraLabel = "DEV: Show invalid items";
                Rect rect3 = rect;
                rect3.width = Mathf.Min(totalWidth - rect2.width, Text.CalcSize(extraLabel).x + size + padding);
                rect3.x = rect2.x - rect3.width;
                Widgets.Label(rect3, extraLabel);
                Widgets.ToggleInvisibleDraggable(rect3, ref showInvalids, true, false);
                Widgets.CheckboxDraw(rect3.x + rect3.width - size, rect.y + (rect.height - size) / 2f, showInvalids, false, size, null, null);
                if (Mouse.IsOver(rect3)) Widgets.DrawHighlight(rect3);
            }

            Text.Anchor = anchor;
        }
    }
}
