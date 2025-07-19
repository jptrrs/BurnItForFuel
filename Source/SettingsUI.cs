using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BurnItForFuel
{
    public class SettingsUI
    {
        private static MethodInfo DoCategoryChildrenInfo = AccessTools.Method(typeof(Listing_TreeThingFilter), nameof(Listing_TreeThingFilter.DoCategoryChildren));
        private static ThingFilterUI.UIState thingFilterState = new ThingFilterUI.UIState();

        public static bool CustomDrawer_ThingFilter(Rect rect, ref ThingFilter filter, ThingFilter parentfilter/*, ThingFilter defaultFilter, SettingHandle<FuelSettingsHandle> fuels*/)
        {
            bool test = filter != null;
            //Log.Message($"CustomDrawer_ThingFilter called with rect: {rect} and filter is {test}");
            if (!test) return false;
            Rect labelRect = new Rect(rect);
            labelRect.width -= 20f;
            labelRect.position = new Vector2(labelRect.position.x - rect.width, labelRect.position.y);
            Text.Anchor = TextAnchor.UpperLeft;
            //if (Current.ProgramState != ProgramState.Playing)
            //{
            //    DoThingFilterConfigWindow(rect, thingFilterState, ref filter, parentfilter);
            //    //Widgets.Label(labelRect, "Start or load a game.".Translate());
            //}
            //else
            //{
            //    ThingFilterUI.DoThingFilterConfigWindow(rect, thingFilterState, filter, parentfilter, 1, null, DefDatabase<SpecialThingFilterDef>.AllDefs);
            //    //Widgets.Label(labelRect, "FuelSettingsNote".Translate());
            //    //fuels.HasUnsavedChanges = true;
            //}

            //Testing using vanilla no matter the state
            ThingFilterUI.DoThingFilterConfigWindow(rect, thingFilterState, filter, parentfilter, 1, null, DefDatabase<SpecialThingFilterDef>.AllDefs);

            return true;
        }

        public static void DoThingFilterConfigWindow(Rect rect, ThingFilterUI.UIState state, ref ThingFilter filter, ThingFilter parentFilter = null, ThingFilter defaultFilter = null, int openMask = 1)
        {
            Widgets.DrawMenuSection(rect);
            Text.Font = GameFont.Tiny; // no font on original
            float num = rect.width - 2f;
            Rect rect2 = new Rect(rect.x + 1f, rect.y + 1f, num / 2f, 24f);
            if (Widgets.ButtonText(rect2, "ClearAll".Translate(), true, true, true))
            {
                filter.SetDisallowAll(null, null); //original takes forceHiddenDefs, forceHiddenFilters
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
            }
            if (Widgets.ButtonText(new Rect(rect2.xMax + 1f, rect2.y, rect.xMax - 1f - (rect2.xMax + 1f), 24f), "AllowAll".Translate(), true, true, true))
            {
                filter.SetAllowAll(parentFilter, false);
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
            }
            Text.Font = GameFont.Small; // no font on original
            rect.yMin = rect2.yMax;
            int num2 = 1;
            Rect rect3 = new Rect(rect.x + 1f, rect.y + 1f + (float)num2, rect.width - 2f, buttonHeight); // different numbers on original
            state.quickSearch.OnGUI(rect3, null);
            rect.yMin = rect3.yMax;
            rect.height -= buttonHeight + 1f; // no button height on original
            TreeNode_ThingCategory node = ThingCategoryNodeDatabase.RootNode;
            if (parentFilter != null)
            {
                node = parentFilter.DisplayRootCategory; //original also sets other "configurable" fields
            }

            // our interference
            node.catDef.ResolveReferences();

            if (node.catDef.SortedChildThingDefs.NullOrEmpty())
            {
                Log.Message($"{node.catDef.defName} has no children");
                return;
            }
            // end of our interference


            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);
            Rect visibleRect = new Rect(0f, 0f, rect.width, rect.height);
            visibleRect.position += state.scrollPosition;
            Widgets.BeginScrollView(rect, ref state.scrollPosition, viewRect, true); // comes a few lines later on original
            //on original, a couple of other conditinal configs here
            float y = 2f;
            Rect rect4 = new Rect(0f, y, viewRect.width, 9999f);
            visibleRect.position -= rect4.position;

            Listing_TreeThingFilter listing_TreeThingFilter = new Listing_TreeThingFilter(filter, parentFilter, null, DefDatabase<SpecialThingFilterDef>.AllDefs, null, state.quickSearch.filter);
            listing_TreeThingFilter.visibleRect = rect4;
            listing_TreeThingFilter.Begin(rect4);
            DoCategoryChildrenNoMap(listing_TreeThingFilter, node, 0, openMask, new Map(), false); // on original, "ListCategorChildren" here
            listing_TreeThingFilter.End();
            state.quickSearch.noResultsMatched = (listing_TreeThingFilter.matchCount == 0);
            if (Event.current.type == EventType.Layout)
            {
                viewHeight = y + listing_TreeThingFilter.CurHeight + 90f;
            }
            Widgets.EndScrollView();

            // Our Reset button
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

        public static void DoCategoryChildrenNoMap(Listing_TreeThingFilter tree, TreeNode_ThingCategory node, int indentLevel, int openMask, Map map, bool subtreeMatchedSearch)
        {
            int step = 0;
            List<SpecialThingFilterDef> childSpecialFilters = node.catDef.childSpecialFilters;
            for (int i = 0; i < childSpecialFilters.Count; i++)
            {
                if (tree.Visible(childSpecialFilters[i], node))
                {
                    tree.DoSpecialFilter(childSpecialFilters[i], indentLevel);
                }
            }
            step++;
            Log.Message($"STEP {step}"); //1

            foreach (TreeNode_ThingCategory childCategoryNode in node.ChildCategoryNodes)
            {
                if (tree.Visible(childCategoryNode) && !HideCategoryDueToSearch(childCategoryNode))
                {
                    DoCategoryChildrenNoMap(tree, childCategoryNode, indentLevel, openMask, map, subtreeMatchedSearch);
                }
            }
            step++;
            Log.Message($"STEP {step}: sortedChildThingDef? {!node.catDef.SortedChildThingDefs.NullOrEmpty()}, HiddenItemsManager? {Find.HiddenItemsManager != null}"); //2

            List<ThingDef> list = new List<ThingDef>();
            foreach (ThingDef sortedChildThingDef in node.catDef.SortedChildThingDefs)
            {
                Log.Message($"checking {sortedChildThingDef.defName} in {node.catDef.defName}.");
                //DoThingDefNoMap(tree, sortedChildThingDef, indentLevel);
                if (Find.HiddenItemsManager.Hidden(sortedChildThingDef))
                {
                    Log.Message("case A");
                    list.Add(sortedChildThingDef);
                }
                else if (tree.Visible(sortedChildThingDef) && !HideThingDueToSearch(sortedChildThingDef))
                {
                    Log.Message("case B");
                    tree.DoThingDef(sortedChildThingDef, indentLevel, map);
                    //DoThingDefNoMap(tree, sortedChildThingDef, indentLevel);
                }
            }
            step++;
            Log.Message($"STEP {step}");

            if (!tree.searchFilter.Active && list.Count > 0)
            {
                tree.DoUndiscoveredEntry(indentLevel, node.catDef.parent != ThingCategoryDefOf.Corpses, list);
            }
            step++;
            Log.Message($"STEP {step}");

            bool HideCategoryDueToSearch(TreeNode_ThingCategory subCat)
            {
                if (!tree.searchFilter.Active || subtreeMatchedSearch) return false;
                if (tree.CategoryMatches(subCat)) return false;
                if (tree.ThisOrDescendantsVisibleAndMatchesSearch(subCat)) return false;
                return true;
            }
            step++;
            Log.Message($"STEP {step}");

            bool HideThingDueToSearch(ThingDef tDef)
            {
                if (!tree.searchFilter.Active || subtreeMatchedSearch) return false;
                return !tree.searchFilter.Matches(tDef);
            }
            step++;
            Log.Message($"STEP {step}");
        }

        public static void DoThingDefNoMap(Listing_TreeThingFilter tree, ThingDef tDef, int nestLevel)
        {
            Color? color = null;
            if (tree.searchFilter.Matches(tDef))
            {
                tree.matchCount++;
            }
            else
            {
                color = new Color?(Listing_TreeThingFilter.NoMatchColor);
            }

            if (tDef.uiIcon != null && tDef.uiIcon != BaseContent.BadTex)
            {
                nestLevel++;
                Widgets.DefIcon(new Rect(tree.XAtIndentLevel(nestLevel) - 6f, tree.curY, 20f, 20f), tDef, null, 1f, null, drawPlaceholder: true, color);
            }

            if (tree.CurrentRowVisibleOnScreen())
            {
                bool num = (tree.suppressSmallVolumeTags == null || !tree.suppressSmallVolumeTags.Contains(tDef)) && tDef.IsStuff && tDef.smallVolume;
                string text = tDef.DescriptionDetailed;
                if (num)
                {
                    text += "\n\n" + "ThisIsSmallVolume".Translate(10.ToStringCached());
                }

                float num2 = -4f;
                if (num)
                {
                    Rect rect = new Rect(tree.LabelWidth - 19f, tree.curY, 19f, 20f);
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperRight;
                    GUI.color = Color.gray;
                    Widgets.Label(rect, "/" + 10.ToStringCached());
                    Text.Font = GameFont.Small;
                    GenUI.ResetLabelAlign();
                    GUI.color = Color.white;
                }

                //num2 -= 19f;
                //if (map != null)
                //{
                //    int count = map.resourceCounter.GetCount(tDef);
                //    if (count > 0)
                //    {
                //        string text2 = count.ToStringCached();
                //        Rect rect2 = new Rect(0f, tree.curY, tree.LabelWidth + num2, 40f);
                //        Text.Font = GameFont.Small;
                //        Text.Anchor = TextAnchor.UpperRight;
                //        GUI.color = new Color(0.5f, 0.5f, 0.1f);
                //        Widgets.Label(rect2, text2);
                //        num2 -= Text.CalcSize(text2).x;
                //        GenUI.ResetLabelAlign();
                //        Text.Font = GameFont.Small;
                //        GUI.color = Color.white;
                //    }
                //}

                tree.LabelLeft(tDef.LabelCap, text, nestLevel, num2, color);
                bool checkOn = tree.filter.Allows(tDef);
                bool flag = checkOn;
                Widgets.Checkbox(new Vector2(tree.LabelWidth, tree.curY), ref checkOn, tree.lineHeight, disabled: false, paintable: true);
                if (checkOn != flag)
                {
                    tree.filter.SetAllow(tDef, checkOn);
                }
            }
            tree.EndLine();
        }

    }
}