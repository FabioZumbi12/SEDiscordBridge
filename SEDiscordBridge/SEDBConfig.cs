using System.Collections.ObjectModel;
using System.Windows.Controls;
using Torch;
using Torch.Views;

namespace SEDiscordBridge
{
    public class SEDBConfig : ViewModel
    {
        private bool _enabled = true;
        public bool Enabled { get => _enabled; set => SetValue(ref _enabled, value); }

        private bool _preLoad = true;
        public bool PreLoad { get => _preLoad; set => SetValue(ref _preLoad, value); }

        private bool _dataCollect = true;
        public bool DataCollect { get => _dataCollect; set => SetValue(ref _dataCollect, value); }

        private string _token = "";
        public string BotToken { get => _token; set => SetValue(ref _token, value); }

        private string _chatChannelID = "";
        public string ChatChannelId { get => _chatChannelID; set => SetValue(ref _chatChannelID, value); }

        private string _format = ":rocket: **{p}**: {msg}";
        public string Format { get => _format; set => SetValue(ref _format, value); }

        private string _format2 = "[D]{p}";
        public string Format2 { get => _format2; set => SetValue(ref _format2, value); }

        private string _commandChannelID = "";
        public string CommandChannelId { get => _commandChannelID; set => SetValue(ref _commandChannelID, value); }

        private string _commandPrefix = ";;";
        public string CommandPrefix { get => _commandPrefix; set => SetValue(ref _commandPrefix, value); }

        private bool _asServer = false;       
        public bool AsServer { get => _asServer; set => SetValue(ref _asServer, value); }

        private bool _useNicks = false;
        public bool UseNicks { get => _useNicks; set => SetValue(ref _useNicks, value); }

        private bool _serverToDiscord = false;
        public bool ServerToDiscord { get => _serverToDiscord; set => SetValue(ref _serverToDiscord, value); }

        private string _serverName = "Server";
        public string ServerName { get => _serverName; set => SetValue(ref _serverName, value); }

        private string _statusChannelID = "";
        public string StatusChannelId { get => _statusChannelID; set => SetValue(ref _statusChannelID, value); }

        private string _started = ":white_check_mark: Server Started!";
        public string Started { get => _started; set => SetValue(ref _started, value); }

        private string _stopped = ":x: Server Stopped!";
        public string Stopped { get => _stopped; set => SetValue(ref _stopped, value); }

        private string _connect = ":key: The player {p} connected to server";
        public string Connect { get => _connect; set => SetValue(ref _connect, value); }

        private string _join = ":sunny: The player {p} joined the server";
        public string Join { get => _join; set => SetValue(ref _join, value); }

        private string _leave = ":new_moon: The player {p} left the server";
        public string Leave { get => _leave; set => SetValue(ref _leave, value); }
        
        private bool _useStatus = true;
        public bool UseStatus { get => _useStatus; set => SetValue(ref _useStatus, value); }

        private int _statusInterval = 5000;
        public int StatusInterval { get => _statusInterval; set => SetValue(ref _statusInterval, value); }

        private string _statusPre = "Server Starting...";
        public string StatusPre { get => _statusPre; set => SetValue(ref _statusPre, value); }

        private string _status = "{p} players | SS {ss}";
        public string Status { get => _status; set => SetValue(ref _status, value); }

        private bool _statusPlayer = true;
        public bool StatusPlayers { get => _statusPlayer; set => SetValue(ref _statusPlayer, value); }

        private string _statusUrl = "http://yourserverurl.or.empty";
        public string StatusUrl { get => _statusUrl; set => SetValue(ref _statusUrl, value); }

        private bool _mentionOthers = true;
        public bool MentOthers { get => _mentionOthers; set => SetValue(ref _mentionOthers, value); }

        private bool _mentionEveryone = false;
        public bool MentEveryone { get => _mentionEveryone; set => SetValue(ref _mentionEveryone, value); }

        private string _tokenVisibleState = "Visible";
        public string TokenVisibleState { get => _tokenVisibleState; set => SetValue(ref _tokenVisibleState, value); }

        private int _removeResponse = 30;
        public int RemoveResponse { get => _removeResponse; set => SetValue(ref _removeResponse, value); }

        private ObservableCollection<string> _facChannels = new ObservableCollection<string>();
        public ObservableCollection<string> FactionChannels { get => _facChannels; set => SetValue(ref _facChannels, value); }

        private string _globalColor = "White";
        public string GlobalColor { get => _globalColor; set => SetValue(ref _globalColor, value); }

        private string _facColor = "Green";
        public string FacColor { get => _facColor; set => SetValue(ref _facColor, value); }

        private string _facformat = ":ledger: **{p}**: {msg}";
        public string FacFormat { get => _facformat; set => SetValue(ref _facformat, value); }

        private string _facformat2 = "[D-Fac]{p}";
        public string FacFormat2 { get => _facformat2; set => SetValue(ref _facformat2, value); }

    }
}
