using System;

using Microsoft.Xna.Framework;
using Netcode;

using StardewValley;
using StardewValley.Monsters;
using StardewValley.Pathfinding;
using StardewValley.Projectiles;

using StardewModdingAPI;

using SUtility = StardewValley.Utility;

namespace FarmTypeManager;

public partial class ModEntry : Mod
{
    /// <summary>A subclass of Stardew's Skeleton class, adjusted for use by this mod.</summary>
    public class SkeletonFTM : Skeleton
    {
        /*** New fields ***/

        /// <summary>True if this monster's normal ranged attack behavior should be enabled.</summary>
        public bool RangedAttacks { get; set; } = true;

        /*** Reflected fields ***/

        private IReflectedField<bool> _spottedPlayer = null;
        /// <summary>A reflection wrapper for a non-public field in this monster's base class.</summary>
        public bool spottedPlayer
        {
            get
            {
                if (_spottedPlayer == null)
                    _spottedPlayer = Utility.Helper.Reflection.GetField<bool>(this, "spottedPlayer", true);
                return _spottedPlayer.GetValue();
            }
            set
            {
                if (_spottedPlayer == null)
                    _spottedPlayer = Utility.Helper.Reflection.GetField<bool>(this, "spottedPlayer", true);
                _spottedPlayer.SetValue(value);
            }
        }

        private IReflectedField<NetBool> _throwing = null;
        /// <summary>A reflection wrapper for a non-public field in this monster's base class.</summary>
        public NetBool throwing
        {
            get
            {
                if (_throwing == null)
                    _throwing = Utility.Helper.Reflection.GetField<NetBool>(this, "throwing", true);
                return _throwing.GetValue();
            }
        }

        private IReflectedField<int> _controllerAttemptTimer = null;
        /// <summary>A reflection wrapper for a non-public field in this monster's base class.</summary>
        public int controllerAttemptTimer
        {
            get
            {
                if (_controllerAttemptTimer == null)
                    _controllerAttemptTimer = Utility.Helper.Reflection.GetField<int>(this, "controllerAttemptTimer", true);
                return _controllerAttemptTimer.GetValue();
            }
            set
            {
                if (_controllerAttemptTimer == null)
                    _controllerAttemptTimer = Utility.Helper.Reflection.GetField<int>(this, "controllerAttemptTimer", true);
                _controllerAttemptTimer.SetValue(value);
            }
        }

        /*** Methods ***/

        /// <summary>Creates an instance of Stardew's Skeleton class, but with adjustments made for this mod.</summary>
        public SkeletonFTM() : base()
        {

        }

        /// <summary>Creates an instance of Stardew's Skeleton class, but with adjustments made for this mod.</summary>
        /// <param name="position">The x,y coordinates of this monster's location.</param>
        /// <param name="">True if this is a skeleton mage.</param>
        /// <param name="rangedAttacks">True if this monster's normal ranged attack behavior should be enabled.</param>
        public SkeletonFTM(Vector2 position, bool isMage = false, bool rangedAttacks = true) : base(position, isMage)
        {
            RangedAttacks = rangedAttacks;
        }

