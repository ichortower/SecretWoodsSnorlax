using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace ichortower.SecretWoodsSnorlax
{
    internal class Events
    {
        public static void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            Forest forest = (Forest)Game1.getLocationFromName("Forest");
            if (forest.log != null && !(forest.log is SnorlaxLog)) {
                forest.log = new SnorlaxLog();
            }
        }
    }
}
