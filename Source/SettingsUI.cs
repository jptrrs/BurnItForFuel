using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BurnItForFuel
{
    public class SettingsUI
    {
		public static bool CustomDrawer_ThingFilter(Rect rect, ref Vector2 scrollPosition, ref ThingFilter filter, ThingFilter parentfilter)
        {
			DoThingFilterConfigWindow(rect, ref scrollPosition, filter, parentfilter, 8);
			Rect labelRect = new Rect(rect);
			labelRect.width -= 20f;
			labelRect.position = new Vector2(labelRect.position.x - rect.width, labelRect.position.y);
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(labelRect, "FuelSettingsNote".Translate());
			return true;
        }

		public static void DoThingFilterConfigWindow(Rect rect, ref Vector2 scrollPosition, ThingFilter filter, ThingFilter parentFilter = null, int openMask = 1)
		{
			Widgets.DrawMenuSection(rect);
			Text.Font = GameFont.Tiny;
			float num = rect.width - 2f;
			Rect rect2 = new Rect(rect.x + 1f, rect.y + 1f, num / 2f, 24f);
			if (Widgets.ButtonText(rect2, "ClearAll".Translate(), true, false, true))
			{
				filter.SetDisallowAll(null, null);
				SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
			}
			Rect rect3 = new Rect(rect2.xMax + 1f, rect2.y, rect.xMax - 1f - (rect2.xMax + 1f), 24f);
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
			Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);
			Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
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
		}

		private static float viewHeight;

	}
}



