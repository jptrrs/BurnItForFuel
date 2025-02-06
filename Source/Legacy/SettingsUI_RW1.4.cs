using HarmonyLib;
using HugsLib.Settings;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
using static BurnItForFuel.ModBaseBurnItForFuel;

namespace BurnItForFuel
{
    //Changed in RW 1.5
    public class SettingsUI
    {
        private static MethodInfo DoCategoryChildrenInfo = AccessTools.Method(typeof(Listing_TreeThingFilter), nameof(Listing_TreeThingFilter.DoCategoryChildren));
        private static ThingFilterUI.UIState thingFilterState = new ThingFilterUI.UIState();

        public static bool CustomDrawer_ThingFilter(Rect rect, ref ThingFilter filter, ThingFilter parentfilter, ThingFilter defaultFilter, SettingHandle<FuelSettingsHandle> fuels)
        {
            DoThingFilterConfigWindow(rect, thingFilterState, ref filter, parentfilter, defaultFilter);
            Rect labelRect = new Rect(rect);
            labelRect.width -= 20f;
            labelRect.position = new Vector2(labelRect.position.x - rect.width, labelRect.position.y);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(labelRect, "FuelSettingsNote".Translate());
            fuels.HasUnsavedChanges = true;
            return true;
        }

        public static void DoThingFilterConfigWindow(Rect rect, ThingFilterUI.UIState state, ref ThingFilter filter, ThingFilter parentFilter = null, ThingFilter defaultFilter = null, int openMask = 1)
        {
            Widgets.DrawMenuSection(rect);
            Text.Font = GameFont.Tiny;
            float num = rect.width - 2f;
            Rect rect2 = new Rect(rect.x + 1f, rect.y + 1f, num / 2f, 24f);
            if (Widgets.ButtonText(rect2, "ClearAll".Translate(), true, true, true))
            {
                filter.SetDisallowAll(null, null);
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
            }
            if (Widgets.ButtonText(new Rect(rect2.xMax + 1f, rect2.y, rect.xMax - 1f - (rect2.xMax + 1f), 24f), "AllowAll".Translate(), true, true, true))
            {
                filter.SetAllowAll(parentFilter, false);
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
            }
            Text.Font = GameFont.Small;
            rect.yMin = rect2.yMax;
            int num2 = 1;
            Rect rect3 = new Rect(rect.x + 1f, rect.y + 1f + (float)num2, rect.width - 2f, buttonHeight);
            state.quickSearch.OnGUI(rect3, null);
            rect.yMin = rect3.yMax;
            rect.height -= buttonHeight + 1f;
            TreeNode_ThingCategory node = ThingCategoryNodeDatabase.RootNode;
            if (parentFilter != null)
            {
                node = parentFilter.DisplayRootCategory;
            }
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);
            Rect visibleRect = new Rect(0f, 0f, rect.width, rect.height);
            visibleRect.position += state.scrollPosition;
            Widgets.BeginScrollView(rect, ref state.scrollPosition, viewRect, true);
            float y = 2f;
            Rect rect4 = new Rect(0f, y, viewRect.width, 9999f);
            visibleRect.position -= rect4.position;
            Listing_TreeThingFilter listing_TreeThingFilter = new Listing_TreeThingFilter(filter, parentFilter, null, DefDatabase<SpecialThingFilterDef>.AllDefs, null, state.quickSearch.filter);
            listing_TreeThingFilter.Begin(rect4);
            try { listing_TreeThingFilter.ListCategoryChildren(node, openMask, new Map(), visibleRect); }
            finally { }
            listing_TreeThingFilter.End();
            state.quickSearch.noResultsMatched = (listing_TreeThingFilter.matchCount == 0);
            if (Event.current.type == EventType.Layout)
            {
                viewHeight = y + listing_TreeThingFilter.CurHeight + 90f;
            }
            Widgets.EndScrollView();
            Rect buttonRect = new Rect(rect.x + 1f, rect.yMax + 1f, num, buttonHeight);
            buttonRect.height = 24f;
            buttonRect.position = new Vector2(buttonRect.position.x, rect.yMax);
            bool clicked = Widgets.ButtonText(buttonRect, "Reset");
            if (clicked && defaultFilter != null)
            {
                filter = defaultFilter;
            }
        }

        private static float viewHeight;
        private const float buttonHeight = 24f;

    }
}