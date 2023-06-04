using System;

using Microsoft.Xna.Framework;

using StardewValley;
using StardewValley.Monsters;
using StardewValley.Pathfinding;

using StardewModdingAPI;

using SUtility = StardewValley.Utility;

namespace FarmTypeManager;

public partial class ModEntry : Mod
{
    /// <summary>A subclass of Stardew's Dust Sprite class, adjusted for use by this mod.</summary>
    public class DustSpiritFTM : DustSpirit
    {
        protected IReflectedField<bool> SeenFarmer;
        protected IReflectedField<bool> RunningAwayFromFarmer;
        protected IReflectedField<bool> ChargingFarmer;
        protected IReflectedField<Multiplayer> Multiplayer;

        /// <summary>Creates an instance of Stardew's Dust Sprite class, but with adjustments made for this mod.</summary>
        public DustSpiritFTM() : base()
        {
            this.InitializeReflection();
        }

        /// <summary>Creates an instance of Stardew's Dust Sprite class, but with adjustments made for this mod.</summary>
        /// <param name="position">The x,y coordinates of this monster's location.</param>
        public DustSpiritFTM(Vector2 position) : base(position)
        {
            this.InitializeReflection();
        }

        protected virtual void InitializeReflection()
        {
            this.SeenFarmer = Utility.Helper.Reflection.GetField<bool>(this, "seenFarmer");
            this.RunningAwayFromFarmer = Utility.Helper.Reflection.GetField<bool>(this, "runningAwayFromFarmer");
            this.ChargingFarmer = Utility.Helper.Reflection.GetField<bool>(this, "chargingFarmer");
            this.Multiplayer = Utility.Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer");
        }

        /// <summary>
        /// Overrides the base method to fix the following issues:
        /// * GameLocation.destroyObject causes an error in some locations if the "who" argument is null.
        /// </summary>
        public override void behaviorAtGameTick(GameTime time)
        {
            // Call a copy of the base method
            this.Monster_behaviorAtGameTick(time); 

            if (yJumpOffset == 0)
            {
                if (Game1.random.NextDouble() < 0.01)
                {
                    Multiplayer.GetValue().broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 128, 64, 64), 40f, 4, 0, getStandingPosition() + new Vector2(-21f, 0f), flicker: false, flipped: false)
                    {
                        layerDepth = (getStandingPosition().Y - 10f) / 10000f
                    });

                    foreach (Vector2 v2 in SUtility.getAdjacentTileLocations(this.Tile))
                    {
                        if (base.currentLocation.objects.ContainsKey(v2) && (base.currentLocation.objects[v2].Name.Contains("Stone") || base.currentLocation.objects[v2].Name.Contains("Twig")))
                        {
                            // Modify destruction to credit the monster's currently targeted player
                            // Note: Player => findPlayer() currently never returns null, but may change or be Harmony patched
                            Farmer who = this.Player;

                            if (who != null)
                            {
                                base.currentLocation.destroyObject(v2, this.Player);
                            }
                        }
                    }
                    yJumpVelocity *= 2f;
                }

                if (!ChargingFarmer.GetValue())
                {
                    xVelocity = (float)Game1.random.Next(-20, 21) / 5f;
                }
            }

            if (ChargingFarmer.GetValue())
            {
                base.Slipperiness = 10;
                Vector2 v = SUtility.getAwayFromPlayerTrajectory(GetBoundingBox(), base.Player);
                xVelocity += (0f - v.X) / 150f + ((Game1.random.NextDouble() < 0.01) ? ((float)Game1.random.Next(-50, 50) / 10f) : 0f);
                if (Math.Abs(xVelocity) > 5f)
                {
                    xVelocity = Math.Sign(xVelocity) * 5;
                }
                yVelocity += (0f - v.Y) / 150f + ((Game1.random.NextDouble() < 0.01) ? ((float)Game1.random.Next(-50, 50) / 10f) : 0f);
                if (Math.Abs(yVelocity) > 5f)
                {
                    yVelocity = Math.Sign(yVelocity) * 5;
                }
                if (Game1.random.NextDouble() < 0.0001)
                {
                    controller = new PathFindController(this, base.currentLocation, new Point((int)base.Player.Tile.X, (int)base.Player.Tile.Y), Game1.random.Next(4), null, 300);
                    ChargingFarmer.SetValue(false);
                }
                if (isHardModeMonster.Value && CaughtInWeb())
                {
                    xVelocity = 0f;
                    yVelocity = 0f;
                    if (shakeTimer <= 0 && Game1.random.NextDouble() < 0.05)
                    {
                        shakeTimer = 200;
                    }
                }
            }
            else if (!SeenFarmer.GetValue() && SUtility.doesPointHaveLineOfSightInMine(base.currentLocation, getStandingPosition() / 64f, base.Player.getStandingPosition() / 64f, 8))
            {
                SeenFarmer.SetValue(true);
            }
            else if (SeenFarmer.GetValue() && controller == null && !RunningAwayFromFarmer.GetValue())
            {
                addedSpeed = 2;
                controller = new PathFindController(this, base.currentLocation, SUtility.isOffScreenEndFunction, -1, offScreenBehavior, 350, Point.Zero);
                RunningAwayFromFarmer.SetValue(true);
            }
            else if (controller == null && RunningAwayFromFarmer.GetValue())
            {
                ChargingFarmer.SetValue(true);
            }
        }

        /// <summary>A local copy of <see cref="Monster.behaviorAtGameTick(GameTime)"/> to call in replaced subclass methods.</summary>
        public void Monster_behaviorAtGameTick(GameTime time)
        {
            if (timeBeforeAIMovementAgain > 0f)
            {
                timeBeforeAIMovementAgain -= time.ElapsedGameTime.Milliseconds;
            }

            if (!Player.isRafting || !withinPlayerThreshold(4))
            {
                return;
            }

            if (Math.Abs(Player.GetBoundingBox().Center.Y - GetBoundingBox().Center.Y) > 192)
            {
                if (Player.GetBoundingBox().Center.X - GetBoundingBox().Center.X > 0)
                {
                    SetMovingLeft(b: true);
                }
                else
                {
                    SetMovingRight(b: true);
                }
            }
            else if (Player.GetBoundingBox().Center.Y - GetBoundingBox().Center.Y > 0)
            {
                SetMovingUp(b: true);
            }
            else
            {
                SetMovingDown(b: true);
            }

            MovePosition(time, Game1.viewport, base.currentLocation);
        }
    }
}
