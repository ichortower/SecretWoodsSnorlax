using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace ichortower.SecretWoodsSnorlax
{

    internal sealed class ModEntry : Mod
    {
        public static IMonitor MONITOR;
        public static IModHelper HELPER;

        public override void Entry(IModHelper helper)
        {
            ModEntry.MONITOR = this.Monitor;
            ModEntry.HELPER = helper;
            helper.Events.GameLoop.GameLaunched += Events.OnGameLaunched;
            helper.Events.GameLoop.DayStarted += Events.OnDayStarted;
            helper.Events.GameLoop.Saving += Events.OnSaving;
            helper.Events.Input.ButtonsChanged += Events.OnButtonsChanged;
            helper.Events.Content.AssetRequested += Events.OnAssetRequested;
        }
    }

}
