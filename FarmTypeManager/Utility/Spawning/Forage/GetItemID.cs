﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace FarmTypeManager
{
    public partial class ModEntry : Mod
    {
        /// <summary>Methods used repeatedly by other sections of this mod, e.g. to locate tiles.</summary>
        private static partial class Utility
        {
            /// <summary>Returns the ID of an item with the given name and category, or null if no such item could be found.</summary>
            /// <param name="category">The name of the item category to search (e.g. "furniture" or "weapon").</param>
            /// <param name="name">The item's name.</param>
            /// <returns>The ID of the item. Null if the item was not found.</returns>
            public static int? GetItemID(string category, string name)
            {
                IDictionary<int, string> itemsInfo = GetItemDictionary(category); //get a dictionary of item information for the provided category

                if (itemsInfo != null) //if the category's information was successfully retrieved
                {
                    foreach (KeyValuePair<int, string> itemInfo in itemsInfo) //for each entry in the dictionary
                    {
                        if (itemInfo.Value.Split('/')[0].Equals(name, StringComparison.OrdinalIgnoreCase)) //if this entry's item name matches the provided name
                        {
                            return itemInfo.Key; //return the item's ID
                        }
                    }
                }

                return null; //no item could be found with this category and name
            }
        }
    }
}