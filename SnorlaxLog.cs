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

        public override void draw(SpriteBatch spriteBatch, Vector2 tileLocation)
        {
            Rectangle sourceRect = Game1.getSourceRectForStandardTileSheet(
                    SnorlaxLog.SpriteSheet, this.parentSheetIndex.Value,
                    this.width.Value * 16, this.height.Value * 16);
            Vector2 position = this.tile.Value * 64f;
            position.Y -= yJumpOffset;
            spriteBatch.Draw(SnorlaxLog.SpriteSheet,
                    Game1.GlobalToLocal(Game1.viewport, position),
                    sourceRect, Color.White, 0f, Vector2.Zero, 4f,
                    SpriteEffects.None,
                    (this.tile.Y + 1f) * 64f / 10000f + this.tile.X / 100000f);
        }

        public override bool tickUpdate(GameTime time,
                Vector2 tileLocation, GameLocation location)
        {
            float prevOffset = yJumpOffset;
            if (yJumpVelocity != 0f) {
                yJumpOffset = Math.Max(0f, yJumpOffset + yJumpVelocity);
            }
            if (yJumpOffset > 0f) {
                yJumpVelocity += yJumpGravity;
            }
            if (prevOffset > 0f && yJumpOffset == 0f) {
                this.parentSheetIndex.Value = 0;
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
            var str = ModEntry.HELPER.Translation.Get("inspectMessage");
            Game1.drawObjectDialogue(Game1.parseText(str));
            return true;
        }

        public override bool performToolAction(Tool t, int damage,
                Vector2 tileLocation, GameLocation location)
        {
            if (t is null) {
                return false;
            }
            if (t is Axe) {
                location.playSound("axe");
                Game1.player.jitterStrength = 1f;
                var str = ModEntry.HELPER.Translation.Get("axeNoEffect");
                Game1.drawObjectDialogue(str);
            }
            else if (t is Pickaxe) {
                location.playSound("hammer");
                Game1.player.jitterStrength = 1f;
                var str = ModEntry.HELPER.Translation.Get("pickaxeNoEffect");
                Game1.drawObjectDialogue(str);
            }
            return false;
        }
    }

}
