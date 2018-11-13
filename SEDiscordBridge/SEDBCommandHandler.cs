using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Torch.API;
using Torch.API.Plugins;
using Torch.Commands;

namespace SEDiscordBridge
{
    public class SEDBCommandHandler : CommandContext
    {
        /// <inheritdoc />
        public SEDBCommandHandler(ITorchBase torch, ITorchPlugin plugin, ulong steamIdSender, string rawArgs = null, List<string> args = null) : 
            base(torch, plugin, steamIdSender, rawArgs, args) { }

        public event Action<DiscordChannel, string, string, string> OnResponse;
        public DiscordChannel ResponeChannel;

        public override void Respond(string message, string sender = "Server", string font = "Blue")
        {
            OnResponse.Invoke(ResponeChannel, message, sender, font);
        }
    }
}
