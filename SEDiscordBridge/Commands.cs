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

        [Command("bridge-reload", "Reload current SEDB configuration")]
        [Permission(MyPromoteLevel.Admin)]
        public void ReloadBridge()
        {
            Plugin.InitConfig();
            Plugin.DDBridge?.SendStatus(null);

            if (Plugin.Config.Enabled)
            {
                if (Plugin.Torch.CurrentSession == null && !Plugin.Config.PreLoad)
                {
                    Plugin.UnloadSEDB();

                }
                else
                {
                    Plugin.LoadSEDB();
                }
            }
            else
            {
                Plugin.UnloadSEDB();
            }
            Context.Respond("SEDB plugin reloaded!");
        }

        [Command("bridge-enable", "To enable SEDB if disabled")]
        [Permission(MyPromoteLevel.Admin)]
        public void EnableBridge()
        {
            Plugin.LoadSEDB();
            Context.Respond("SEDB plugin enabled!");
        }

        [Command("bridge-disable", "To disable SEDB if enabled")]
        [Permission(MyPromoteLevel.Admin)]
        public void DisableBridge()
        {
            Plugin.UnloadSEDB();
            Context.Respond("SEDB plugin disabled!");
        }
    }
}
