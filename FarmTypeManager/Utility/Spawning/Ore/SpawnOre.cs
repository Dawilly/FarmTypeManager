using Microsoft.Xna.Framework;

using StardewValley;

using StardewModdingAPI;

using SObject = StardewValley.Object;

namespace FarmTypeManager;

public partial class ModEntry : Mod
{
    /// <summary>Methods used repeatedly by other sections of this mod, e.g. to locate tiles.</summary>
    private static partial class Utility
    {
        /// <summary>Generates ore and places it on the specified map and tile.</summary>
        /// <param name="oreName">A string representing the name of the ore type to be spawned, e.g. "stone"</param>
        /// <param name="location">The GameLocation where the ore should be spawned.</param>
        /// <param name="tile">The x/y coordinates of the tile where the ore should be spawned.</param>
        /// <returns>The spawned ore's parentSheetIndex. Null if spawn failed.</returns>
        public static int? SpawnOre(string oreName, GameLocation location, Vector2 tile)
        {
            SObject ore = null; //the ore object to spawn

            switch (oreName.ToLower()) //avoid any casing issues in method calls by making this lower-case
            {
                case "stone":
                    ore = new SObject(tile, 668 + (RNG.Next(2) * 2), 1); //either of the two random stones spawned in the vanilla hilltop quarry
                    break;
                case "geode":
                    ore = new SObject(tile, 75, 1);
                    break;
                case "frozengeode":
                    ore = new SObject(tile, 76, 1);
                    break;
                case "magmageode":
                    ore = new SObject(tile, 77, 1);
                    break;
                case "omnigeode":
                    ore = new SObject(tile, 819, "Stone", true, false, false);
                    break;
                case "gem":
                    ore = new SObject(tile, (RNG.Next(7) + 1) * 2, "Stone", true, false, false); //any of the specific gem nodes (NOT the gem node with ID 44, which was considered excessively high-reward)
                    break;
                case "copper":
                    ore = new SObject(tile, 751, 1);
                    break;
                case "iron":
                    ore = new SObject(tile, 290, 1);
                    break;
                case "gold":
                    ore = new SObject(tile, 764, 1);
                    break;
                case "iridium":
                    ore = new SObject(tile, 765, 1);
                    break;
                case "mystic":
                    ore = new SObject(tile, 46, "Stone", true, false, false);
                    break;
                case "radioactive":
                    ore = new SObject(tile, 95, "Stone", true, false, false);
                    break;
                case "diamond":
                    ore = new SObject(tile, 2, "Stone", true, false, false);
                    break;
                case "ruby":
                    ore = new SObject(tile, 4, "Stone", true, false, false);
                    break;
                case "jade":
                    ore = new SObject(tile, 6, "Stone", true, false, false);
                    break;
                case "amethyst":
                    ore = new SObject(tile, 8, "Stone", true, false, false);
                    break;
                case "topaz":
                    ore = new SObject(tile, 10, "Stone", true, false, false);
                    break;
                case "emerald":
                    ore = new SObject(tile, 12, "Stone", true, false, false);
                    break;
                case "aquamarine":
                    ore = new SObject(tile, 14, "Stone", true, false, false);
                    break;
                case "mussel":
                    ore = new SObject(tile, 25, "Stone", true, false, false);
                    break;
                case "fossil":
                    ore = new SObject(tile, 816 + RNG.Next(2), 1); //either of the two random fossil nodes
                    break;
                case "clay":
                    ore = new SObject(tile, 818, 1);
                    break;
                case "cindershard":
                    ore = new SObject(tile, 843 + RNG.Next(2), "Stone", true, false, false);
                    break;

                default: break;
            }

            if (ore == null)
            {
                Utility.Monitor.Log($"The ore to be spawned (\"{oreName}\") doesn't match any known ore types. Make sure that name isn't misspelled in your config file.", LogLevel.Info);
                return null;
            }

            int? durability = Utility.GetDefaultDurability(ore.ParentSheetIndex); //try to get this ore type's default durability
            if (durability.HasValue) //if a default exists
                ore.MinutesUntilReady = durability.Value; //use it

            Utility.Monitor.VerboseLog($"Spawning ore. Type: {oreName}. Location: {tile.X},{tile.Y} ({location.Name}).");
            location.setObject(tile, ore); //actually spawn the ore object into the world
            return ore.ParentSheetIndex;
        }
    }
}