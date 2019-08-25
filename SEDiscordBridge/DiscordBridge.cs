using DSharpPlus;
using DSharpPlus.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Commands;
using VRage.Game;
using VRage.Game.ModAPI;

namespace SEDiscordBridge
{
    public class DiscordBridge
    {
        private static SEDiscordBridgePlugin Plugin;
        private static DiscordClient discord;
        private Thread thread;
        private DiscordGame game;
        private string lastMessage = "";
        TimeZone zone = TimeZone.CurrentTimeZone;
        
        public bool Ready { get; set; } = false;

        public DiscordBridge(SEDiscordBridgePlugin plugin)
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
            thread = new Thread(() =>
            {
                DisconnectDiscord().ConfigureAwait(false).GetAwaiter().GetResult();
            });
            thread.Start();
        }

        private async Task DisconnectDiscord()
        {
            Ready = false;
            await discord?.DisconnectAsync();
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
            game = new DiscordGame();

            discord.Ready += async e => Ready = true;
            return Task.CompletedTask;
        }

        public void SendStatus(string status)
        {
            if (Ready && status?.Length > 0)
            {
                game.Name = status;
                discord.UpdateStatusAsync(game);
            }            
        }

        public void SendChatMessage(string user, string msg, bool console)
        {
            DateTime local = zone.ToLocalTime(DateTime.Now);
            try
            {
                if (lastMessage.Equals(user + msg)) return;

                if (Ready && Plugin.Config.ChatChannelId.Length > 0)
                {
                    DiscordChannel chann = discord.GetChannelAsync(ulong.Parse(Plugin.Config.ChatChannelId)).Result;
                    //mention
                    msg = MentionNameToID(msg, chann, console);

                    if (user != null)
                    {
                        msg = Plugin.Config.Format.Replace("{msg}", msg).Replace("{p}", user).Replace("{ts}",local.ToString());
                    }

                    discord.SendMessageAsync(chann, msg.Replace("/n", "\n"));
                }
            }
            catch (Exception e)
            {
                DiscordChannel chann = discord.GetChannelAsync(ulong.Parse(Plugin.Config.ChatChannelId)).Result;
                discord.SendMessageAsync(chann, e.ToString());
            }
        }

        public void SendFacChatMessage(string user, string msg, string facName, bool console)
        {
            IEnumerable<string> channelIds = Plugin.Config.FactionChannels.Where(c => c.Split(':')[0].Equals(facName));
            if (Ready && channelIds.Count() > 0)
            {
                foreach (string chId in channelIds)
                {
                    DiscordChannel chann = discord.GetChannelAsync(ulong.Parse(chId.Split(':')[1])).Result;
                    //mention
                    msg = MentionNameToID(msg, chann, console);

                    if (user != null)
                    {
                        msg = Plugin.Config.FacFormat.Replace("{msg}", msg).Replace("{p}", user);
                    }
                    discord.SendMessageAsync(chann, msg.Replace("/n", "\n"));
                }
            }            
        }

        public void SendStatusMessage(string user, string msg)
        {
            if (Ready && Plugin.Config.StatusChannelId.Length > 0)
            {
                DiscordChannel chann = discord.GetChannelAsync(ulong.Parse(Plugin.Config.StatusChannelId)).Result;

                if (user != null)
                {
                    if (user.StartsWith("ID:"))
                        return;

                    msg = msg.Replace("{p}", user);
                }

                discord.SendMessageAsync(chann, msg.Replace("/n","\n"));
            }
        }

