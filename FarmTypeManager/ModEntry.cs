using StardewModdingAPI;

namespace FarmTypeManager;

/// <summary>
/// The mod entry point.
/// </summary>
public partial class ModEntry : Mod
{
    /// <summary>
    /// Tasks performed when the mod initially loads.
    /// </summary>
    public override void Entry(IModHelper helper)
    {
        // pass SMAPI utilities to the Utility class for global use
        Utility.Monitor.IMonitor = Monitor;
        Utility.Helper = helper;
        Utility.Manifest = ModManifest;

        // Attempt to load the config.json ModConfig file
        Utility.LoadModConfig();

        // If enabled, pass the mod's console command methods to the helper
        if (Utility.MConfig?.EnableConsoleCommands == true) 
        {
            helper.ConsoleCommands.Add("whereami", "Outputs coordinates and other information about the player's current location.", this.WhereAmI);
            helper.ConsoleCommands.Add("list_monsters", "Outputs a list of available monster types, including custom types loaded by other mods.", this.ListMonsters);
        }

        // Pass any necessary event methods to SMAPI
        this.AddSMAPIEvents(helper);

        // Pass any necessary patches to Harmony
        this.ApplyHarmonyPatches();
    }
}