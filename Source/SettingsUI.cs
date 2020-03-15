using HugsLib.Settings;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static BurnItForFuel.ModBaseBurnItForFuel;

namespace BurnItForFuel
{
    public class SettingsUI
    {
		public static bool CustomDrawer_ThingFilter(Rect rect, ref Vector2 scrollPosition, ref ThingFilter filter, ThingFilter parentfilter, ThingFilter defaultFilter, SettingHandle<FuelSettingsHandle> fuels)
        {
			DoThingFilterConfigWindow(rect, ref scrollPosition, ref filter, parentfilter, defaultFilter);
			Rect labelRect = new Rect(rect);
			labelRect.width -= 20f;
			labelRect.position = new Vector2(labelRect.position.x - rect.width, labelRect.position.y);
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(labelRect, "FuelSettingsNote".Translate());
			fuels.HasUnsavedChanges = true; 
			return true;
        }

		public static void DoThingFilterConfigWindow(Rect rect, ref Vector2 scrollPosition, ref ThingFilter filter, ThingFilter parentFilter = null, ThingFilter defaultFilter= null, int openMask = 1)
		{
			Widgets.DrawMenuSection(rect);
			Text.Font = GameFont.Tiny;
			float num = rect.width - 2f;
			Rect rect2 = new Rect(rect.x + 1f, rect.y + 1f, num / 2f, buttonHeight);
			if (Widgets.ButtonText(rect2, "ClearAll".Translate(), true, false, true))
			{
				filter.SetDisallowAll(null, null);
				SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
			}
			Rect rect3 = new Rect(rect2.xMax + 1f, rect2.y, rect.xMax - 1f - (rect2.xMax + 1f), buttonHeight);
			if (Widgets.ButtonText(rect3, "AllowAll".Translate(), true, false, true))
			{
				filter.SetAllowAll(parentFilter);
				SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
			}
			Text.Font = GameFont.Small;
			rect.yMin = rect2.yMax;
			TreeNode_ThingCategory node = ThingCategoryNodeDatabase.RootNode;
			if (parentFilter != null)
			{
				node = parentFilter.DisplayRootCategory;
			}
			Rect viewRect = new Rect(0f, 0f, rect.width - 17f, viewHeight);
			Rect scrollrect = new Rect(rect.x, rect.y, rect.width -1f, rect.height - buttonHeight -1f);
			Widgets.BeginScrollView(scrollrect, ref scrollPosition, viewRect, true);
			float num2 = 2f;
			float num3 = num2;
			Rect rect4 = new Rect(0f, num2, viewRect.width, 9999f);
			Listing_TreeThingFilter listing_TreeThingFilter = new Listing_TreeThingFilter(filter, parentFilter, null, null, null);
			listing_TreeThingFilter.Begin(rect4);
			listing_TreeThingFilter.DoCategoryChildren(node, 0, openMask, null, true);
			listing_TreeThingFilter.End();
			if (Event.current.type == EventType.Layout)
			{
				viewHeight = num3 + listing_TreeThingFilter.CurHeight + 90f;
			}
			Widgets.EndScrollView();
			Rect buttonRect = new Rect(rect.x + 1f, rect.y +1f, num, buttonHeight);
			buttonRect.height = 24f;
			buttonRect.position = new Vector2(buttonRect.position.x, rect.yMax - buttonHeight - 1f);
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



