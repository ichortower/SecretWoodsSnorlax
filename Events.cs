using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using System.IO;
using System.Threading;

namespace ichortower.SecretWoodsSnorlax
{
    internal class Events
    {
        public static string SnorlaxMailId = "SecretWoodsSnorlax_Moved";
        public static string SnorlaxFluteCueShort = "SecretWoodsSnorlax_fluteshort";
        public static string SnorlaxFluteCue = "SecretWoodsSnorlax_flutemelody";
        public static int msPerBeat = 432;
        public static int ForeignFluteId = -1;
        public static JsonAssets.IApi JAApi = null;


        public static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            string path;
            /* Load the embedded JA content pack */
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
                path = Path.Combine(ModEntry.HELPER.DirectoryPath,
                        "assets", "[JA] Foreign Flute");
                JAApi.LoadAssets(path);
            }

            /* Load the flute musics (sound effects).
             * They're short, but snarfing an ogg does take time */
            Thread t = new Thread((ThreadStart)delegate {
                Ogg.LoadSound(SnorlaxFluteCue, Path.Combine(
                        ModEntry.HELPER.DirectoryPath, "assets", "melody.ogg"));
                Ogg.LoadSound(SnorlaxFluteCueShort, Path.Combine(
                        ModEntry.HELPER.DirectoryPath, "assets", "melody_short.ogg"));
            });
            t.Start();
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
         * expected vanilla log status. DayStarted will put our friend back.
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
                    Events.PlayFlute(button);
                    break;
                }
            }
        }

        private static void PlayFlute(SButton button)
        {
            bool normalGameplay = !Game1.eventUp && !Game1.isFestival() &&
                    !Game1.fadeToBlack && !Game1.player.swimming.Value &&
                    !Game1.player.bathingClothes.Value &&
                    !Game1.player.onBridge.Value &&
                    Game1.player.CanMove && !Game1.freezeControls &&
                    Game1.player.freezePause <= 0;
            if (!normalGameplay) {
                return;
            }
            GameLocation loc = Game1.player.currentLocation;
            // can't play inside, unless it's your house
            if (!loc.IsOutdoors && !loc.Name.Equals("FarmHouse")) {
                string text = ModEntry.HELPER.Translation.Get("flute.dontPlayHere");
                Game1.showRedMessage(text.Replace("{{p}}", Game1.player.displayName));
                return;
            }
            if (Game1.player.currentLocation.Name.Equals("Forest") &&
                    Game1.player.getTileX() <= 6 &&
                    Game1.player.getTileY() <= 10 &&
                    !Game1.player.mailReceived.Contains(SnorlaxMailId)) {
                // prevent inspecting snorlax while starting the cutscene
                ModEntry.HELPER.Input.Suppress(button);
                WakeUpCutscene();
                return;
            }
            int nowFacing = Game1.player.FacingDirection;
            Game1.player.faceDirection(2);
            Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[6]{
                new FarmerSprite.AnimationFrame(98, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(99, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(100, 2*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(98, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(99, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(98, 2*msPerBeat, true, false),
            }, delegate {
                Game1.player.faceDirection(nowFacing);
            });
            loc.playSoundAt(SnorlaxFluteCueShort, Game1.player.getTileLocation());
            Game1.player.freezePause = 8*msPerBeat;
        }

        private static void WakeUpCutscene()
        {
            Game1.freezeControls = true;
            Game1.player.CanMove = false;
            Game1.player.faceDirection(2);
            Game1.player.onBridge.Value = true;
            int tally = 0;
            int beforeSongPause = 1500;
            int afterSongPause = 1200;

            var snorlax = (Game1.player.currentLocation as Forest).log as SnorlaxLog;
            var snorloc = new Vector2(2f, 7f);

            Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[14]{
                new FarmerSprite.AnimationFrame(16, 2*beforeSongPause/3, false, false),
                new FarmerSprite.AnimationFrame(98, 1*beforeSongPause/3, true, false),
                new FarmerSprite.AnimationFrame(98, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(99, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(100, 2*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(98, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(99, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(98, 3*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(100, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(98, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(99, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(98, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(99, 1*msPerBeat, true, false),
                new FarmerSprite.AnimationFrame(100, 4*msPerBeat, true, false),
            });
            DelayedAction.functionAfterDelay(delegate {
                Game1.player.currentLocation.playSoundAt(SnorlaxFluteCue,
                        Game1.player.getTileLocation());
            }, beforeSongPause);
            tally += beforeSongPause + 18*msPerBeat;

            DelayedAction.functionAfterDelay(delegate {
                Game1.player.faceGeneralDirection(snorloc * 64f, 0, false, true);
            }, tally);
            tally += afterSongPause;

            DelayedAction.functionAfterDelay(delegate {
                //Game1.player.currentLocation.playSoundPitched("bob", 100);
                        //new Vector2(2f, 7f));
                snorlax.parentSheetIndex.Value = 1;
                Game1.player.doEmote(16);
                Game1.player.setRunning(true);
                Game1.player.controller = new PathFindController(Game1.player,
                        Game1.player.currentLocation, new Point(5, 10), 0,
                        delegate {
                            Game1.player.faceGeneralDirection(
                                    snorloc * 64f, 0, false, true);
                        });
            }, tally);
            tally += 2400;

            DelayedAction.functionAfterDelay(delegate {
                snorlax.parentSheetIndex.Value = 2;
                snorlax.yJumpVelocity = 14f;
                Game1.player.currentLocation.playSoundAt("dwop", snorloc);
            }, tally);
            tally += 1000;

            DelayedAction.functionAfterDelay(delegate {
                Game1.freezeControls = false;
                Game1.player.CanMove = true;
                Game1.player.onBridge.Value = false;
            }, tally);
        }
    }
}
