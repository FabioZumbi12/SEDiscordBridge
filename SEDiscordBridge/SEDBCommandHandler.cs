using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.Plugins;
using Torch.Commands;

namespace SEDiscordBridge
{
    public class SEDBCommandHandler : CommandContext
    {
        /// <inheritdoc />
        public SEDBCommandHandler(ITorchBase torch, ITorchPlugin plugin, ulong steamIdSender, string rawArgs = null, List<string> args = null) :
            base(torch, plugin, steamIdSender, rawArgs, args)
        { }

        public event Action<DiscordChannel, string, string, string> OnResponse;
        public DiscordChannel ResponeChannel;

        private readonly StringBuilder _response = new StringBuilder();
        private CancellationTokenSource _cancelToken;

        public override void Respond(string message, string sender = "Server", string font = "Blue")
        {
            _response.AppendLine(message);

            if (_cancelToken != null)
                _cancelToken.Cancel();
            _cancelToken = new CancellationTokenSource();

            var a = Task.Delay(500, _cancelToken.Token)
                .ContinueWith((t) =>
                {
                    string chunk;
                    lock (_response)
                    {
                        chunk = _response.ToString();
                        _response.Clear();
                    }
                    OnResponse.Invoke(ResponeChannel, chunk, sender, font);
                });
        }
    }
}
