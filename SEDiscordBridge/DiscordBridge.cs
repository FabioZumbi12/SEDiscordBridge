using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Net.WebSocket;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        private Thread thread;
        private DiscordActivity game;
        private string lastMessage = "";
        private ulong botId = 0;

        public bool Ready { get; set; } = false;
        public static DiscordClient Discord { get; set; }

        public static int Cooldown;
        public static decimal Increment;
        public static decimal Factor;
        public static decimal CooldownNeutral;
        public static int FirstWarning;
        public static decimal MinIncrement;
        public static decimal Locked;
        public DiscordBridge(SEDiscordBridgePlugin plugin)
        {
            Plugin = plugin;

            Cooldown = plugin.Config.SimCooldown;
            Increment = (plugin.Config.StatusInterval / 1000);
            Factor = plugin.Config.SimCooldown / Increment;
            Increment = plugin.Config.SimCooldown / Increment;
            MinIncrement = 60 / (plugin.Config.StatusInterval / 1000);
            Locked = 0;

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
            await Discord?.DisconnectAsync();
        }

        private Task RegisterDiscord()
        {
            try
            {
                // Windows Vista - 8.1
                if (Environment.OSVersion.Platform.Equals(PlatformID.Win32NT) && Environment.OSVersion.Version.Major == 6)
                {
                    Discord = new DiscordClient(new DiscordConfiguration
                    {
                        Token = Plugin.Config.BotToken,
                        TokenType = TokenType.Bot,
                        WebSocketClientFactory = WebSocket4NetClient.CreateNew
                    });
                }
                else
                {
                    Discord = new DiscordClient(new DiscordConfiguration
                    {
                        Token = Plugin.Config.BotToken,
                        TokenType = TokenType.Bot
                    });
                }
            }
            catch (Exception) { }

            Discord.ConnectAsync();

            Discord.MessageCreated += Discord_MessageCreated;
            game = new DiscordActivity();

            Discord.Ready += async e =>
            {
                Ready = true;
                await Task.CompletedTask;
            };
            return Task.CompletedTask;
        }

        public void SendStatus(string status)
        {
            if (Ready && status?.Length > 0)
            {
                game.Name = status;
                Discord.UpdateStatusAsync(game);
            }
        }

        public void SendSimMessage(string msg)
        {
            new Thread(() =>
            {
                try
                {
                    if (Ready && Plugin.Config.SimChannel.Length > 0)
                    {
                        DiscordChannel chann = Discord.GetChannelAsync(ulong.Parse(Plugin.Config.SimChannel)).Result;
                        //mention
                        msg = MentionNameToID(msg, chann);
                        msg = Plugin.Config.SimMessage.Replace("{ts}", TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Now).ToString());
                        botId = Discord.SendMessageAsync(chann, msg.Replace("/n", "\n")).Result.Author.Id;
                    }
                }
                catch (Exception e)
                {
                    DiscordChannel chann = Discord.GetChannelAsync(ulong.Parse(Plugin.Config.SimChannel)).Result;
                    botId = Discord.SendMessageAsync(chann, e.ToString()).Result.Author.Id;
                }
            });            
        }

        public void SendChatMessage(string user, string msg)
        {
            new Thread(() => {
                try
                {
                    if (lastMessage.Equals(user + msg)) return;

                    if (Ready && Plugin.Config.ChatChannelId.Length > 0)
                    {
                        DiscordChannel chann = Discord.GetChannelAsync(ulong.Parse(Plugin.Config.ChatChannelId)).Result;
                        //mention
                        msg = MentionNameToID(msg, chann);

                        if (user != null)
                        {
                            msg = Plugin.Config.Format.Replace("{msg}", msg).Replace("{p}", user).Replace("{ts}", TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Now).ToString());
                        }
                        botId = Discord.SendMessageAsync(chann, msg.Replace("/n", "\n")).Result.Author.Id;
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        DiscordChannel chann = Discord.GetChannelAsync(ulong.Parse(Plugin.Config.ChatChannelId)).Result;
                        botId = Discord.SendMessageAsync(chann, e.ToString()).Result.Author.Id;
                    }
                    catch (Exception ex)
                    {
                        SEDiscordBridgePlugin.Log.Error($"SendChatMessage: {ex.Message}");
                    }
                }
            }).Start();            
        }

        public void SendFacChatMessage(string user, string msg, string facName)
        {
            new Thread(() =>
            {
                try
                {
                    IEnumerable<string> channelIds = Plugin.Config.FactionChannels.Where(c => c.Split(':')[0].Equals(facName));
                    if (Ready && channelIds.Count() > 0)
                    {
                        foreach (string chId in channelIds)
                        {
                            DiscordChannel chann = Discord.GetChannelAsync(ulong.Parse(chId.Split(':')[1])).Result;
                            //mention
                            msg = MentionNameToID(msg, chann);

                            if (user != null)
                            {
                                msg = Plugin.Config.FacFormat.Replace("{msg}", msg).Replace("{p}", user).Replace("{ts}", TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Now).ToString());
                            }
                            botId = Discord.SendMessageAsync(chann, msg.Replace("/n", "\n")).Result.Author.Id; ;
                        }
                    }
                }
                catch (Exception e)
                {
                    SEDiscordBridgePlugin.Log.Error($"SendFacChatMessage: {e.Message}");
                }
            }).Start();                
        }

        public void SendStatusMessage(string user, string msg)
        {
            new Thread(() => {
                if (Ready && Plugin.Config.StatusChannelId.Length > 0)
                {
                    try
                    {
                        DiscordChannel chann = Discord.GetChannelAsync(ulong.Parse(Plugin.Config.StatusChannelId)).Result;

                        if (user != null)
                        {
                            if (user.StartsWith("ID:"))
                                return;

                            msg = msg.Replace("{p}", user).Replace("{ts}", TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Now).ToString());
                        }
                        botId = Discord.SendMessageAsync(chann, msg.Replace("/n", "\n")).Result.Author.Id;
                    }
                    catch (Exception e)
                    {
                        SEDiscordBridgePlugin.Log.Error($"SendStatusMessage: {e.Message}");
                    }
                }
            }).Start();            
        }

        private Task Discord_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (!e.Author.IsBot || (!botId.Equals(e.Author.Id) && Plugin.Config.BotToGame))
            {
                string comChannelId = Plugin.Config.CommandChannelId;
                if (!string.IsNullOrEmpty(comChannelId))
                {
                    //execute commands
                    if (e.Channel.Id.Equals(ulong.Parse(Plugin.Config.CommandChannelId)) && e.Message.Content.StartsWith(Plugin.Config.CommandPrefix))
                    {
                        var cmdArgs = e.Message.Content.Substring(Plugin.Config.CommandPrefix.Length);
                        var cmd = cmdArgs.Split(' ')[0];

                        // Check for permission
                        if (Plugin.Config.CommandPerms.Count() > 0)
                        {
                            var userId = e.Author.Id.ToString();
                            bool hasRolePerm = e.Guild.GetMemberAsync(e.Author.Id).Result.Roles.Where(r => Plugin.Config.CommandPerms.Where(c => c.Split(':')[0].Equals(r.Id.ToString())).Any()).Any();

                            if (Plugin.Config.CommandPerms.Where(c =>
                            {
                                if (!hasRolePerm && !c.Split(':')[0].Equals(userId))
                                    return true;
                                else
                                if ((c.Split(':')[0].Equals(userId) || hasRolePerm) && (c.Split(':')[1].Equals(cmd) || c.Split(':')[1].Equals("*")))
                                    return false;

                                return true;
                            }).Any())
                            {
                                SendCmdResponse($"No permission for command: {cmd}", e.Channel, DiscordColor.Red, cmd);
                                return Task.CompletedTask;
                            }
                        }

                        // Server start command
                        if (cmd.Equals("bridge-startserver"))
                        {
                            if (Plugin.Torch.CurrentSession == null)
                            {
                                Plugin.Torch.Start();
                                SendCmdResponse("Torch initiated!", e.Channel, DiscordColor.Green, cmd);
                            }
                            else
                            {
                                SendCmdResponse("Torch is already running!", e.Channel, DiscordColor.Yellow, cmd);
                            }
                            return Task.CompletedTask;
                        }

                        if (Plugin.Torch.CurrentSession?.State == TorchSessionState.Loaded)
                        {
                            var manager = Plugin.Torch.CurrentSession.Managers.GetManager<CommandManager>();
                            var command = manager.Commands.GetCommand(cmdArgs, out string argText);

                            if (command == null)
                            {
                                SendCmdResponse($"Command not found: {cmdArgs}", e.Channel, DiscordColor.Red, cmd);
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
                                    SendCmdResponse($"Error executing command: {cmdArgs}", e.Channel, DiscordColor.Red, cmd);
                                }
                                SEDiscordBridgePlugin.Log.Info($"Server ran command '{cmdArgs}'");
                            }
                        }
                        else
                        {
                            SendCmdResponse("Error: Server is not running.", e.Channel, DiscordColor.Red, cmd);
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
                                var dSender = Plugin.Config.FacFormat2.Replace("{p}", sender);
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

        private void SendCmdResponse(string response, DiscordChannel chann, DiscordColor color, string command)
        {
            new Thread(() =>
            {
                DiscordEmbed discordEmbed = new DiscordEmbedBuilder()
                {
                    Description = response,
                    Color = color,
                    Title = string.IsNullOrEmpty(command) ? null : $"Command: {command}"
                };

                DiscordMessage dms = Discord.SendMessageAsync(chann, "", false, discordEmbed).Result;

                botId = dms.Author.Id;
                if (Plugin.Config.RemoveResponse > 0)
                    Task.Delay(Plugin.Config.RemoveResponse * 1000).ContinueWith(t => dms?.DeleteAsync());
            }).Start();            
        }

        private string MentionNameToID(string msg, DiscordChannel chann)
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
                            if (string.Compare(name, "everyone", true) == 0 && !Plugin.Config.MentEveryone)
                            {
                                msg = msg.Replace(part, part.Substring(1));
                                continue;
                            }
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

                        var emojis = chann.Guild.Emojis;
                        if (part.StartsWith(":") && part.EndsWith(":") && emojis.Any(e => string.Compare(e.Value.GetDiscordName(), part, true) == 0))
                        {
                            msg = msg.Replace(part, $"<{part}{emojis.Where(e => string.Compare(e.Value.GetDiscordName(), part, true) == 0).First().Key}>");
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
                if (part.StartsWith("<@!") && part.EndsWith(">"))
                {
                    try
                    {
                        ulong id = ulong.Parse(part.Substring(3, part.Length - 4));

                        var name = Discord.GetUserAsync(id).Result.Username;
                        if (Plugin.Config.UseNicks)
                            name = ddMsg.Channel.Guild.GetMemberAsync(id).Result.Nickname;

                        msg = msg.Replace(part, "@" + name);
                    }
                    catch (FormatException) { }
                }
                if (part.StartsWith("<:") && part.EndsWith(">"))
                {
                    string id = part.Substring(2, part.Length - 3);
                    msg = msg.Replace(part, ":" + id.Split(':')[0] + ":");
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
                    SendCmdResponse(message, channel, DiscordColor.Green, null);
                }
                else
                {
                    var index = 0;
                    do
                    {

                        SEDiscordBridgePlugin.Log.Debug($"while iteration index {index}");

                        /* if remaining part of message is small enough then just output it. */
                        if (index + chunkSize >= message.Length)
                        {
                            SendCmdResponse(message.Substring(index), channel, DiscordColor.Green, null);
                            break;
                        }

                        var chunk = message.Substring(index, chunkSize);
                        var newLineIndex = chunk.LastIndexOf("\n");
                        SEDiscordBridgePlugin.Log.Debug($"while iteration newLineIndex {newLineIndex}");

                        SendCmdResponse(chunk.Substring(0, newLineIndex), channel, DiscordColor.Green, null);
                        index += newLineIndex + 1;

                    } while (index < message.Length);
                }
            }
        }
    }
}
