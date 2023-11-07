using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using System.IO;

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

        public static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var jsapi = ModEntry.HELPER.ModRegistry.GetApi<JsonAssets.IApi>(
                    "spacechase0.JsonAssets");
            if (jsapi is null) {
                ModEntry.MONITOR.Log("Could not load the Json Assets API, so the " +
                        "flute object could not be registered. You will not be able " +
                        "to wake the Snorlax!", LogLevel.Error);
                ModEntry.MONITOR.Log("This shouldn't be possible, so this mod is " +
                        "probably broken. Please try updating it, or contacting " +
                        "ichortower to report a bug.", LogLevel.Error);
            }
            else {
                var path = Path.Combine(ModEntry.HELPER.DirectoryPath,
                        "assets", "[JA] Foreign Flute");
                jsapi.LoadAssets(path);
            }
        }
    }
}
