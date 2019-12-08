﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace FarmTypeManager
{
    public partial class ModEntry : Mod
    {
        /// <summary>Methods involved in spawning objects into the game.</summary> 
        private static partial class Generation
        {
            /// <summary>Generates ore in the game based on the current player's config settings.</summary>
            public static void OreGeneration()
            {
                foreach (FarmData data in Utility.FarmDataList)
                {
                    if (data.Pack != null) //content pack
                    {
                        Utility.Monitor.Log($"Starting ore generation for this content pack: {data.Pack.Manifest.Name}", LogLevel.Trace);
                    }
                    else //not a content pack
                    {
                        Utility.Monitor.Log($"Starting ore generation for this file: FarmTypeManager/data/{Constants.SaveFolderName}.json", LogLevel.Trace);
                    }

                    if (data.Config.Ore_Spawn_Settings != null) //if this config contains ore settings
                    {
                        if (data.Config.OreSpawnEnabled)
                        {
                            Utility.Monitor.Log("Ore generation is enabled. Starting generation process...", LogLevel.Trace);
                        }
                        else
                        {
                            Utility.Monitor.Log($"Ore generation is disabled for this {(data.Pack == null ? "file" : "content pack")}.", LogLevel.Trace);
                            continue;
                        }
                    }
                    else //if this config's ore settings are null
                    {
                        Utility.Monitor.Log($"This {(data.Pack == null ? "file" : "content pack")}'s ore spawn settings are blank.", LogLevel.Trace);
                        continue;
                    }

                    foreach (OreSpawnArea area in data.Config.Ore_Spawn_Settings.Areas)
                    {
                        Utility.Monitor.Log($"Checking ore settings for this area: \"{area.UniqueAreaID}\" ({area.MapName})", LogLevel.Trace);

                        //validate the map name for the area
                        List<GameLocation> locations = Utility.GetAllLocationsFromName(area.MapName); //get all locations for this map name
                        if (locations.Count == 0) //if no locations were found
                        {
                            Utility.Monitor.Log($"No map named \"{area.MapName}\" could be found. Ore won't be spawned there.", LogLevel.Trace);
                            continue;
                        }

                        //validate extra conditions, if any
                        if (Utility.CheckExtraConditions(area, data.Save) != true)
                        {
                            Utility.Monitor.Log($"Extra conditions prevent spawning in this area ({area.MapName}). Next area...", LogLevel.Trace);
                            continue;
                        }

                        Utility.Monitor.Log("All extra conditions met. Determining spawn chances for ore...", LogLevel.Trace);

                        //figure out which config section to use (if the spawn area's data is null, use the "global" data instead)
                        Dictionary<string, int> skillReq = area.MiningLevelRequired ?? data.Config.Ore_Spawn_Settings.MiningLevelRequired;
                        Dictionary<string, int> startChance = area.StartingSpawnChance ?? data.Config.Ore_Spawn_Settings.StartingSpawnChance;
                        Dictionary<string, int> tenChance = area.LevelTenSpawnChance ?? data.Config.Ore_Spawn_Settings.LevelTenSpawnChance;
                        //also use the "global" data if the area data is non-null but empty (which can happen accidentally when the json file is manually edited)
                        if (skillReq.Count < 1)
                        {
                            skillReq = data.Config.Ore_Spawn_Settings.MiningLevelRequired;
                        }
                        if (startChance.Count < 1)
                        {
                            startChance = data.Config.Ore_Spawn_Settings.StartingSpawnChance;
                        }
                        if (tenChance.Count < 1)
                        {
                            tenChance = data.Config.Ore_Spawn_Settings.LevelTenSpawnChance;
                        }

                        //calculate the final spawn chance for each type of ore
                        Dictionary<string, int> oreChances = Utility.AdjustedSpawnChances(Utility.Skills.Mining, skillReq, startChance, tenChance);

                        if (oreChances.Count < 1) //if there's no chance of spawning any ore for some reason, just stop working on this area now
                        {
                            Utility.Monitor.Log("No chance of spawning any ore. Skipping to the next area...", LogLevel.Trace);
                            continue;
                        }

                        Utility.Monitor.Log($"Spawn chances complete. Beginning generation process...", LogLevel.Trace);

                        for (int x = 0; x < locations.Count; x++) //for each location matching this area's map name
                        {
                            //calculate how much ore to spawn today
                            int spawnCount = Utility.AdjustedSpawnCount(area.MinimumSpawnsPerDay, area.MaximumSpawnsPerDay, data.Config.Ore_Spawn_Settings.PercentExtraSpawnsPerMiningLevel, Utility.Skills.Mining);

                            if (locations.Count > 1) //if this area targets multiple locations
                            {
                                Utility.Monitor.Log($"Potential spawns at {locations[x].Name} #{x + 1}: {spawnCount}.", LogLevel.Trace);
                            }
                            else //if this area only targets one location
                            {
                                Utility.Monitor.Log($"Potential spawns at {locations[x].Name}: {spawnCount}.", LogLevel.Trace);
                            }

                            List<SavedObject> spawns = new List<SavedObject>(); //the list of objects to be spawned

                            //begin to generate ore
                            int randomOreNum;
                            while (spawnCount > 0) //while more ore should be spawned
                            {
                                spawnCount--;

                                int totalWeight = 0; //the upper limit for the random number that picks ore type (i.e. the sum of all ore chances)
                                foreach (KeyValuePair<string, int> ore in oreChances)
                                {
                                    totalWeight += ore.Value; //sum up all the ore chances
                                }
                                randomOreNum = Utility.RNG.Next(totalWeight); //generate random number from 0 to [totalWeight - 1]
                                foreach (KeyValuePair<string, int> ore in oreChances)
                                {
                                    if (randomOreNum < ore.Value) //this ore "wins"
                                    {
                                        //create a saved object representing this spawn (with a "blank" tile location)
                                        SavedObject saved = new SavedObject(locations[x].uniqueName.Value ?? locations[x].Name, new Vector2(), SavedObject.ObjectType.Ore, null, ore.Key, area.DaysUntilSpawnsExpire);
                                        spawns.Add(saved); //add it to the list

                                        break;
                                    }
                                    else //this ore "loses"
                                    {
                                        randomOreNum -= ore.Value; //subtract this ore's chance from the random number before moving to the next one
                                    }
                                }
                            }

                            Utility.PopulateTimedSpawnList(spawns, data, area); //process the listed spawns and add them to Utility.TimedSpawns
                        }

                        Utility.Monitor.Log($"Ore generation process complete for this area: \"{area.UniqueAreaID}\" ({area.MapName})", LogLevel.Trace);
                    }

                    if (data.Pack != null) //content pack
                    {
                        Utility.Monitor.Log($"All areas checked. Ore generation complete for this content pack: {data.Pack.Manifest.Name}", LogLevel.Trace);
                    }
                    else //not a content pack
                    {
                        Utility.Monitor.Log($"All areas checked. Ore generation complete for this file: FarmTypeManager/data/{Constants.SaveFolderName}.json", LogLevel.Trace);
                    }
                }

                Utility.Monitor.Log("All files and content packs checked. Ore spawn process complete.", LogLevel.Trace);
            }
        }
    }
}
