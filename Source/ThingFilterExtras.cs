using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BurnItForFuel
{
    public static class ThingFilterExtras //publisher
    {
        public static float buttonHeight = 24f;
        public static event EventHandler<bool> FuelFilterOpen; //event
        private static BurnItForFuelSettings Settings => BurnItForFuelMod.settings;

        public static void NotifyFuelFilterOpen(object sender, bool open)  //Raises the event
        {
            OnFuelFilterOpen(sender, open); //No event data
        }

        public static void OnFuelFilterOpen(object sender, bool open) //This will call all the event handler methods registered with the ProcessCompleted event
        {
            FuelFilterOpen?.Invoke(sender, open);
        }

        public static void TTFilterExtraButtons(Rect rect, bool fuelTab = false)
        {
            float module = rect.width /= 3f;
            string resetButtonText = "ResetButton".Translate();
            Rect rect1 = new Rect(rect.x, rect.y, module, rect.height);
            if (Widgets.ButtonText(rect1, resetButtonText, true, true, true, null))
            {
                Console.WriteLine(resetButtonText);
                //
                //filter = defaultFilter
            }
            if (!fuelTab)
            {
                Settings.TTFilterCheckbox(new Rect(rect1.xMax, rect1.y, module * 2, rect1.height), buttonHeight);
                return;
            }
            string loadButtonText = "LoadGameButton".Translate();
            string saveButtonText = "SaveGameButton".Translate();
            Rect rect2 = new Rect(rect1);
            rect2.x = rect1.xMax;
            Rect rect3 = new Rect(rect2);
            rect3.x = rect2.xMax;
            if (Widgets.ButtonText(rect2, loadButtonText, true, true, true, null))
            {
                Console.WriteLine(loadButtonText);
            }
            if (Widgets.ButtonText(rect3, saveButtonText, true, true, true, null))
            {
                Console.WriteLine(saveButtonText);
            }
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
    }
}
