using NLog;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace SEDiscordBridge
{

    public class Commands : CommandModule
    {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public SEDiscordBridgePlugin Plugin => (SEDiscordBridgePlugin)Context.Plugin;

        [Command("reload-bridge", "Reload current SEDB configuration")]
        [Permission(MyPromoteLevel.Admin)]
        public void ReloadBridge()
        {
            Plugin.LoadSEDB();
            Context.Respond("Plugin reloaded!");
        }
    }
}
