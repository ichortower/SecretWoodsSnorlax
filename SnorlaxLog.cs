using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Tools;
using System;

namespace ichortower.SecretWoodsSnorlax
{
    internal class SnorlaxLog : StardewValley.TerrainFeatures.ResourceClump
    {
        public static string SpriteSheetName = "Maps\\SecretWoodsSnorlax";
        public static Texture2D SpriteSheet = null!;

        public float yJumpOffset = 0f;
        public float yJumpVelocity = 0f;
        public float yJumpGravity = -0.5f;
        public int jumpTicks = -1;

        public SnorlaxLog(float x, float y)
            : base()
        {
            this.width.Value = 3;
            this.height.Value = 3;
            this.parentSheetIndex.Value = 0;
            this.tile.Value = new Vector2(x, y);
            this.health.Value = 160; // snorlax's base HP stat
            if (SnorlaxLog.SpriteSheet is null) {
                SnorlaxLog.SpriteSheet = ModEntry.HELPER.ModContent
                        .Load<Texture2D>("assets/map_snorlax.png");
            }
        }

        /*
         * The jump is firmcoded. This just sets the values, and draw and
         * tickUpdate actually handle it.
         */
        public void JumpAside()
        {
            this.parentSheetIndex.Value = 2;
            this.yJumpVelocity = 16;
            this.jumpTicks = 0;
        }

        public bool HasMoved()
        {
            if (Game1.player.mailReceived.Contains(Events.SnorlaxMailId)) {
                return true;
            }
            return this.tile.Value.X > 1f;
        }

        public override void draw(SpriteBatch spriteBatch, Vector2 tileLocation)
        {
            Rectangle sourceRect = Game1.getSourceRectForStandardTileSheet(
                    SnorlaxLog.SpriteSheet, this.parentSheetIndex.Value,
                    this.width.Value * 16, this.height.Value * 16);
            Vector2 position = this.tile.Value * 64f;
            position.Y -= yJumpOffset;
            if (jumpTicks > 0) {
                position.X += 2 * jumpTicks;
                position.Y -= 2 * jumpTicks;
            }
            spriteBatch.Draw(SnorlaxLog.SpriteSheet,
                    Game1.GlobalToLocal(Game1.viewport, position),
                    sourceRect, Color.White, 0f, Vector2.Zero, 4f,
                    SpriteEffects.None,
                    (this.tile.Y + 1f) * 64f / 10000f + this.tile.X / 100000f);
        }

        public override bool tickUpdate(GameTime time,
                Vector2 tileLocation, GameLocation location)
        {
            if (jumpTicks >= 0) {
                ++jumpTicks;
            }
            float prevOffset = yJumpOffset;
            if (yJumpVelocity != 0f) {
                yJumpOffset = Math.Max(0f, yJumpOffset + yJumpVelocity);
            }
            if (yJumpOffset > 0f) {
                yJumpVelocity += yJumpGravity;
            }
            if (prevOffset > 0f && yJumpOffset == 0f) {
                this.parentSheetIndex.Value = 0;
                this.jumpTicks = -1;
                this.tile.Value = new Vector2(3f, 4f);
                location.playSoundAt("clubSmash", this.tile.Value);
                location.playSoundAt("treethud", this.tile.Value);
            }
            return base.tickUpdate(time, tileLocation, location);
        }

        public override bool performUseAction(Vector2 tileLocation,
                GameLocation location)
        {
            if (!Game1.didPlayerJustRightClick(true)) {
                Game1.haltAfterCheck = false;
                return false;
            }
            string key = HasMoved() ? "inspect.moved" : "inspect.unmoved";
            string text = ModEntry.HELPER.Translation.Get(key);
            Game1.drawObjectDialogue(Game1.parseText(text));
            return true;
        }

        public override bool performToolAction(Tool t, int damage,
                Vector2 tileLocation, GameLocation location)
        {
            if (t is null) {
                return false;
            }
            if (!HasMoved()) {
                if (t is Axe) {
                    location.playSound("woodyHit");
                    Game1.player.jitterStrength = 1f;
                    var str = ModEntry.HELPER.Translation.Get("tool.noEffect");
                    Game1.drawObjectDialogue(str);
                }
                else if (t is Pickaxe) {
                    location.playSound("woodyHit");
                    Game1.player.jitterStrength = 1f;
                    var str = ModEntry.HELPER.Translation.Get("tool.noEffect");
                    Game1.drawObjectDialogue(str);
                }
            }
            return false;
        }
    }

}