        /// <summary>A modified version of the base monster class's method.</summary>
        /// <remarks>
        /// Based on the original code of SDV v1.5.6. Modifed code sections are commented.
        /// Intended changes:
        /// * Fix a base game issue where Skeletons always use a sight range of 8
        /// * Implement a custom monster setting to disable ranged attacks (bone throwing)
        /// </remarks>
        public override void behaviorAtGameTick(GameTime time)
        {
            if (!this.throwing.Value)
            {
                this.Monster_behaviorAtGameTick(time); //replace inaccessible "base" call with a local copy
            }

            //replace 8 with the threshold value (a.k.a. sight range)
            if (!this.spottedPlayer && !base.wildernessFarmMonster && StardewValley.Utility.doesPointHaveLineOfSightInMine(base.currentLocation, base.Tile, base.Player.Tile, this.moveTowardPlayerThreshold.Value)) 
            {
                this.Player.StandingPixel.Deconstruct(out int x, out int y);

                base.controller = new PathFindController(this, base.currentLocation, new Point(x / 64, y / 64), -1, null, 200);
                this.spottedPlayer = true;

                if (base.controller == null || base.controller.pathToEndPoint == null || base.controller.pathToEndPoint.Count == 0)
                {
                    this.Halt();
                    base.facePlayer(base.Player);
                }

                base.currentLocation.playSound("skeletonStep");
                base.IsWalkingTowardPlayer = true;
            }
            else if (this.throwing.Value)
            {
                if (base.invincibleCountdown > 0)
                {
                    base.invincibleCountdown -= time.ElapsedGameTime.Milliseconds;
                    if (base.invincibleCountdown <= 0)
                    {
                        base.stopGlowing();
                    }
                }

                this.Sprite.Animate(time, 20, 5, 150f);

                if (this.Sprite.currentFrame == 24)
                {
                    this.throwing.Value = false;
                    this.Sprite.currentFrame = 0;
                    this.faceDirection(2);

                    var velocity = SUtility.getVelocityTowardPlayer(new Point((int)base.Position.X, (int)base.Position.Y), 8f, base.Player);

                    Projectile projectile;

                    if (this.isMage.Value)
                    {
                        if (Game1.random.NextDouble() < 0.5)
                        {
                            projectile = new DebuffingProjectile(Buff.frozen, 14, 4, 4, (float)Math.PI / 16f, velocity.X, velocity.Y, new Vector2(base.Position.X, base.Position.Y), base.currentLocation, this);
                        }
                        else
                        {
                            projectile = new BasicProjectile(base.DamageToFarmer * 2, 9, 0, 4, 0f, velocity.X, velocity.Y, new Vector2(base.Position.X, base.Position.Y), "flameSpellHit", "flameSpell", null, false, false, base.currentLocation, this);
                        }
                    }
                    else
                    {
                        projectile = new BasicProjectile(base.DamageToFarmer, 4, 0, 0, (float)Math.PI / 16f, velocity.X, velocity.Y, new Vector2(base.Position.X, base.Position.Y), "skeletonHit", "skeletonStep", null, false, false, base.currentLocation, this);
                    }

                    base.currentLocation.projectiles.Add(projectile);
                }
            } //check the ranged attacks setting before attempting to start throwing
            else if (RangedAttacks && this.spottedPlayer && base.controller == null && Game1.random.NextDouble() < (this.isMage.Value ? 0.008 : 0.002) && !base.wildernessFarmMonster && SUtility.doesPointHaveLineOfSightInMine(base.currentLocation, base.Tile, base.Player.Tile, 8))
            {
                this.throwing.Value = true;
                this.Halt();
                this.Sprite.currentFrame = 20;
                base.shake(750);
            }
            else if (this.withinPlayerThreshold(2))
            {
                base.controller = null;
            }
            else if (this.spottedPlayer && base.controller == null && this.controllerAttemptTimer <= 0)
            {
                base.Player.StandingPixel.Deconstruct(out int x, out int y);

                base.controller = new PathFindController(this, base.currentLocation, new Point(x / 64, y / 64), -1, null, 200);

                this.controllerAttemptTimer = (base.wildernessFarmMonster ? 2000 : 1000);
                if (base.controller == null || base.controller.pathToEndPoint == null || base.controller.pathToEndPoint.Count == 0)
                {
                    this.Halt();
                }
            }
            else if (base.wildernessFarmMonster)
            {
                this.spottedPlayer = true;
                base.IsWalkingTowardPlayer = true;
            }
            this.controllerAttemptTimer -= time.ElapsedGameTime.Milliseconds;
        }

        /// <summary>
        /// A copy of <see cref="Monster.behaviorAtGameTick(GameTime)"/> as a workaround for "base" calls in the overriden method.
        /// </summary>
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
            MovePosition(time, Game1.viewport, currentLocation);
        }
    }
}
