using DSharpPlus;
using DSharpPlus.Entities;
using System.Threading;
using System.Threading.Tasks;
using Torch;
using Torch.API.Managers;
using Torch.Commands;
using VRage.Game;

namespace SEDiscordBridge
{
    public class DiscordBridge
    {
        private static SEDicordBridgePlugin Plugin;
        private static DiscordClient discord;
        private Thread thread;

        public DiscordBridge(SEDicordBridgePlugin plugin)
        {
            Plugin = plugin;

            thread = new Thread(() =>
            {
                RegisterDiscord().ConfigureAwait(false).GetAwaiter().GetResult();
            });
            thread.Start();            
        }

        public void Stopdiscord()
        {
            DisconnectDiscord().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task DisconnectDiscord()
        {            
            await discord.DisconnectAsync();
        }

        private Task RegisterDiscord()
        {            
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = Plugin.Config.BotToken,
                TokenType = TokenType.Bot
            });
            discord.ConnectAsync();

            discord.MessageCreated += Discord_MessageCreated;
            
            //start message
            if (Plugin.Config.Started.Length > 0)
                discord.SendMessageAsync(discord.GetChannelAsync(ulong.Parse(Plugin.Config.ChannelId)).Result, Plugin.Config.Started);
            return Task.CompletedTask;
        }

        public Task SendStatus(string status)
        {
            DiscordGame game = new DiscordGame()
            {
                Name = status
            };
            discord.UpdateStatusAsync(game);
            return Task.CompletedTask;
        }

        public void SendMessage(string user, string msg)
        {
            if (user != null)
            {
                string fmsg = Plugin.Config.Format.Replace("{p}", user).Replace("{msg}", msg);
                discord.SendMessageAsync(discord.GetChannelAsync(ulong.Parse(Plugin.Config.ChannelId)).Result, fmsg);
            }
            else
            {
                discord.SendMessageAsync(discord.GetChannelAsync(ulong.Parse(Plugin.Config.ChannelId)).Result, msg);
            }                
        }
        
        private Task Discord_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (!e.Author.IsBot)
            {
                if (e.Channel.Id.Equals(ulong.Parse(Plugin.Config.ChannelId)))
                {
                    string sender = Plugin.Config.ServerName;

                    if (!Plugin.Config.AsServer)
                        sender = e.Author.Username;
                    
                    Plugin.Torch.Invoke(() =>
                    {
                        var manager = Plugin.Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();
                        manager.SendMessageAsOther(Plugin.Config.Format2.Replace("{p}", sender), e.Message.Content, MyFontEnum.White);
                    });                        
                }
                if (e.Channel.Id.Equals(ulong.Parse(Plugin.Config.CommandChannelId)) && e.Message.Content.StartsWith(Plugin.Config.CommandPrefix))
                {
                    string cmd = e.Message.Content.Substring(0, Plugin.Config.CommandPrefix.Length - e.Message.Content.Length);
                    Plugin.Log.Warn("cmd: " + cmd);
                    Plugin.Torch.Invoke(() =>
                    {
                        var manager = Plugin.Torch.CurrentSession.Managers.GetManager<CommandManager>();
                        manager.HandleCommandFromServer(cmd);
                    });
                }
            }            
            return Task.CompletedTask;
        }

    }
}
