using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;
using static System.Xml.Schema.FacetsChecker.FacetsCompiler;

namespace BurnItForFuel
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static bool TTFilterWindowFlag = false;
        private static float? TTFilterWindowBorder;
        private static ITab_Fuel FuelTab;
        private static readonly Type patchType = typeof(HarmonyPatches);
        private static ThingDef TTFilterLabelThingDef;
        private static bool TTFilterExtrasAllowed => TTFilterWindowFlag || TTFilterOnTab;
        private static bool TTFilterOnSettings => TTFilterWindowFlag && !TTFilterOnTab;
        private static bool TTFilterOnTab => FuelTab != null;
        private static BurnItForFuelSettings Settings => BurnItForFuelMod.settings;
        static HarmonyPatches()
        {
            //Harmony.DEBUG = true;
            var harmonyInstance = new Harmony("JPT_BurnItForFuel");

            //only for DEBUG purposes:
            //harmonyInstance.Patch(AccessTools.Method(typeof(RefuelWorkGiverUtility), "CanRefuel"),
            //    new HarmonyMethod(patchType, nameof(CanRefuel_Prefix)), new HarmonyMethod(patchType, nameof(CanRefuel_Postfix)), transpiler: null);

            //Our own way of looking for fuels and refueling.
            harmonyInstance.Patch(AccessTools.Method(typeof(RefuelWorkGiverUtility), "FindAllFuel"),
                new HarmonyMethod(patchType, nameof(FindAllFuel_Prefix))); //FindAllFuel is used for atomicFueling
            harmonyInstance.Patch(AccessTools.Method(typeof(RefuelWorkGiverUtility), "FindBestFuel"), 
                null, /*new HarmonyMethod(patchType, nameof(FindBestFuel_Postfix))*/null, new HarmonyMethod(patchType, nameof(FuelFilter_Transpiler))); //FindBestFuel is used for non-atomic fuelling.
            harmonyInstance.Patch(AccessTools.Method(typeof(CompRefuelable), nameof(CompRefuelable.Refuel), new Type[] { typeof(List<Thing>) }),
                new HarmonyMethod(patchType, nameof(Refuel_Prefix)));
            harmonyInstance.Patch(AccessTools.Method(typeof(CompRefuelable), nameof(CompRefuelable.GetFuelCountToFullyRefuel)),
                new HarmonyMethod(patchType, nameof(GetFuelCountToFullyRefuel_Prefix)));

            //Tweaks the refuel job to consider the weight of different fuels.
            harmonyInstance.Patch(AccessTools.Method(typeof(JobDriver_RefuelAtomic), nameof(JobDriver_RefuelAtomic.MakeNewToils)),
                null, new HarmonyMethod(patchType, nameof(MakeNewToils_Postfix)), null);

            //Teaching the tree filter thingy to behave when employed on the mod settings panel outside of a running game. 
            harmonyInstance.Patch(AccessTools.Method(typeof(Listing_TreeThingFilter), nameof(Listing_TreeThingFilter.DoCategoryChildren)),
                null, null, new HarmonyMethod(patchType, nameof(HiddenItemsManager_Transpiler)));
            harmonyInstance.Patch(AccessTools.Method(typeof(QuickSearchFilter), nameof(QuickSearchFilter.Matches), new Type[] {typeof(ThingDef)}),
                null, null, new HarmonyMethod(patchType, nameof(HiddenItemsManager_Transpiler)));

            //Inserting extra buttons and a fuel power indicator to the Thing Filter config window.
            harmonyInstance.Patch(AccessTools.Method(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow)),
                new HarmonyMethod(patchType, nameof(DoThingFilterConfigWindow_Prefix)), new HarmonyMethod(patchType, nameof(DoThingFilterConfigWindow_Postfix)), new HarmonyMethod(patchType, nameof(DoThingFilterConfigWindow_Transpiler)));
            harmonyInstance.Patch(AccessTools.Method(typeof(Listing_TreeThingFilter), nameof(Listing_TreeThingFilter.DoThingDef)),
                new HarmonyMethod(patchType, nameof(DoThingDef_Prefix)), new HarmonyMethod(patchType, nameof(DoThingDef_Postfix)), new HarmonyMethod(patchType, nameof(DoThingDef_Transpiler)));
            harmonyInstance.Patch(AccessTools.Method(typeof(Listing_Tree), nameof(Listing_Tree.LabelLeft)),
                new HarmonyMethod(patchType, nameof(LabelLeft_Prefix)));
            ThingFilterExtras.FuelFilterOpen += FuelFilterWindowOpened; // register with an event
        }

        //Event handler that reacts to the opening of a Fuel Filter window.
        public static void FuelFilterWindowOpened(object sender, bool open)
        {
            bool fromTab = false;
            if (sender is ITab_Fuel tab)
            {
                FuelTab = tab; //If necessary, will be cleared later on DoThingFilterConfigWindow_Prefix. This prevents an open tab from glitching if unfocused but still open. 
                fromTab = true;
            }
            else FuelTab = null;
            TTFilterWindowFlag = open;
            if (!fromTab && open && Current.ProgramState == ProgramState.Playing) //This frees us from having to draw different sets of extra buttons for different panels.
            {
                Find.WindowStack.WindowOfType<MainTabWindow_Inspect>()?.CloseOpenTab();
            }
        }

        //Modifies the expected fuel count to account for the the targeted fuel during a refuel job. Can't be a regular Prefix/Postfix because all toils are delegates.
        private static IEnumerable<Toil> MakeNewToils_Postfix(IEnumerable<Toil> toils)
        {
            foreach (var toil in toils)
            {
                if (toil.debugName == "StartCarryThing")
                {
                    yield return AdaptJobCount();
                    yield return toil;
                    yield return RectifyJobCount();
                }
                else                 
                {
                    yield return toil;
                }
            }
        }

        public static Toil AdaptJobCount()
        {
            Toil toil = ToilMaker.MakeToil("AdaptJobCount");
            toil.initAction = delegate ()
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing refuelable = curJob.GetTarget(TargetIndex.A).Thing;
                ThingDef fuel = curJob.GetTarget(TargetIndex.B).Thing.def;
                CompSelectFuel compSelectFuel = refuelable.TryGetComp<CompSelectFuel>();
                if (compSelectFuel == null)
                {
                    Log.Error($"[BurnItForFuel] AdaptJobCount: {refuelable.LabelCap} has no CompSelectFuel. Aborting.");
                    return;
                }
                int previousCount = curJob.count;
                curJob.count = Mathf.CeilToInt(curJob.count / compSelectFuel.EquivalentFuelRatio(fuel));
            };
            return toil;
        }

        public static Toil RectifyJobCount()
        {
            Toil toil = ToilMaker.MakeToil("RectifyJobCount");
            toil.initAction = delegate ()
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing refuelable = curJob.GetTarget(TargetIndex.A).Thing;
                CompSelectFuel compSelectFuel = refuelable.TryGetComp<CompSelectFuel>();
                if (compSelectFuel == null) return;
                int previousCount = curJob.count;
                curJob.count = Mathf.CeilToInt(curJob.count * compSelectFuel.EquivalentFuelRatio(compSelectFuel.lastEquivalentFuel));
                //Log.Message($"[BurnItForFuel] job.count successfully rectified back from {previousCount} to {curJob.count}");
            };
            return toil;
        }

        public static void DoThingDef_Prefix(ThingDef tDef)
        {
            if (TTFilterWindowFlag) TTFilterLabelThingDef = tDef;
        }

        public static void DoThingDef_Postfix(ThingDef tDef)
        {
            TTFilterLabelThingDef = null;
        }

        //Replaces Find.HiddenItemsManager.Hidden(ThingDef) with HiddenItemsManager_bypass(ThingDef) on the targeted methods.
        static IEnumerable<CodeInstruction> DoThingDef_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codeMatcher = new CodeMatcher(instructions);
            codeMatcher.MatchStartForward(new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Listing_Lines), "lineHeight"))).Advance(1)
                .SetInstruction(new CodeMatch(CodeMatch.Call(typeof(HarmonyPatches), nameof(DisableInvalidOnThingFilter))));
            return codeMatcher.Instructions();
        }

        public static bool DisableInvalidOnThingFilter()
        {
            if (TTFilterLabelThingDef == null) return false;
            return TTFilterLabelThingDef.UnitFuelValue() == 0f;
        }

        public static void LabelLeft_Prefix(Listing_TreeThingFilter __instance, ref float widthOffset)
        {
            if (TTFilterLabelThingDef == null || (!TTFilterOnTab && !Settings.showFuelPotential)) return;
            var ratio = FuelTab?.SelFuelComp?.EquivalentFuelRatio(TTFilterLabelThingDef) ?? TTFilterLabelThingDef.AbsoluteFuelRatio();
            if (ratio == 0f) return;
            widthOffset = ThingFilterExtras.InsertFuelPowerTag(__instance, widthOffset, ratio);
        }

        private static bool GetFuelCountToFullyRefuel_Prefix(CompRefuelable __instance, ref int __result)
        {
            CompSelectFuel compSelectFuel = __instance.parent.TryGetComp<CompSelectFuel>();
            if (compSelectFuel != null)
            {
                __result = compSelectFuel.GetFuelCountToFullyRefuel();
                return false;
            }
            return true;
        }

        private static bool Refuel_Prefix(CompRefuelable __instance, List<Thing> fuelThings)
        {
            CompSelectFuel compSelectFuel = __instance.parent.TryGetComp<CompSelectFuel>();
            if (compSelectFuel != null)
            {
                compSelectFuel.Refuel(fuelThings);
                return false;
            }
            return true;
        }

        //Replaces Find.HiddenItemsManager.Hidden(ThingDef) with HiddenItemsManager_bypass(ThingDef) on the targeted methods.
        static IEnumerable<CodeInstruction> HiddenItemsManager_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codeMatcher = new CodeMatcher(instructions);
            codeMatcher.MatchStartForward(new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(HiddenItemsManager), "Hidden")))
                .SetInstruction(new CodeMatch(CodeMatch.Call(typeof(HarmonyPatches), nameof(HiddenItemsManager_Bypass), new Type[] { typeof(ThingDef) })))
                .Advance(-2).RemoveInstruction();
            return codeMatcher.Instructions();
        }

        public static bool HiddenItemsManager_Bypass(ThingDef t)
        {
            return Current.ProgramState == ProgramState.Playing ? Find.HiddenItemsManager.Hidden(t) : false;
        }

        //Insterts a call to MakeRoomForFootButtons() before the Rect.yMax assignment in DoThingFilterConfigWindow.
        public static IEnumerable<CodeInstruction> DoThingFilterConfigWindow_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codeMatcher = new CodeMatcher(instructions);
            codeMatcher.End()
                .MatchStartBackwards(new CodeMatch(new CodeInstruction(OpCodes.Call, AccessTools.PropertySetter(typeof(Rect), nameof(Rect.yMax)))))
                .MatchStartBackwards(new CodeMatch(new CodeInstruction(OpCodes.Ldc_R4))).Advance(1)
                .Insert(new CodeMatch(CodeMatch.Call(typeof(HarmonyPatches), nameof(MakeRoomForFootbar), new Type[] {typeof(float)})));
            return codeMatcher.Instructions();
        }

        public static float MakeRoomForFootbar(float original)
        {   
            if (TTFilterWindowBorder == null) TTFilterWindowBorder = original;
            float result = original;
            if (TTFilterExtrasAllowed) result += ThingFilterExtras.buttonHeight;
            return result;
        }

        public static void DoThingFilterConfigWindow_Prefix(object __instance, Rect rect, byte __state)
        {
            if (TTFilterOnSettings) __state = Settings.FuelPotentialValuesState; //Records calculation settings state if we're on the main settings window.
            else if (TTFilterOnTab && !FuelTab.IsVisible && !TTFilterWindowFlag) FuelTab = null;
        }

        public static void DoThingFilterConfigWindow_Postfix(Rect rect, ref ThingFilter filter, byte __state)
        {
            if (!TTFilterExtrasAllowed) return; //Acts only if called for.
            float padding = TTFilterWindowBorder.value / 2;
            Rect footbar = new Rect(rect.x + padding, rect.yMax + padding, rect.width - TTFilterWindowBorder.value, ThingFilterExtras.buttonHeight);
            ThingFilterExtras.TTFilterExtraButtons(footbar, FuelTab?.SelFuelComp);
            if (TTFilterOnSettings && Settings.FuelPotentialValuesState != __state) SelectFuelHelper.ResetFuelValueCache(); //Calls for the fuel values updating if needed & we're on the main settings window.
        }

        //Debug only.
        public static void CanRefuel_Prefix(object __instance, Pawn pawn, Thing t, bool forced, ref bool __result)
        {
            Log.Message($"[BurnItForFuel] CanRefuel starting for? {__instance}.");
        }

        //Debug only.
        public static void CanRefuel_Postfix(object __instance, Pawn pawn, Thing t, bool forced, ref bool __result)
        {
            Log.Message($"[BurnItForFuel] CanRefuel? {__result.ToStringYesNo()}.");
        }

        //Debug only.
        public static void FindBestFuel_Postfix(Pawn pawn, Thing refuelable, ref Thing __result)
        {
            Log.Message($"[BurnItForFuel] FindBestFuel returns {__result.Label}. Atomic? {refuelable.TryGetComp<CompRefuelable>().Props.atomicFueling}");
        }

        public static bool FindAllFuel_Prefix(Pawn pawn, Thing refuelable, ref List<Thing> __result, MethodInfo __originalMethod)
        {
            if (refuelable.TryGetComp<CompSelectFuel>() == null)
            {
                Log.WarningOnce($"[BurnItForFuel] Failed when looking CompSelectFuel for {refuelable.LabelCap}. Proceeding with base fuel only.", 1);
                return true;
            }
            __result = FindAllFuel(pawn, refuelable);
            return false;
        }

        private static List<Thing> FindAllFuel(Pawn pawn, Thing refuelable) //returning null when basefuel not selected
        {
            var compSelectFuel = refuelable.TryGetComp<CompSelectFuel>();
            int fuelCountToFullyRefuel = compSelectFuel.GetFuelCountToFullyRefuel();
            List<Thing> result = FindEnoughReservableThings(pawn, refuelable.Position, new IntRange(fuelCountToFullyRefuel, fuelCountToFullyRefuel), compSelectFuel);
            return result;
        }

        //Ideia: Já que GetFuelCountToFullyRefuel retorna a quantidade de combustivel padrão, podemos fazer a equivalência com outros combustíveis no momento em que a busca é feita, quando o item potencial é averiguado. Aparentemente, essa é a função de FindEnoughReservableThings. ThingListProcessor (nested) acumula o stackcount de cada item e compara com a desiredQuantity, que por sua vez se refere ao combustível padrão.
        public static List<Thing> FindEnoughReservableThings(Pawn pawn, IntVec3 rootCell, IntRange desiredQuantity, CompSelectFuel compSelectFuel)
        {
            Region localRegion = rootCell.GetRegion(pawn.Map) ?? compSelectFuel.parent.InteractionCell.GetRegion(pawn.Map);
            if (localRegion == null)
            {
                Log.Error($"[BurnItForFuel] Null region when looking for reservable things while refuelling {compSelectFuel.parent.def}.");
            }
            TraverseParms traverseParams = TraverseParms.For(pawn);
            List<Thing> chosenThings = new List<Thing>();
            int accumulatedQuantity = 0;
            float accumulatedFraction = 0f;
            ThingListProcessor(rootCell.GetThingList(localRegion.Map), localRegion);
            if (accumulatedQuantity < desiredQuantity.max)
            {
                RegionTraverser.BreadthFirstTraverse(localRegion, EntryCondition, RegionProcessor, 99999);
            }
            if (accumulatedQuantity >= desiredQuantity.min)
            {
                return chosenThings;
            }
            return null;

            bool EntryCondition(Region from, Region r)
            {
                return r.Allows(traverseParams, isDestination: false);
            }

            bool RegionProcessor(Region r)
            {
                List<Thing> moreThings = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
                return ThingListProcessor(moreThings, r);
            }

            bool ThingListProcessor(List<Thing> things, Region region)
            {
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (Validator(thing) && !chosenThings.Contains(thing) && ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, region, PathEndMode.ClosestTouch, pawn))
                    {
                        chosenThings.Add(thing);
                        float found = thing.stackCount * compSelectFuel.EquivalentFuelRatio(thing.def);
                        int foundInt = Mathf.FloorToInt(found);
                        accumulatedQuantity += foundInt;
                        float fraction = found - foundInt;
                        accumulatedFraction += fraction;
                        if (accumulatedFraction >= 1f)
                        {
                            accumulatedQuantity++;
                            accumulatedFraction--;
                        }
                        if (accumulatedQuantity >= desiredQuantity.max) return true;
                    }
                }
                return false;
            }

            bool Validator(Thing x)
            {
                if (x.Fogged() || x.IsForbidden(pawn) || !pawn.CanReserve(x))
                {
                    return false;
                }
                if (!compSelectFuel.FuelSettings.filter.Allows(x))
                {
                    return false;
                }
                return true;
            }
        }

        //Replaces TryGetComp<CompRefuelable>().Props.fuelFilter with TryGetComp<CompSelectFuel>().FuelSettings.filter.
        static IEnumerable<CodeInstruction> FuelFilter_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codeMatcher = new CodeMatcher(instructions);

            codeMatcher.MatchStartForward(new CodeMatch(CodeMatch.LoadField(typeof(CompProperties_Refuelable), "fuelFilter")))
                .RemoveInstructionsInRange(codeMatcher.Pos - 2, codeMatcher.Pos).Advance(-2)
                .InsertAndAdvance(CodeMatch.Call(typeof(ThingCompUtility), "TryGetComp", new Type[] { typeof(Thing) }, new Type[] { typeof(CompSelectFuel) }))
                .InsertAndAdvance(CodeMatch.LoadField(typeof(CompSelectFuel), nameof(CompSelectFuel.FuelSettings)))
                .InsertAndAdvance(CodeMatch.LoadField(typeof(StorageSettings), nameof(StorageSettings.filter)));

            return codeMatcher.Instructions();
        }

    }
}