using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BurnItForFuel
{
    public class BurnItForFuelSettings : ModSettings
    {
        /// <summary>
        /// The three settings our mod has.
        /// </summary>
        //public float exampleFloat = 200f;
        //public List<Pawn> exampleListOfPawns = new List<Pawn>();

        public bool HasEverBeenSet;
        public ThingFilter masterFuelSettings;

        /// <summary>
        /// The part that writes our settings to file. Note that saving is by ref.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref HasEverBeenSet, "HasEverBeenSet");
            Scribe_Deep.Look(ref masterFuelSettings, "masterFuelSettings");
            //Scribe_Values.Look(ref exampleFloat, "exampleFloat", 200f);
            //Scribe_Collections.Look(ref exampleListOfPawns, "exampleListOfPawns", LookMode.Reference);
            base.ExposeData();
        }
    }
}