        private Task Discord_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (!e.Author.IsBot)
            {
                string comChannelId = Plugin.Config.CommandChannelId;
                if (comChannelId != "") {
                    //execute commands
                    if (e.Channel.Id.Equals(ulong.Parse(Plugin.Config.CommandChannelId)) && e.Message.Content.StartsWith(Plugin.Config.CommandPrefix))
                    {
                        string cmd = e.Message.Content.Substring(Plugin.Config.CommandPrefix.Length);
                        var cmdText = new string(cmd.Skip(1).ToArray());

                        if (Plugin.Torch.CurrentSession?.State == TorchSessionState.Loaded)
                        {
                            var manager = Plugin.Torch.CurrentSession.Managers.GetManager<CommandManager>();
                            var command = manager.Commands.GetCommand(cmdText, out string argText);

                            if (command == null)
                            {
                                SendCmdResponse("Command not found: " + cmdText, e.Channel);
                            }
                            else
                            {
                                var cmdPath = string.Join(".", command.Path);
                                var splitArgs = Regex.Matches(argText, "(\"[^\"]+\"|\\S+)").Cast<Match>().Select(x => x.ToString().Replace("\"", "")).ToList();
                                SEDiscordBridgePlugin.Log.Trace($"Invoking {cmdPath} for server.");

                                var context = new SEDBCommandHandler(Plugin.Torch, command.Plugin, Sync.MyId, argText, splitArgs);
                                context.ResponeChannel = e.Channel;
                                context.OnResponse += OnCommandResponse;
                                var invokeSuccess = false;
                                Plugin.Torch.InvokeBlocking(() => invokeSuccess = command.TryInvoke(context));
                                SEDiscordBridgePlugin.Log.Debug($"invokeSuccess {invokeSuccess}");
                                if (!invokeSuccess)
                                {
                                    SendCmdResponse("Error executing command: " + cmdText, e.Channel);
                                }
                                SEDiscordBridgePlugin.Log.Info($"Server ran command '{string.Join(" ", cmdText)}'");
                            }
                        }
                        else
                        {
                            SendCmdResponse("Error: Server is not running.", e.Channel);
                        }
                        return Task.CompletedTask;
                    }
                }

                //send to global
                if (e.Channel.Id.Equals(ulong.Parse(Plugin.Config.ChatChannelId)))
                {
                    string sender = Plugin.Config.ServerName;

                    if (!Plugin.Config.AsServer)
                    {
                        if (Plugin.Config.UseNicks)
                            sender = e.Guild.GetMemberAsync(e.Author.Id).Result.Nickname;
                        else
                            sender = e.Author.Username;
                    }                        
                    
                    var manager = Plugin.Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();
                    var dSender = Plugin.Config.Format2.Replace("{p}", sender);
                    var msg = MentionIDToName(e.Message);
                    lastMessage = dSender + msg;
                    manager.SendMessageAsOther(dSender, msg,
                        typeof(MyFontEnum).GetFields().Select(x => x.Name).Where(x => x.Equals(Plugin.Config.GlobalColor)).First());
                }

                //send to faction
                IEnumerable<string> channelIds = Plugin.Config.FactionChannels.Where(c => e.Channel.Id.Equals(ulong.Parse(c.Split(':')[1])));
                if (channelIds.Count() > 0)
                {
                    foreach (string chId in channelIds)
                    {
                        IEnumerable<IMyFaction> facs = MySession.Static.Factions.Factions.Values.Where(f => f.Name.Equals(chId.Split(':')[0]));
                        if (facs.Count() > 0)
                        {
                            IMyFaction fac = facs.First();
                            foreach (MyFactionMember mb in fac.Members.Values)
                            {                                
                                if (!MySession.Static.Players.GetOnlinePlayers().Any(p => p.Identity.IdentityId.Equals(mb.PlayerId)))
                                    continue;

                                ulong steamid = MySession.Static.Players.TryGetSteamId(mb.PlayerId);
                                string sender = Plugin.Config.ServerName;
                                if (!Plugin.Config.AsServer)
                                {
                                    if (Plugin.Config.UseNicks)
                                        sender = e.Guild.GetMemberAsync(e.Author.Id).Result.Nickname;
                                    else
                                        sender = e.Author.Username;
                                }
                                var manager = Plugin.Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();
                                var dSender = Plugin.Config.Format2.Replace("{p}", sender);
                                var msg = MentionIDToName(e.Message);
                                lastMessage = dSender + msg;
                                manager.SendMessageAsOther(dSender, msg,
                                    typeof(MyFontEnum).GetFields().Select(x => x.Name).Where(x => x.Equals(Plugin.Config.FacColor)).First(), steamid);
                            }
                        }                        
                    }
                }
            }            
            return Task.CompletedTask;
        }
        
        private void SendCmdResponse(string response, DiscordChannel chann)
        {
            DiscordMessage dms = discord.SendMessageAsync(chann, response).Result;
            if (Plugin.Config.RemoveResponse > 0)
                Task.Delay(Plugin.Config.RemoveResponse*1000).ContinueWith(t => dms?.DeleteAsync());
        }

        private string MentionNameToID(string msg, DiscordChannel chann, bool console)
        {
            try
            {
                
                var parts = msg.Split(' ');
                foreach (string part in parts)
                {
                    if (part.Length > 2)
                    {
                        if (part.StartsWith("@"))
                        {
                            string name = Regex.Replace(part.Substring(1), @"[,#]", "");
                            if (String.Compare(name, "everyone", true) == 0 && !Plugin.Config.MentEveryone)
                            {
                                msg = msg.Replace(part, part.Substring(1));
                                continue;
                            }

                            if (!console)
                            {
                                try
                                {
                                    var members = chann.Guild.GetAllMembersAsync().Result;

                                    if (!Plugin.Config.MentOthers)
                                    {
                                        continue;
                                    }
                                    var memberByNickname = members.FirstOrDefault((u) => String.Compare(u.Nickname, name, true) == 0);
                                    if (memberByNickname != null)
                                    {
                                        msg = msg.Replace(part, $"<@{memberByNickname.Id}>");
                                        continue;
                                    }
                                    var memberByUsername = members.FirstOrDefault((u) => String.Compare(u.Username, name, true) == 0);
                                    if (memberByUsername != null)
                                    {
                                        msg = msg.Replace(part, $"<@{memberByUsername.Id}>");
                                        continue;
                                    }
                                }
                                catch (Exception)
                                {
                                    SEDiscordBridgePlugin.Log.Warn("Error on convert a member id to name on mention other players.");
                                    continue;
                                }
                            }
                        }

                        var emojis = chann.Guild.Emojis;
                        if (part.StartsWith(":") && part.EndsWith(":") && emojis.Any(e => String.Compare(e.GetDiscordName(), part, true) == 0))
                        {
                            msg = msg.Replace(part, "<" + part + emojis.Where(e => String.Compare(e.GetDiscordName(), part, true) == 0).First().Id + ">");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SEDiscordBridgePlugin.Log.Warn(e, "Error on convert a member id to name on mention other players.");
            }
            return msg;
        }

        private string MentionIDToName(DiscordMessage ddMsg)
        {
            string msg = ddMsg.Content;
            var parts = msg.Split(' ');
            foreach (string part in parts)
            {
                if (part.StartsWith("<@") && part.EndsWith(">"))
                {
                    try
                    {
                        ulong id = ulong.Parse(part.Substring(2, part.Length - 3));

                        var name = discord.GetUserAsync(id).Result.Username;
                        if (Plugin.Config.UseNicks)
                            name = ddMsg.Channel.Guild.GetMemberAsync(id).Result.Nickname;                        

                        msg = msg.Replace(part, "@" + name);
                    }
                    catch (FormatException) { }                    
                }
                if (part.StartsWith("<:") && part.EndsWith(">"))
                {
                    string id = part.Substring(2, part.Length - 3);
                    msg = msg.Replace(part, ":"+ id.Split(':')[0]+":");
                }
            }
            return msg;
        }

        private void OnCommandResponse(DiscordChannel channel, string message, string sender = "Server", string font = "White")
        {
            SEDiscordBridgePlugin.Log.Debug($"response length {message.Length}");
            if (message.Length > 0)
            {
                message = message.Replace("_", "\\_")
                    .Replace("*", "\\*")
                    .Replace("~", "\\~");

                const int chunkSize = 2000 - 1; // Remove 1 just ensure everything is ok

                if (message.Length <= chunkSize)
                {
                    SendCmdResponse(message, channel);
                }
                else
                {
                    var index = 0;
                    do {

                        SEDiscordBridgePlugin.Log.Debug($"while iteration index {index}");

                        /* if remaining part of message is small enough then just output it. */
                        if (index + chunkSize >= message.Length) {
                            SendCmdResponse(message.Substring(index), channel);
                            break;
                        }

                        var chunk = message.Substring(index, chunkSize);
                        var newLineIndex = chunk.LastIndexOf("\n");
                        SEDiscordBridgePlugin.Log.Debug($"while iteration newLineIndex {newLineIndex}");

                        SendCmdResponse(chunk.Substring(0, newLineIndex), channel);
                        index += newLineIndex + 1;

                    } while (index < message.Length);
                }
            }
        }
    }
}
