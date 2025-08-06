using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BurnItForFuel
{
    public static class SettingsUI
    {
        public static float buttonHeight = 24f;

        static BurnItForFuelSettings settings => BurnItForFuelMod.settings;

        public static void Draw(Rect rect)
        {
            //text
            string fuels_label = "SelectFuels".Translate();
            string fuels_tt = "SelectFuelsToolTip".Translate();
            string calculation_label = "FuelPotentialCalculation".Translate();
            string useMass_label = "UseMassLabel".Translate();
            string useMass_tt = "UseMassToolTip".Translate();
            string useFlamm_label = "UseFlammLabel".Translate();
            string useFlammToolTip = "UseFlammToolTip".Translate();
            string zeroPotentialNotice = "* " + "ZeroPotentialNotice".Translate();
            string enableTargetFuelLevel_label = "EnableTargetFuelLevelLabel".Translate();
            string enableTargetFuelLevel_tt = "EnableTargetFuelLevelToolTip".Translate();
            string enableWithNonFuel_label = "EnableWithNonFuelLabel".Translate();
            string enableWithNonFuel_tt = "EnableWithNonFuelToolTip".Translate();

            //layout
            Text.Anchor = TextAnchor.UpperLeft;
            rect.height -= buttonHeight;
            Rect colA = rect;
            Rect colB = rect;
            var divider = rect.width / 1.618f;
            colA.width = divider;
            colB.width -= colA.width;
            colB.x += colA.xMax;
            var padding = 10f;
            colA.width -= padding;
            colB.width -= padding;
            colB.x += padding;

            Rect colA_header = colA;
            colA_header.height = Text.LineHeightOf(Text.Font);
            float num = colA_header.y;
            Rect ColA_body = colA;
            ColA_body.y += colA_header.height;
            ColA_body.height -= colA_header.height;

            Rect colB_header = colB;
            colB_header.height = Text.LineHeightOf(Text.Font);
            Rect colB_body = colB;
            colB_body.y += colB_header.height;
            colB_body.height -= colB_header.height;

            //action!
            Widgets.Label(colA_header, ref num, fuels_label, new TipSignal(fuels_tt));
            ThingFilterUI.DoThingFilterConfigWindow(ColA_body, new ThingFilterUI.UIState(), settings.masterFuelSettings, settings.PossibleFuels, 1, null, DefDatabase<SpecialThingFilterDef>.AllDefs, true, true);
            Widgets.Label(colB_header, calculation_label);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(colB_body);
            float boxHeight = Text.LineHeightOf(Text.Font) * 2 + 2f;
            Listing_Standard subBox = listing.BeginSection(boxHeight);
            subBox.CheckboxLabeled(useMass_label, ref settings.useMass, useMass_tt);
            subBox.CheckboxLabeled(useFlamm_label, ref settings.useFlamm, useFlammToolTip);
            listing.EndSection(subBox);
            Text.Anchor = TextAnchor.UpperRight;
            listing.SubLabel(zeroPotentialNotice, 1);
            Text.Anchor = TextAnchor.UpperLeft;
            listing.Gap();
            listing.CheckboxLabeled(enableTargetFuelLevel_label, ref settings.enableTargetFuelLevel, enableTargetFuelLevel_tt);
            listing.CheckboxLabeled(enableWithNonFuel_label, ref settings.enableWithNonFuel, enableWithNonFuel_tt);
            listing.End();
        }

        public static float InsertFuelPowerTag(Listing_TreeThingFilter __instance, float widthOffset, float ratio)
        {
            string text = ratio.ToStringPercent();
            Rect rect = new Rect(0f, __instance.curY, __instance.LabelWidth + widthOffset, 40f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperRight;
            GUI.color = new Color(1f, 0.6f, 0.08f); //light orange
            Widgets.Label(rect, text);
            widthOffset -= Text.CalcSize(text).x;
            GenUI.ResetLabelAlign();
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            return widthOffset;
        }

        public static void TTFilterCheckbox(Rect rect, float size)
        {
            //text
            string label = "ShowFuelPotential".Translate();
            string tooltip = "ShowFuelPotentialToolTip".Translate(settings.standardFuel.LabelCap);

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
            Widgets.ToggleInvisibleDraggable(rect2, ref settings.showFuelPotential, true, false);
            Widgets.CheckboxDraw(rect2.x + rect2.width - size, rect.y + (rect.height - size) / 2f, settings.showFuelPotential, false, size, null, null);
            if (Mouse.IsOver(rect2)) Widgets.DrawHighlight(rect2);
            TooltipHandler.TipRegion(rect2, tooltip);

            if (Prefs.DevMode)
            {
                string extraLabel = "DEV: Show invalid items";
                Rect rect3 = rect;
                rect3.width = Mathf.Min(totalWidth - rect2.width, Text.CalcSize(extraLabel).x + size + padding);
                rect3.x = rect2.x - rect3.width;
                Widgets.Label(rect3, extraLabel);
                Widgets.ToggleInvisibleDraggable(rect3, ref settings.showInvalids, true, false);
                Widgets.CheckboxDraw(rect3.x + rect3.width - size, rect.y + (rect.height - size) / 2f, settings.showInvalids, false, size, null, null);
                if (Mouse.IsOver(rect3)) Widgets.DrawHighlight(rect3);
            }
            Text.Anchor = anchor;
        }

        public static void TTFilterExtraButtons(Rect rect, CompSelectFuel compFuel = null)
        {
            bool onTab = compFuel != null;
            float btnSpacing = 3f;
            float module = (rect.width + btnSpacing) / 3f;
            string resetButtonText = "ResetButton".Translate();
            Rect rect1 = new Rect(rect.x, rect.y, module - btnSpacing, rect.height);
            if (Widgets.ButtonText(rect1, resetButtonText, true, true, true, null))
            {
                if (onTab) compFuel.ResetFuelSettings();
                else settings.ResetFuelSettings();
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
            }
            if (!onTab)
            {
                TTFilterCheckbox(new Rect(rect1.xMax, rect1.y, module * 2, rect1.height), buttonHeight);
                return;
            }
            string loadButtonText = "LoadGameButton".Translate();
            string saveButtonText = "SaveGameButton".Translate();
            Rect rect2 = new Rect(rect1);
            rect2.x = rect1.xMax + btnSpacing;
            Rect rect3 = new Rect(rect2);
            rect3.x = rect2.xMax + btnSpacing;
            if (Widgets.ButtonText(rect2, loadButtonText, true, true, true, null))
            {
                compFuel.LoadCustomFuels();
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
            }
            if (Widgets.ButtonText(rect3, saveButtonText, true, true, true, null))
            {
                compFuel.SaveCustomFuels();
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
            }
        }
    }
}