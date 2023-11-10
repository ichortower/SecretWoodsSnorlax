using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using System.IO;
using System.Reflection;
using System.Threading;

namespace ichortower.SecretWoodsSnorlax
{
    internal class Events
    {
        public static string SnorlaxMailId = "SecretWoodsSnorlax_Moved";
        public static string SnorlaxFluteCueShort = "SecretWoodsSnorlax_fluteshort";
        public static string SnorlaxFluteCue = "SecretWoodsSnorlax_flutemelody";
        public static int msPerBeat = 432;
        public static string FluteName = "Strange Flute";
        public static int FluteId = -1;
        public static bool FluteHeardToday = false;
        public static JsonAssets.IApi JAApi = null;


        public static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            string path;
            JAApi = ModEntry.HELPER.ModRegistry.GetApi<JsonAssets.IApi>(
                    "spacechase0.JsonAssets");
            if (JAApi is null) {
                ModEntry.MONITOR.Log("CRITICAL: could not load the Json " +
                        "Assets API. This shouldn't be possible, so this mod " +
                        "install is probably broken.", LogLevel.Error);
                ModEntry.MONITOR.Log("Please try reinstalling, updating (if " +
                        "available), or complaining to ichortower about it.",
                        LogLevel.Error);
            }
            else {
                path = Path.Combine(ModEntry.HELPER.DirectoryPath,
                        "assets", "[JA] Embedded Pack");
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

            /* Set up event commands for the wizard event */
            var SCApi = ModEntry.HELPER.ModRegistry.GetApi<SpaceCore.IApi>(
                    "spacechase0.SpaceCore");
            if (SCApi is null) {
                ModEntry.MONITOR.Log("CRITICAL: could not load the SpaceCore " +
                        "API. This shouldn't be possible, so this mod " +
                        "install is probably broken.", LogLevel.Error);
                ModEntry.MONITOR.Log("Please try reinstalling, updating (if " +
                        "available), or complaining to ichortower about it.",
                        LogLevel.Error);
            }
            else {
                MethodInfo giveKeyMethod = typeof(Events).GetMethod(
                        "command_giveKey", BindingFlags.Static | BindingFlags.Public);
                MethodInfo holdKeyMethod = typeof(Events).GetMethod(
                        "command_holdKey", BindingFlags.Static | BindingFlags.Public);
                SCApi.AddEventCommand("SWS_giveKey", giveKeyMethod);
                SCApi.AddEventCommand("SWS_holdKey", holdKeyMethod);
            }
        }

        public static void command_giveKey(Event e, GameLocation location,
                GameTime time, string[] split)
        {
            if (FluteId == -1) {
                FluteId = JAApi.GetObjectId(FluteName);
            }
            Object flute = new Object(Vector2.Zero, FluteId, 1);
            e.farmer.addItemByMenuIfNecessary(flute);
            e.CurrentCommand++;
        }

        public static void command_holdKey(Event e, GameLocation location,
                GameTime time, string[] split)
        {
            if (FluteId == -1) {
                FluteId = JAApi.GetObjectId(FluteName);
            }
            e.farmer.holdUpItemThenMessage(new Object(Vector2.Zero, FluteId, 1));
            e.CurrentCommand++;
        }


        public static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            /*
             * Spawn the snorlax. If the forest log is already gone, put him
             * in the moved location and flag it. Otherwise, replace the log.
             */
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

            /* try to get the flute id (also occurs when needed) */
            if (FluteId == -1) {
                FluteId = JAApi.GetObjectId(FluteName);
            }

            /* clear the once-per-day flag for the longer play cutscenes */
            FluteHeardToday = false;
        }

        public static void OnSaving(object sender, SavingEventArgs e)
        {
            /*
             * Here, we revert the snorlax to how the log would be in vanilla:
             * if he's moved, delete the log, and if he hasn't, restore it.
             */
            Forest forest = (Forest)Game1.getLocationFromName("Forest");
            // check the mail id first; fall back to current location
            if (Game1.player.hasOrWillReceiveMail(SnorlaxMailId)) {
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
            if (FluteId == -1) {
                FluteId = JAApi.GetObjectId(FluteName);
            }
            if (Game1.player.ActiveObject is null || Game1.player.ActiveObject
                    .ParentSheetIndex != FluteId) {
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
                Game1.drawObjectDialogue(text.Replace("{{p}}", Game1.player.displayName));
                return;
            }
            if (Game1.player.currentLocation.Name.Equals("Forest") &&
                    Game1.player.getTileX() <= 6 &&
                    Game1.player.getTileY() <= 10) {
                // prevent inspecting snorlax while starting these cutscenes
                if (!Game1.player.mailReceived.Contains(SnorlaxMailId)) {
                    ModEntry.HELPER.Input.Suppress(button);
                    WakeUpCutscene();
                    return;
                }
                else if (!FluteHeardToday) {
                    ModEntry.HELPER.Input.Suppress(button);
                    RelistenCutscene();
                    return;
                }
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
            var startloc = new Vector2(2f, 7f);
            var endloc = new Vector2(4f, 5f);

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
                Game1.player.faceGeneralDirection(startloc * 64f, 0, false, true);
            }, tally);
            tally += afterSongPause;

            DelayedAction.functionAfterDelay(delegate {
                Game1.player.currentLocation.playSoundAt("croak", startloc);
                snorlax.parentSheetIndex.Value = 1;
                Game1.player.doEmote(16);
                Game1.player.setRunning(true);
                Game1.player.controller = new PathFindController(Game1.player,
                        Game1.player.currentLocation, new Point(5, 10), 0,
                        delegate {
                            Game1.player.faceGeneralDirection(
                                    startloc * 64f, 0, false, true);
                        });
            }, tally);
            tally += 2400;

            DelayedAction.functionAfterDelay(delegate {
                snorlax.JumpAside();
                Game1.player.currentLocation.playSoundAt("dwoop", startloc);
            }, tally);
            tally += 1500;

            DelayedAction.functionAfterDelay(delegate {
                Game1.player.currentLocation.playSoundAt("secret1", endloc);
                Game1.player.mailReceived.Add(SnorlaxMailId);
            }, tally);
            tally += 2000;

            DelayedAction.functionAfterDelay(delegate {
                Game1.freezeControls = false;
                Game1.player.CanMove = true;
                Game1.player.onBridge.Value = false;
            }, tally);
        }

        private static void RelistenCutscene()
        {
        }
    }
}
