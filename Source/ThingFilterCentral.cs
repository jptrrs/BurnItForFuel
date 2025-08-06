using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BurnItForFuel
{
    public static class ThingFilterCentral //publisher
    {
        public static event EventHandler<bool> FuelFilterOpen; //event

        public static void NotifyFuelFilterOpen(object sender, bool open)  //Raises the event
        {
            OnFuelFilterOpen(sender, open); //No event data
        }

        public static void OnFuelFilterOpen(object sender, bool open) //This will call all the event handler methods registered with the ProcessCompleted event
        {
            FuelFilterOpen?.Invoke(sender, open);
        }
    }
}
