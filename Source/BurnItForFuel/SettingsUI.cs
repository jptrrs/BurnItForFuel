using HugsLib.Settings;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BurnItForFuel;

public class SettingsUI
{
    private const float buttonHeight = 24f;

    private static readonly ThingFilterUI.UIState thingFilterState = new ThingFilterUI.UIState();

    private static float viewHeight;

    public static bool CustomDrawer_ThingFilter(Rect rect, ref ThingFilter filter, ThingFilter parentfilter,
        ThingFilter defaultFilter, SettingHandle<ModBaseBurnItForFuel.FuelSettingsHandle> fuels)
    {
        DoThingFilterConfigWindow(rect, thingFilterState, ref filter, parentfilter, defaultFilter);
        var labelRect = new Rect(rect);
        labelRect.width -= 20f;
        labelRect.position = new Vector2(labelRect.position.x - rect.width, labelRect.position.y);
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(labelRect, "FuelSettingsNote".Translate());
        fuels.HasUnsavedChanges = true;
        return true;
    }

    public static void DoThingFilterConfigWindow(Rect rect, ThingFilterUI.UIState state, ref ThingFilter filter,
        ThingFilter parentFilter = null, ThingFilter defaultFilter = null, int openMask = 1)
    {
        Widgets.DrawMenuSection(rect);
        Text.Font = GameFont.Tiny;
        var num = rect.width - 2f;
        var rect2 = new Rect(rect.x + 1f, rect.y + 1f, num / 2f, 24f);
        if (Widgets.ButtonText(rect2, "ClearAll".Translate()))
        {
            filter.SetDisallowAll();
            SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
        }

        if (Widgets.ButtonText(new Rect(rect2.xMax + 1f, rect2.y, rect.xMax - 1f - (rect2.xMax + 1f), 24f),
                "AllowAll".Translate()))
        {
            filter.SetAllowAll(parentFilter);
            SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
        }

        Text.Font = GameFont.Small;
        rect.yMin = rect2.yMax;
        var num2 = 1;
        var rect3 = new Rect(rect.x + 1f, rect.y + 1f + num2, rect.width - 2f, buttonHeight);
        state.quickSearch.OnGUI(rect3);
        rect.yMin = rect3.yMax;
        rect.height -= buttonHeight + 1f;
        var node = ThingCategoryNodeDatabase.RootNode;
        if (parentFilter != null)
        {
            node = parentFilter.DisplayRootCategory;
        }

        var viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);
        var visibleRect = new Rect(0f, 0f, rect.width, rect.height);
        visibleRect.position += state.scrollPosition;
        Widgets.BeginScrollView(rect, ref state.scrollPosition, viewRect);
        var y = 2f;
        var rect4 = new Rect(0f, y, viewRect.width, 9999f);
        visibleRect.position -= rect4.position;
        var listing_TreeThingFilter = new Listing_TreeThingFilter(filter, parentFilter, null,
            DefDatabase<SpecialThingFilterDef>.AllDefs, null, state.quickSearch.filter);
        listing_TreeThingFilter.Begin(rect4);
        listing_TreeThingFilter.ListCategoryChildren(node, openMask, null, visibleRect);
        listing_TreeThingFilter.End();
        state.quickSearch.noResultsMatched = listing_TreeThingFilter.matchCount == 0;
        if (Event.current.type == EventType.Layout)
        {
            viewHeight = y + listing_TreeThingFilter.CurHeight + 90f;
        }

        Widgets.EndScrollView();
        var buttonRect = new Rect(rect.x + 1f, rect.yMax + 1f, num, buttonHeight)
        {
            height = 24f
        };
        buttonRect.position = new Vector2(buttonRect.position.x, rect.yMax);
        var clicked = Widgets.ButtonText(buttonRect, "Reset");
        if (clicked && defaultFilter != null)
        {
            filter = defaultFilter;
        }
    }
}