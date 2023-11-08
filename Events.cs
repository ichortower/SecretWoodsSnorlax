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
        public static int ForeignFluteId = -1;
        public static JsonAssets.IApi JAApi = null;

        public static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            JAApi = ModEntry.HELPER.ModRegistry.GetApi<JsonAssets.IApi>(
                    "spacechase0.JsonAssets");
            if (JAApi is null) {
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
                JAApi.LoadAssets(path);
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

            if (ForeignFluteId == -1) {
                ForeignFluteId = JAApi.GetObjectId("Foreign Flute");
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
                else { //if (forest.log.tile.Value.X == 3f)
                    forest.log = null;
                }
            }
        }

        /*
         * Feels a bit yucky listening to input events to play the flute.
         * Harmony probably closer to target, but I'm trying not to use it
         * in this mod.
         */
        public static void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (ForeignFluteId == -1) {
                ForeignFluteId = JAApi.GetObjectId("Foreign Flute");
            }
            if (Game1.player.ActiveObject is null || Game1.player.ActiveObject
                    .ParentSheetIndex != ForeignFluteId) {
                return;
            }
            foreach (var button in e.Pressed) {
                if (button.IsActionButton()) {
                    Events.PlayForeignFlute();
                    break;
                }
            }
        }

        private static void PlayForeignFlute()
        {
            bool normalGameplay = !Game1.eventUp && !Game1.isFestival() &&
                    !Game1.fadeToBlack && !Game1.player.swimming.Value &&
                    !Game1.player.bathingClothes.Value &&
                    !Game1.player.onBridge.Value &&
                    Game1.player.freezePause <= 0;
            if (!normalGameplay) {
                return;
            }
            if (Game1.player.currentLocation.Name.Equals("Forest") &&
                    Game1.player.getTileX() <= 6 &&
                    Game1.player.getTileY() <= 10) {
                ModEntry.MONITOR.Log("full play cutscene here", LogLevel.Info);
                return;
            }
            int nowFacing = Game1.player.FacingDirection;
            Game1.player.faceDirection(2);
            Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[3]{
                new FarmerSprite.AnimationFrame(98, 500, true, false),
                new FarmerSprite.AnimationFrame(99, 500, true, false),
                new FarmerSprite.AnimationFrame(100, 500, true, false),
            });
            Game1.soundBank.PlayCue("horse_flute");
            Game1.player.freezePause = 1500;
            DelayedAction.functionAfterDelay(delegate {
                    Game1.player.faceDirection(nowFacing);
            }, 1500);
        }
    }
}
