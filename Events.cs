using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
using StardewValley.GameData.Objects;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace ichortower.SecretWoodsSnorlax
{
    internal class Events
    {
        public static bool FluteHeardToday = false;

        public static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            /* move the secondary location if Lunna is installed */
            bool haveLunna = ModEntry.HELPER.ModRegistry.IsLoaded("Rafseazz.LunnaCP");
            if (haveLunna) {
                Constants.vec_MovedPosition = new Vector2(7f, 7f);
            }

            Event.RegisterCustomCommand("SWS_giveKey", giveKeyMethod);
        }

        public static void giveKeyMethod(Event @evt, string[] args,
                EventContext context)
        {
            Item i = ItemRegistry.Create(Constants.id_Flute, 1, 0);
            if (i != null) {
                i.specialItem = true;
                @evt.farmer.addItemByMenuIfNecessary(i);
                @evt.farmer.holdUpItemThenMessage(i);
            }
            @evt.CurrentCommand++;
        }

        public static ResourceClump getBlockingLog(GameLocation loc)
        {
            foreach (ResourceClump clump in loc.resourceClumps) {
                if (clump.Tile == Constants.vec_BlockingPosition &&
                        clump.parentSheetIndex.Value == 602) {
                    return clump;
                }
            }
            return null;
        }

        public static SnorlaxLog getBigBoi(GameLocation loc)
        {
            foreach (ResourceClump clump in loc.resourceClumps) {
                if (clump is SnorlaxLog) {
                    return (SnorlaxLog)clump;
                }
            }
            return null;
        }


        /*
         * On a new day, spawn the snorlax and clear the flag for having
         * heard the flute today.
         * Spawn location depends on the existence of the vanilla log.
         */
        public static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            GameLocation forest = Game1.getLocationFromName("Forest");
            ResourceClump log = getBlockingLog(forest);
            SnorlaxLog boi = null;
            if (log is null) {
                Game1.player.mailReceived.Add(Constants.mail_SnorlaxMoved);
                boi = new SnorlaxLog(Constants.vec_MovedPosition);
            }
            else {
                forest.resourceClumps.Remove(log);
                boi = new SnorlaxLog(Constants.vec_BlockingPosition);
            }
            forest.resourceClumps.Add(boi);
            FluteHeardToday = false;

            /* convert the old event id to the new one.
             * if that's needed, also give a flute, since the old JA one will
             * become an error item */
            if (Game1.player.eventsSeen.Remove(Constants.id_EventOld)) {
                Game1.player.eventsSeen.Add(Constants.id_Event);
                Item i = ItemRegistry.Create(Constants.id_Flute, 1, 0);
                if (i != null) {
                    i.specialItem = true;
                    Game1.player.addItemByMenuIfNecessary(i);
                }
            }
        }

        /*
         * When saving at end of day, revert the resourceClumps to how they
         * would be in vanilla: if snorlax has moved, no blocking log, else
         * put it back.
         */
        public static void OnSaving(object sender, SavingEventArgs e)
        {
            GameLocation forest = Game1.getLocationFromName("Forest");
            SnorlaxLog boi = getBigBoi(forest);
            /* shouldn't happen with this mod installed */
            if (boi is null) {
                return;
            }
            if (!boi.HasMoved()) {
                forest.resourceClumps.Add(new ResourceClump(602, 2, 2,
                        Constants.vec_BlockingPosition));
            }
            forest.resourceClumps.Remove(boi);
        }

        /*
         * Feels a bit yucky listening to input events to play the flute.
         * Harmony probably closer to target, but I'm trying not to use it
         * in this mod.
         */
        public static void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            Object obj = Game1.player.ActiveObject;
            if (obj != null && obj.ItemId.Equals(Constants.id_Flute)) {
                foreach (var button in e.Pressed) {
                    if (button.IsActionButton()) {
                        Events.PlayFlute(button);
                        break;
                    }
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
                    Game1.player.Tile.X <= 6 &&
                    Game1.player.Tile.Y <= 10) {
                // suppressing input prevents inspecting snorlax while
                // starting these cutscenes
                var snorlax = getBigBoi(Game1.player.currentLocation);
                if (snorlax != null && !snorlax.HasMoved()) {
                    ModEntry.HELPER.Input.Suppress(button);
                    WakeUpCutscene();
                    return;
                }
                else if (snorlax != null && !FluteHeardToday) {
                    ModEntry.HELPER.Input.Suppress(button);
                    RelistenCutscene();
                    return;
                }
            }
            int nowFacing = Game1.player.FacingDirection;
            int msPerBeat = Constants.msPerBeat;
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
            loc.playSound(Constants.id_FluteCueShort, Game1.player.Tile);
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
            int msPerBeat = Constants.msPerBeat;

            var snorlax = getBigBoi(Game1.player.currentLocation);
            var startloc = new Vector2(Constants.vec_BlockingPosition.X+1f,
                    Constants.vec_BlockingPosition.Y+1f);
            var endloc = new Vector2(Constants.vec_MovedPosition.X+1f,
                    Constants.vec_MovedPosition.Y+1f);

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
                Game1.player.currentLocation.playSound(Constants.id_FluteCue,
                        Game1.player.Tile);
            }, beforeSongPause);
            tally += beforeSongPause + 18*msPerBeat;

            DelayedAction.functionAfterDelay(delegate {
                Game1.player.faceGeneralDirection(startloc * 64f, 0, false, true);
            }, tally);
            tally += afterSongPause;

            DelayedAction.functionAfterDelay(delegate {
                Game1.player.currentLocation.playSound("sandyStep", startloc);
                Game1.player.currentLocation.playSound("croak", startloc);
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
                Game1.player.currentLocation.playSound("dwoop", startloc);
            }, tally);
            tally += 1500;

            DelayedAction.functionAfterDelay(delegate {
                Game1.player.currentLocation.playSound("secret1", endloc);
                Game1.player.mailReceived.Add(Constants.mail_SnorlaxMoved);
                FluteHeardToday = true;
            }, tally);
            tally += 2000;

            DelayedAction.functionAfterDelay(delegate {
                Game1.freezeControls = false;
                Game1.player.CanMove = true;
                Game1.player.onBridge.Value = false;
            }, tally);
        }

        private static void heartSprite(Vector2 tileP)
        {
            var heart = new TemporaryAnimatedSprite("LooseSprites\\Cursors",
                    new Microsoft.Xna.Framework.Rectangle(211, 428, 7, 6),
                    2000f, 1, 0,
                    tileP * 64f + new Vector2(48f, -32f),
                    false, false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f){
                        motion = new Vector2(0f, -0.5f),
                        alphaFade = 0.01f
                    };
            Game1.player.currentLocation.TemporarySprites.Add(heart);
            Game1.player.currentLocation.playSound("dwop", tileP);
        }

        // bunch of copy-paste from WakeUpCutscene.
        // less work than adding logic to the other one.
        private static void RelistenCutscene()
        {
            Game1.freezeControls = true;
            Game1.player.CanMove = false;
            Game1.player.faceDirection(2);
            Game1.player.onBridge.Value = true;
            int tally = 0;
            int beforeSongPause = 1500;
            int afterSongPause = 1200;
            int msPerBeat = Constants.msPerBeat;

            var snorlax = getBigBoi(Game1.player.currentLocation);
            var soundloc = new Vector2(Constants.vec_MovedPosition.X+1f,
                    Constants.vec_MovedPosition.Y+1f);

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
                Game1.player.currentLocation.playSound(Constants.id_FluteCue,
                        Game1.player.Tile);
            }, beforeSongPause);

            DelayedAction.functionAfterDelay(delegate {
                snorlax.parentSheetIndex.Value = 1;
            }, beforeSongPause + 1000);
            DelayedAction.functionAfterDelay(delegate {
                heartSprite(soundloc);
            }, beforeSongPause + 2000);
            tally += beforeSongPause + 18*msPerBeat;

            DelayedAction.functionAfterDelay(delegate {
                Game1.player.faceGeneralDirection(soundloc * 64f, 0, false, true);
            }, tally);
            tally += afterSongPause;

            DelayedAction.functionAfterDelay(delegate {
                snorlax.JumpInPlace();
                Game1.player.currentLocation.playSound("dwoop", soundloc);
            }, tally);
            tally += 1000;

            DelayedAction.functionAfterDelay(delegate {
                heartSprite(soundloc);
            }, tally);
            tally += 500;

            DelayedAction.functionAfterDelay(delegate {
                FluteHeardToday = true;
                Game1.freezeControls = false;
                Game1.player.CanMove = true;
                Game1.player.onBridge.Value = false;
            }, tally);
        }

        public static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            /* Load in the flute object. Requires three asset edits */
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects")) {
                var modAsset = ModEntry.HELPER.ModContent.Load
                        <Dictionary<string, ObjectData>>("assets/key_item.json");
                e.Edit(asset => {
                    var dict = asset.AsDictionary<string, ObjectData>();
                    foreach (var entry in modAsset) {
                        dict.Data[entry.Key] = entry.Value;
                    }
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Strings/Objects")) {
                e.Edit(asset => {
                    var dict = asset.AsDictionary<string, string>();
                    dict.Data["ichortower.SecretWoodsSnorlax_StrangeFlute_Name"] =
                            ModEntry.HELPER.Translation.Get("object.strangeflute.name");
                    dict.Data["ichortower.SecretWoodsSnorlax_StrangeFlute_Description"] =
                            ModEntry.HELPER.Translation.Get("object.strangeflute.description");
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Mods/ichortower.SecretWoodsSnorlax/StrangeFlute")) {
                e.LoadFrom(() => {
                    return ModEntry.HELPER.ModContent.Load<Texture2D>("assets/key_item.png");
                }, AssetLoadPriority.Medium);
            }

            /* Load in the flute music cues */
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges")) {
                var modAsset = ModEntry.HELPER.ModContent.Load
                        <Dictionary<string, AudioCueData>>("assets/audio.json");
                foreach (var entry in modAsset) {
                    List<string> map = new();
                    foreach (var p in entry.Value.FilePaths) {
                        map.Add(Path.Combine(ModEntry.HELPER.DirectoryPath, p));
                    }
                    entry.Value.FilePaths = map;
                }
                e.Edit(asset => {
                    var dict = asset.AsDictionary<string, AudioCueData>();
                    foreach (var entry in modAsset) {
                        dict.Data[entry.Key] = entry.Value;
                    }
                });
            }

            /* Add CT dialogue keys.
             * tricky cheat here, splitting on '/'. through SMAPI, the asset
             * names passed to this function are already normalized */
            else if (e.NameWithoutLocale.IsDirectlyUnderPath("Characters/Dialogue")) {
                string npcName = e.NameWithoutLocale.BaseName.Split("/")[2];
                for (int i = 1; i <= 3; ++i) {
                    var hintline = ModEntry.HELPER.Translation.Get(
                            $"hint.{i}.{npcName}");
                    string key = $"{Constants.ct_Prefix}{i}";
                    if (hintline.HasValue()) {
                        e.Edit(asset => {
                            var dict = asset.AsDictionary<string, string>();
                            dict.Data[key] = hintline.ToString();
                        });
                    }
                }
            }

            /*
             * add the wizard event. actually three entries, since there's a
             * fork and rejoin to change one line depending on if you have
             * already moved the snorlax (only old saves should see that line).
             */
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/WizardHouse")) {
                string asset = "assets/events.json";
                if (ModEntry.HELPER.ModRegistry.IsLoaded(
                        "FlashShifter.StardewValleyExpandedCP")) {
                    asset = "assets/events_sve.json";
                }
                var snorlax = ModEntry.HELPER.ModContent.Load
                        <Dictionary<string,string>>(asset);
                foreach (var entry in snorlax) {
                    string val = entry.Value.Replace("{{moved}}",
                            Constants.mail_SnorlaxMoved);
                    int haveIndex = -1;
                    int start = 0;
                    while ((haveIndex = val.IndexOf("{{i18n", start)) != -1) {
                        int tail = val.IndexOf("}}", haveIndex+1);
                        string subkey = val.Substring(haveIndex+7, tail-(haveIndex+7));
                        string subval = ModEntry.HELPER.Translation.Get(subkey);
                        val = val.Remove(haveIndex, tail+2-haveIndex)
                                .Insert(haveIndex, subval);
                        start += subval.Length;
                    }
                    snorlax[entry.Key] = val;
                }
                e.Edit(asset => {
                    var dict = asset.AsDictionary<string, string>();
                    foreach (var entry in snorlax) {
                        dict.Data[entry.Key] = entry.Value;
                    }
                });
            }

            /* add the empty farm events that set the CTs when the hints mail
             * is active */
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Farm")) {
                e.Edit(asset => {
                    var dict = asset.AsDictionary<string, string>();
                    for (int i = 1; i <= 3; ++i) {
                        string key = $"19112010{i}/n {Constants.mail_SnorlaxHints}";
                        if (i > 1) {
                            key += $"/e 19112010{i-1}/A {Constants.ct_Prefix}{i-1}";
                        }
                        string script = "continue/-100 -100/farmer -1000 -1000 0" +
                                "/ignoreEventTileOffset" +
                                $"/addConversationTopic {Constants.ct_Prefix}{i} 2" +
                                "/pause 50/end";
                        dict.Data[key] = script;
                    }
                });
            }
        }
    }
}
