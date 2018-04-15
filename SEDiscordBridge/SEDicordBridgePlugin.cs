using System;
using System.IO;
using System.Timers;
using System.Windows.Controls;
using NLog;
using Sandbox.Game.World;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers.ChatManager;
using Torch.Server;
using Torch.Session;

namespace SEDiscordBridge
{
    public sealed class SEDicordBridgePlugin : TorchPluginBase, IWpfPlugin
    {
        public SEDBConfig Config => _config?.Data;
        private Persistent<SEDBConfig> _config;

        public DiscordBridge DDBridge;

        private UserControl _control;
        private TorchSessionManager _sessionManager;
        private ChatManagerServer _chatmanager;
        private IMultiplayerManagerBase _multibase;
        private Timer _timer;
        private TorchServer torchServer;

        public readonly Logger Log = LogManager.GetLogger("SEDicordBridge");

        /// <inheritdoc />
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            torchServer = (TorchServer)torch;

            try
            {
                _config = Persistent<SEDBConfig>.Load(Path.Combine(StoragePath, "SEDiscordBridge.cfg"));
            } catch(Exception e)
            {
                Log.Warn(e);
            }
            if (_config?.Data == null)
                _config = new Persistent<SEDBConfig>(Path.Combine(StoragePath, "SEDiscordBridge.cfg"), new SEDBConfig());

            if (Config.BotToken.Length > 0)
            {
                _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
                if (_sessionManager != null)
                    _sessionManager.SessionStateChanged += SessionChanged;
                else
                    Log.Warn("No session manager loaded!");                
            }
            else
                Log.Warn("No BOT token set, plugin will not work at all!");
            
        }

        private void MessageRecieved(TorchChatMessage msg, ref bool consumed)
        {
            if (msg.AuthorSteamId != null)
                DDBridge.SendMessage(msg.Author, msg.Message);
        }

        public void Save()
        {
            _config.Save();
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loaded:
                    _multibase = Torch.CurrentSession.Managers.GetManager<IMultiplayerManagerBase>();
                    if (_multibase != null)
                    {
                        _multibase.PlayerJoined += _multibase_PlayerJoined;
                        _multibase.PlayerLeft += _multibase_PlayerLeft;
                    }
                    else
                        Log.Warn("No join/leave manager loaded!");

                    _chatmanager = Torch.CurrentSession.Managers.GetManager<ChatManagerServer>();
                    if (_chatmanager != null)
                        _chatmanager.MessageRecieved += MessageRecieved;
                    else
                        Log.Warn("No chat manager loaded!");

                    Log.Warn("Starting Discord Bridge!");

                    DDBridge = new DiscordBridge(this);

                    //send status
                    if (Config.UseStatus)
                    {
                        StartTimer();
                    }

                    break;
                case TorchSessionState.Unloading:
                    if (DDBridge != null)
                    {
                        if (Config.Stopped.Length > 0)
                            DDBridge.SendMessage(null, Config.Stopped);
                        DDBridge.Stopdiscord();
                    }                    
                    Log.Warn("Discord Bridge Unloaded!");

                    Dispose();
                    break;
                default:
                    // ignore
                    break;
            }
        }

        public void StartTimer()
        {
            _timer = new Timer(Config.StatusInterval);
            _timer.Elapsed += _timer_Elapsed;
            _timer.Enabled = true;
        }

        public void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Elapsed -= _timer_Elapsed;
                _timer.Dispose();
                _timer = null;
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DDBridge.SendStatus(Config.Status
                .Replace("{p}", MySession.Static.Players.GetOnlinePlayers().Count.ToString())
                .Replace("{ss}", torchServer.SimulationRatio.ToString("0.00")));
        }

        private void _multibase_PlayerLeft(IPlayer obj)
        {
            if (Config.Leave.Length > 0)
            DDBridge.SendMessage(null, Config.Leave.Replace("{p}", obj.Name));                            
        }

        private void _multibase_PlayerJoined(IPlayer obj)
        {
            if (Config.Join.Length > 0)
                DDBridge.SendMessage(null, Config.Join.Replace("{p}", obj.Name));
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (_multibase != null)
            {
                _multibase.PlayerJoined -= _multibase_PlayerJoined;
                _multibase.PlayerLeft -= _multibase_PlayerLeft;
            }

            if (_sessionManager != null)
                _sessionManager.SessionStateChanged -= SessionChanged;
            _sessionManager = null;

            if (_chatmanager != null)
                _chatmanager.MessageRecieved -= MessageRecieved;
            _chatmanager = null;

            StopTimer();          
        }

        UserControl IWpfPlugin.GetControl()
        {
            return _control ?? (_control = new SEDBControl(this));
        }
    }   
}
