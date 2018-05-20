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

        public StringBuilder Response { get; } = new StringBuilder();

        public override void Respond(string message, string sender = "Server", string font = "Blue")
        {
            Response.AppendLine(message);
        }
    }
}
