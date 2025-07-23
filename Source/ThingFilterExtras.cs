using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnItForFuel
{
    public static class ThingFilterExtras //publisher
    {
        public static event EventHandler FuelFilterOpen; //event

        public static void NotifyFuelFilterOpen(object sender)  //Raises the event
        {
            Console.WriteLine("Process Started!");
            // some code here..
            OnFuelFilterOpen(sender); //No event data
        }

        public static void OnFuelFilterOpen(object sender) //This will call all the event handler methods registered with the ProcessCompleted event
        {
            FuelFilterOpen?.Invoke(sender, EventArgs.Empty);
        }

    }
}
