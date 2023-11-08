using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using System.IO;

namespace ichortower.SecretWoodsSnorlax
{
    internal class Events
    {
        public static string SnorlaxMailId = "SecretWoodsSnorlax_Moved";

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

        /*
         * Spawn the snorlax. If the forest log is already gone, this will skip
         * the event & flute and just put him in the moved location.
         * Otherwise he takes over the normal log position.
         */
        public static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            Forest forest = (Forest)Game1.getLocationFromName("Forest");
            if (Game1.player.mailReceived.Contains(SnorlaxMailId)) {
                forest.log = new SnorlaxLog(3f, 4f);
                return;
            }
            if (forest.log is null) {
                forest.log = new SnorlaxLog(3f, 4f);
                Game1.player.mailReceived.Add(SnorlaxMailId);
            }
            else {
                forest.log = new SnorlaxLog(1f, 6f);
            }
        }

        /*
         * We don't want to deal with the serializer here: just restore the
         * expected vanilla log status. SaveLoaded will put our friend back.
         * (this should make the mod safer to uninstall)
         */
        public static void OnSaving(object sender, SavingEventArgs e)
        {
            Forest forest = (Forest)Game1.getLocationFromName("Forest");
            // check the mail id first; fall back to current friend location
            if (Game1.player.hasOrWillReceiveMail("SecretWoodsSnorlax_Moved")) {
                forest.log = null;
                return;
            }
            if (forest.log != null && forest.log is SnorlaxLog) {
                if (forest.log.tile.Value.X == 1f) {
                    forest.log = new ResourceClump(602, 2, 2, new Vector2(1f, 6f));
                }
                else { //if (forest.log.tile.Value.X == 3f) {
                    forest.log = null;
                }
            }
        }
    }
}
